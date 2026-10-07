using System.Text;
using CompiladorVariables;
using CompiladorVariables.Generacion;
using CompiladorVariables.Sintactico;

// ==========================================================================
//  Mini-Compilador 03 · Variables
//  Universidad Interamericana de Panamá · Compiladores · Portal Nexo
//  Grupo 3: Ana Vallarino · Josimar Osorio
//
//  Lenguaje fuente:  ENTERO edad = 25      →  C#:  int edad = 25;
//                    IMPRIMIR edad         →       Console.WriteLine(edad);
//
//  Uso:  dotnet run                     (menú interactivo)
//        dotnet run -- archivo.txt      (compila un archivo directamente)
// ==========================================================================

Console.OutputEncoding = Encoding.UTF8;

if (args.Length > 0)
{
    // Modo directo: compilar el archivo indicado
    if (!File.Exists(args[0])) { Console.WriteLine($"No se encontró el archivo \"{args[0]}\"."); return; }
    MostrarCompilacion(File.ReadAllText(args[0]));
    return;
}

string? ultimoCSharp = null;

while (true)
{
    Encabezado();
    Console.WriteLine("  1. Compilar el ejemplo correcto");
    Console.WriteLine("  2. Compilar el ejemplo con errores");
    Console.WriteLine("  3. Escribir mi propio código");
    Console.WriteLine("  4. Abrir un archivo .txt");
    Console.WriteLine("  5. Compilar y ejecutar el último C# generado con .NET");
    Console.WriteLine("  0. Salir");
    Console.Write("\n  Elige una opción: ");

    string opcion = Console.ReadLine()?.Trim() ?? "0";
    string? codigo = opcion switch
    {
        "1" => LeerEjemplo("correcto.txt"),
        "2" => LeerEjemplo("con_errores.txt"),
        "3" => LeerDelTeclado(),
        "4" => LeerArchivo(),
        _ => null
    };

    if (opcion == "0") break;

    if (opcion == "5")
    {
        EjecutarConDotnet(ultimoCSharp);
    }
    else if (codigo != null)
    {
        var r = MostrarCompilacion(codigo);
        if (r.Exito) ultimoCSharp = r.CodigoCSharp;
    }

    Console.Write("\n  Presiona Enter para volver al menú...");
    Console.ReadLine();
}

// --------------------------------------------------------------------------

static void Encabezado()
{
    Console.Clear();
    Pantalla.Escribir(ConsoleColor.Green, "\n  { } nexo.  Mini-Compilador 03 · Variables");
    Pantalla.Nota("Lenguaje en español  →  C# Básico");
    Pantalla.Nota("Tipos: ENTERO, DECIMAL, TEXTO, BOOLEANO  ·  Salida: IMPRIMIR / MOSTRAR\n");
}

static ResultadoCompilacion MostrarCompilacion(string codigo)
{
    var r = Compilador.Compilar(codigo);

    // Código fuente
    Console.WriteLine();
    Pantalla.Escribir(ConsoleColor.Green, "  CÓDIGO FUENTE");
    Pantalla.Escribir(ConsoleColor.DarkGray, "  " + new string('─', 60));
    var lineasConError = r.Errores.Select(e => e.Linea).ToHashSet();
    for (int i = 0; i < r.LineasFuente.Length; i++)
    {
        if (i == r.LineasFuente.Length - 1 && r.LineasFuente[i].Trim() == "") break;
        var color = lineasConError.Contains(i + 1) ? ConsoleColor.Red : ConsoleColor.White;
        Pantalla.Escribir(color, $"  {i + 1,3} │ {r.LineasFuente[i]}");
    }

    // FASE 1 · Tokens
    Pantalla.Titulo("1", "Análisis léxico (tokens)");
    Pantalla.Tabla(new[] { "Línea", "Lexema", "Token" },
        r.Tokens.Select(t => new[] { $"{t.Linea}:{t.Columna}", t.Lexema, t.Nombre }));
    MostrarErrores(r, Fase.Lexico);

    // FASE 2 · Árbol sintáctico
    Pantalla.Titulo("2", "Análisis sintáctico (árbol)");
    if (r.Arbol.Sentencias.Count > 0) DibujarArbol(r.Arbol);
    MostrarErrores(r, Fase.Sintactico);

    // FASE 3 · Semántica y tabla de símbolos
    Pantalla.Titulo("3", "Análisis semántico y tabla de símbolos");
    foreach (var v in r.Semantico.Verificaciones) Pantalla.Ok(v);
    if (r.Semantico.Tabla.Count > 0)
    {
        Console.WriteLine();
        Pantalla.Tabla(new[] { "Identificador", "Tipo", "Tipo C#", "Valor", "Línea" },
            r.Semantico.Tabla.Values.Select(s => new[] { s.Nombre, s.TipoFuente.ToLowerInvariant(), s.TipoCSharp, s.Valor, s.Linea.ToString() }));
    }
    MostrarErrores(r, Fase.Semantico);

    // FASE 4 · Generación de C#
    Pantalla.Titulo("4", "Generación de código C#");
    if (r.Exito && r.Generador != null)
    {
        foreach (var t in r.Generador.Traducciones)
            Console.WriteLine($"  {t.Fuente,-34} →  {t.CSharp}");
        Console.WriteLine();
        Pantalla.Nota("Programa completo:");
        Pantalla.Codigo(r.CodigoCSharp);
    }
    else Pantalla.Omitida();

    // FASE 5 · Resultado
    Pantalla.Titulo("5", "Resultado de la ejecución");
    if (r.Exito)
    {
        if (r.Salida.Count == 0) Pantalla.Nota("El programa no imprime nada. Agrega una línea IMPRIMIR para ver la salida.");
        foreach (var linea in r.Salida) Pantalla.Escribir(ConsoleColor.White, "  > " + linea);
        Console.WriteLine();
        Pantalla.Ok("Compilación exitosa. Usa la opción 5 del menú para compilarlo y ejecutarlo con .NET.");
    }
    else
    {
        Pantalla.Omitida();
        Console.WriteLine();
        Pantalla.Escribir(ConsoleColor.Red, $"  ✗ Compilación fallida: {r.Errores.Count} error(es).");
        if (r.Arbol.Sentencias.Count == 0 && r.Errores.Count == 0)
            Pantalla.Nota("No hay instrucciones para compilar.");
    }
    return r;
}

static void MostrarErrores(ResultadoCompilacion r, Fase fase)
{
    var errores = r.Errores.Where(e => e.Fase == fase).ToList();
    if (errores.Count == 0)
    {
        bool omitida = fase != Fase.Lexico && (r.ErroresLexicos || (fase == Fase.Semantico && r.ErroresSintacticos))
                       && r.Arbol.Sentencias.Count == 0;
        if (!omitida) Pantalla.Ok("Sin errores en esta fase.");
        return;
    }
    foreach (var e in errores)
    {
        Pantalla.Escribir(ConsoleColor.Red, "  ✗ " + e);
        if (e.Sugerencia != "") Pantalla.Escribir(ConsoleColor.DarkYellow, "    → " + e.Sugerencia);
    }
}

static void DibujarArbol(Programa programa)
{
    Pantalla.Escribir(ConsoleColor.Cyan, "  Programa");
    for (int i = 0; i < programa.Sentencias.Count; i++)
    {
        bool ultimo = i == programa.Sentencias.Count - 1;
        string rama = ultimo ? "  └─ " : "  ├─ ";
        string sub = ultimo ? "     " : "  │  ";

        switch (programa.Sentencias[i])
        {
            case Declaracion d:
                Console.WriteLine($"{rama}Declaración (línea {d.Linea})");
                Console.WriteLine($"{sub}├─ Tipo: {d.Tipo.Lexema}");
                Console.WriteLine($"{sub}├─ Identificador: {d.Nombre.Lexema}");
                Console.WriteLine($"{sub}├─ Operador: =");
                Console.WriteLine($"{sub}└─ Valor: {d.Valor.Lexema}");
                break;
            case Impresion p:
                Console.WriteLine($"{rama}Impresión (línea {p.Linea})");
                Console.WriteLine($"{sub}└─ Valor: {p.Valor.Lexema}");
                break;
        }
    }
}

static string? LeerEjemplo(string archivo)
{
    string ruta = Path.Combine(AppContext.BaseDirectory, "ejemplos", archivo);
    if (File.Exists(ruta)) return File.ReadAllText(ruta);
    Console.WriteLine($"\n  No se encontró {ruta}.");
    return null;
}

static string? LeerDelTeclado()
{
    Console.WriteLine("\n  Escribe tu programa, una instrucción por línea.");
    Pantalla.Nota("Deja una línea vacía para compilar.\n");
    var sb = new StringBuilder();
    int n = 1;
    while (true)
    {
        Console.Write($"  {n,3} │ ");
        string? linea = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(linea)) break;
        sb.AppendLine(linea);
        n++;
    }
    return sb.Length == 0 ? null : sb.ToString();
}

static string? LeerArchivo()
{
    Console.Write("\n  Ruta del archivo: ");
    string ruta = (Console.ReadLine() ?? "").Trim().Trim('"');
    if (File.Exists(ruta)) return File.ReadAllText(ruta);
    Console.WriteLine("  No se encontró el archivo.");
    return null;
}

static void EjecutarConDotnet(string? codigoCSharp)
{
    if (codigoCSharp == null)
    {
        Console.WriteLine("\n  Primero compila un programa sin errores (opciones 1, 3 o 4).");
        return;
    }
    Console.WriteLine("\n  Compilando con .NET (puede tardar unos segundos)...");
    var (ok, salida, carpeta) = Ejecutor.EjecutarConDotnet(codigoCSharp);
    Pantalla.Nota($"Proyecto generado en: {carpeta}\n");
    Pantalla.Escribir(ok ? ConsoleColor.White : ConsoleColor.Red, salida);
    if (ok) { Console.WriteLine(); Pantalla.Ok("El programa C# generado compiló y se ejecutó con .NET."); }
}
