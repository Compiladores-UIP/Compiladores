using System.Text;
using CompiladorConfiguracion;
using CompiladorConfiguracion.Generacion;
using CompiladorConfiguracion.Sintactico;

// ==========================================================================
//  Mini-Compilador 11 · Lenguaje de configuración
//  Universidad Interamericana de Panamá · Compiladores · Portal Nexo
//  Grupo 4: Karen · Zachrison
//
//  Lenguaje fuente:  FORMATO JSON              →  JSON:  {
//                    CONFIG puerto = 3100      →           "puerto": 3100
//                                                        }
//
//  Uso:  dotnet run                     (menú interactivo)
//        dotnet run -- archivo.cfg      (compila un archivo directamente)
// ==========================================================================

Console.OutputEncoding = Encoding.UTF8;

if (args.Length > 0)
{
    if (!File.Exists(args[0])) { Console.WriteLine($"No se encontró el archivo \"{args[0]}\"."); return; }
    MostrarCompilacion(File.ReadAllText(args[0]));
    return;
}

ResultadoCompilacion? ultimo = null;

while (true)
{
    Encabezado();
    Console.WriteLine("  1. Compilar el ejemplo correcto (JSON)");
    Console.WriteLine("  2. Compilar el ejemplo correcto (XML)");
    Console.WriteLine("  3. Compilar el ejemplo con errores");
    Console.WriteLine("  4. Escribir mi propia configuración");
    Console.WriteLine("  5. Abrir un archivo .cfg o .txt");
    Console.WriteLine("  6. Compilar y ejecutar con .NET el programa que lee la última configuración");
    Console.WriteLine("  0. Salir");
    Console.Write("\n  Elige una opción: ");

    string opcion = Console.ReadLine()?.Trim() ?? "0";
    if (opcion == "0") break;

    string? codigo = opcion switch
    {
        "1" => LeerEjemplo("correcto_json.cfg"),
        "2" => LeerEjemplo("correcto_xml.cfg"),
        "3" => LeerEjemplo("con_errores.cfg"),
        "4" => LeerDelTeclado(),
        "5" => LeerArchivo(),
        _ => null
    };

    if (opcion == "6") EjecutarConDotnet(ultimo);
    else if (codigo != null)
    {
        var r = MostrarCompilacion(codigo);
        if (r.Exito) ultimo = r;
    }

    Console.Write("\n  Presiona Enter para volver al menú...");
    Console.ReadLine();
}

// --------------------------------------------------------------------------

static void Encabezado()
{
    Console.Clear();
    Pantalla.Escribir(ConsoleColor.Green, "\n  { } nexo.  Mini-Compilador 11 · Lenguaje de configuración");
    Pantalla.Nota("Configuración en español  →  JSON / XML");
    Pantalla.Nota("FORMATO JSON|XML  ·  CONFIG clave [: TIPO] = valor  ·  SECCION … FINSECCION  ·  listas [ ]\n");
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

    // FASE 2 · Árbol
    Pantalla.Titulo("2", "Análisis sintáctico (árbol)");
    if (r.Arbol.Formato != null || r.Arbol.Elementos.Count > 0) DibujarArbol(r.Arbol);
    MostrarErrores(r, Fase.Sintactico);

    // FASE 3 · Semántica y tabla de símbolos
    Pantalla.Titulo("3", "Análisis semántico y tabla de símbolos");
    foreach (var v in r.Semantico.Verificaciones)
        if (v.StartsWith("Aviso")) Pantalla.Escribir(ConsoleColor.DarkYellow, "  ! " + v);
        else Pantalla.Ok(v);
    if (r.Semantico.Tabla.Count > 0)
    {
        Console.WriteLine();
        Pantalla.Tabla(new[] { "Clave (ruta)", "Tipo", "Valor", "Línea" },
            r.Semantico.Tabla.Select(e => new[] { e.Ruta, e.Tipo, e.Valor, e.Linea.ToString() }));
    }
    MostrarErrores(r, Fase.Semantico);

    // FASE 4 · Generación
    Pantalla.Titulo("4", $"Generación del documento {(r.Exito ? r.Arbol.NombreFormato : "JSON / XML")}");
    if (r.Exito && r.Generador != null)
    {
        foreach (var t in r.Generador.Traducciones)
            Console.WriteLine($"  {t.Fuente,-40} →  {t.Destino}");
        Console.WriteLine();
        Pantalla.Nota($"{r.NombreArchivo}:");
        Pantalla.Codigo(r.Documento);
    }
    else Pantalla.Omitida();

    // FASE 5 · Resultado
    Pantalla.Titulo("5", "Resultado");
    if (r.Exito)
    {
        string ruta = Ejecutor.Guardar(r.Documento, r.NombreArchivo);
        Pantalla.Ok($"Archivo guardado en: {ruta}");
        Pantalla.Ok($"El documento se volvió a leer con {(r.Arbol.NombreFormato == "JSON" ? "System.Text.Json" : "System.Xml.Linq")} y es válido.");
        Console.WriteLine();
        foreach (var linea in r.Salida) Pantalla.Escribir(ConsoleColor.White, "  > " + linea);
        Console.WriteLine();
        Pantalla.Ok("Compilación exitosa. Usa la opción 6 del menú para ver el programa C# que lee esta configuración.");
    }
    else
    {
        Pantalla.Omitida();
        Console.WriteLine();
        Pantalla.Escribir(ConsoleColor.Red, $"  ✗ Compilación fallida: {r.Errores.Count} error(es).");
        if (r.Tokens.Count == 0) Pantalla.Nota("El archivo está vacío. Empieza con FORMATO JSON o FORMATO XML.");
    }
    return r;
}

static void MostrarErrores(ResultadoCompilacion r, Fase fase)
{
    var errores = r.Errores.Where(e => e.Fase == fase).ToList();
    if (errores.Count == 0)
    {
        bool omitida = fase == Fase.Semantico && r.Arbol.Formato == null;
        if (!omitida) Pantalla.Ok("Sin errores en esta fase.");
        return;
    }
    foreach (var e in errores)
    {
        Pantalla.Escribir(ConsoleColor.Red, "  ✗ " + e);
        if (e.Sugerencia != "") Pantalla.Escribir(ConsoleColor.DarkYellow, "    → " + e.Sugerencia);
    }
}

static void DibujarArbol(Archivo archivo)
{
    Pantalla.Escribir(ConsoleColor.Cyan, $"  Configuración (formato {archivo.Formato?.Lexema ?? "?"})");
    DibujarElementos(archivo.Elementos, "  ");
}

static void DibujarElementos(List<Elemento> elementos, string prefijo)
{
    for (int i = 0; i < elementos.Count; i++)
    {
        bool ultimo = i == elementos.Count - 1;
        string rama = prefijo + (ultimo ? "└─ " : "├─ ");
        string sub = prefijo + (ultimo ? "   " : "│  ");
        switch (elementos[i])
        {
            case Seccion s:
                Pantalla.Escribir(ConsoleColor.Yellow, $"{rama}Sección {s.Nombre.Lexema} (líneas {s.Linea}-{s.LineaFin})");
                DibujarElementos(s.Elementos, sub);
                break;
            case Clave c:
                string tipo = c.TipoAnotado != null ? $" : {c.TipoAnotado.Lexema}" : "";
                string valor = c.Valor is ValorLista l
                    ? "[" + string.Join(", ", l.Elementos.Select(e => e.Lexema)) + "]"
                    : c.Valor.Token.Lexema;
                Console.WriteLine($"{rama}Clave {c.Nombre.Lexema}{tipo} = {valor} (línea {c.Linea})");
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
    Console.WriteLine("\n  Escribe tu configuración, una instrucción por línea.");
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

static void EjecutarConDotnet(ResultadoCompilacion? r)
{
    if (r == null)
    {
        Console.WriteLine("\n  Primero compila una configuración sin errores (opciones 1, 2, 4 o 5).");
        return;
    }
    Console.WriteLine();
    Pantalla.Nota("Programa C# generado (lee el archivo y guarda cada clave con su tipo):");
    Pantalla.Codigo(r.CodigoCSharp);
    Console.WriteLine("\n  Compilando con .NET (puede tardar unos segundos)...");
    var (ok, salida, carpeta) = Ejecutor.EjecutarConDotnet(r.CodigoCSharp, r.Documento, r.NombreArchivo);
    Pantalla.Nota($"Proyecto generado en: {carpeta}\n");
    Pantalla.Escribir(ok ? ConsoleColor.White : ConsoleColor.Red, salida);
    if (ok) { Console.WriteLine(); Pantalla.Ok("El programa C# compiló, leyó la configuración y conservó los tipos."); }
}
