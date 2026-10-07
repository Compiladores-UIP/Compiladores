using System.Text;
using MensajesPersonalizados;
using MensajesPersonalizados.Compilador;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Uso:
//   dotnet run                                   → menú interactivo
//   dotnet run -- ejemplos/bienvenida.msj        → compila y ejecuta un archivo
//   dotnet run -- ejemplos/bienvenida.msj --solo-traducir
//   dotnet run -- --probar                       → pruebas automáticas
if (args.Contains("--ayuda") || args.Contains("-h"))
{
    MostrarAyuda();
    return 0;
}

if (args.Contains("--probar"))
{
    return Pruebas.Ejecutar();
}

bool soloTraducir = args.Contains("--solo-traducir");
string? archivo = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));

Console.WriteLine("Mini-compilador de Mensajes Personalizados · Tema 05 · Grupo 5");
Console.WriteLine();

string fuente;
string nombreSalida;
if (archivo is not null)
{
    string? ruta = Resolver(archivo);
    if (ruta is null)
    {
        Console.WriteLine($"No se encontró el archivo '{archivo}'.");
        return 1;
    }
    fuente = File.ReadAllText(ruta);
    nombreSalida = Path.GetFileNameWithoutExtension(ruta);
}
else
{
    (fuente, nombreSalida) = LeerDesdeConsola();
}

Titulo("1. Código fuente");
string[] lineas = fuente.Replace("\r\n", "\n").Split('\n');
for (int i = 0; i < lineas.Length; i++)
{
    if (i == lineas.Length - 1 && lineas[i].Length == 0) break;
    Console.WriteLine($"{i + 1,3} │ {lineas[i]}");
}

ResultadoCompilacion resultado = CompiladorMensajes.Compilar(fuente);

if (resultado.Tokens.Count > 0)
{
    Titulo("2. Análisis léxico · tokens");
    Console.Write(CompiladorMensajes.TablaTokens(resultado.Tokens));
}

if (resultado.Arbol is not null)
{
    Titulo("3. Análisis sintáctico · árbol");
    Console.Write(CompiladorMensajes.DibujarArbol(resultado.Arbol));
    Titulo("4. Análisis semántico · tabla de símbolos");
    Console.Write(CompiladorMensajes.TablaSimbolos(resultado.Simbolos));
}

foreach (string aviso in resultado.Avisos)
{
    Console.WriteLine(aviso);
}

if (!resultado.Exitoso)
{
    Titulo("Compilación detenida");
    foreach (Diagnostico error in resultado.Errores)
    {
        Console.WriteLine(error);
    }
    Console.WriteLine($"Total: {resultado.Errores.Count} error(es). No se generó código C#.");
    return 1;
}

Console.WriteLine("Análisis semántico: correcto.");

Titulo("5. Código C# generado");
Console.Write(resultado.CodigoCSharp);

string carpeta = Path.Combine(Directory.GetCurrentDirectory(), "salida", nombreSalida);
Ejecutor.GuardarArtefactos(carpeta, resultado);
Console.WriteLine();
Console.WriteLine($"Archivos guardados en: {Path.GetRelativePath(Directory.GetCurrentDirectory(), carpeta)}");
Console.WriteLine("  Program.cs · ProgramaGenerado.csproj · tokens.txt · arbol.txt · simbolos.txt");

if (soloTraducir)
{
    return 0;
}

Titulo("6. Compilación y ejecución");
return Ejecutor.CompilarYEjecutar(carpeta);

static (string Fuente, string Nombre) LeerDesdeConsola()
{
    string carpetaEjemplos = Path.Combine(AppContext.BaseDirectory, "ejemplos");
    string[] ejemplos = Directory.Exists(carpetaEjemplos)
        ? Directory.GetFiles(carpetaEjemplos, "*.msj").OrderBy(f => f, StringComparer.Ordinal).ToArray()
        : Array.Empty<string>();

    Console.WriteLine("Elija un ejemplo o escriba su propio programa:");
    Console.WriteLine("  0. Escribir mi propio código");
    for (int i = 0; i < ejemplos.Length; i++)
    {
        Console.WriteLine($"  {i + 1}. {Path.GetFileName(ejemplos[i])}");
    }
    Console.Write("Opción > ");

    if (int.TryParse(Console.ReadLine(), out int opcion) && opcion >= 1 && opcion <= ejemplos.Length)
    {
        string ruta = ejemplos[opcion - 1];
        return (File.ReadAllText(ruta), Path.GetFileNameWithoutExtension(ruta));
    }

    Console.WriteLine();
    Console.WriteLine("Escriba el programa línea por línea. Termine con EJECUTAR en una línea sola.");
    Console.WriteLine("Ejemplo:");
    Console.WriteLine("  TEXTO nombre = \"Ana\"");
    Console.WriteLine("  MOSTRAR \"Bienvenida, {nombre}\"");
    Console.WriteLine("  EJECUTAR");
    Console.WriteLine();

    var lineas = new List<string>();
    while (true)
    {
        Console.Write("> ");
        string? linea = Console.ReadLine();
        if (linea is null || linea.Trim() == "EJECUTAR") break;
        lineas.Add(linea);
    }
    return (string.Join('\n', lineas), "interactivo");
}

static string? Resolver(string archivo)
{
    if (File.Exists(archivo)) return Path.GetFullPath(archivo);
    string junto = Path.Combine(AppContext.BaseDirectory, archivo);
    return File.Exists(junto) ? junto : null;
}

static void Titulo(string texto)
{
    Console.WriteLine();
    Console.WriteLine($"═══ {texto} ═══");
}

static void MostrarAyuda()
{
    Console.WriteLine("""
        Mini-compilador de Mensajes Personalizados (Tema 05)

        dotnet run                                  Menú interactivo
        dotnet run -- <archivo.msj>                 Compila, genera C#, lo compila y lo ejecuta
        dotnet run -- <archivo.msj> --solo-traducir Solo genera el C# y los reportes
        dotnet run -- --probar                      Ejecuta las pruebas automáticas
        """);
}
