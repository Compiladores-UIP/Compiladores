using System.Text;
using CompiladorConsultas;
using CompiladorConsultas.Generacion;
using CompiladorConsultas.Semantico;
using CompiladorConsultas.Sintactico;

// ==========================================================================
//  Mini-Compilador 10 · Consultas simples
//  Universidad Interamericana de Panamá · Compiladores · Portal Nexo
//  Grupo 3: Ana Vallarino · Josimar Osorio
//
//  Lenguaje fuente:  BUSCAR estudiante DONDE edad > 18
//  SQL generado:     SELECT * FROM estudiante WHERE edad > 18;
//
//  Uso:  dotnet run                     (menú interactivo)
//        dotnet run -- archivo.txt      (compila un archivo directamente)
// ==========================================================================

Console.OutputEncoding = Encoding.UTF8;

if (args.Length > 0)
{
    if (!File.Exists(args[0])) { Console.WriteLine($"No se encontró el archivo \"{args[0]}\"."); return; }
    MostrarCompilacion(File.ReadAllText(args[0]));
    return;
}

var sqlGenerado = new List<string>();

while (true)
{
    Encabezado();
    Console.WriteLine("  1. Compilar el ejemplo correcto");
    Console.WriteLine("  2. Compilar el ejemplo con errores");
    Console.WriteLine("  3. Escribir mis propias consultas");
    Console.WriteLine("  4. Abrir un archivo .txt");
    Console.WriteLine("  5. Ver las tablas de la base de datos");
    Console.WriteLine("  6. Exportar script SQL (tablas + consultas generadas)");
    Console.WriteLine("  0. Salir");
    Console.Write("\n  Elige una opción: ");

    string opcion = Console.ReadLine()?.Trim() ?? "0";
    if (opcion == "0") break;

    string? codigo = opcion switch
    {
        "1" => LeerEjemplo("correcto.txt"),
        "2" => LeerEjemplo("con_errores.txt"),
        "3" => LeerDelTeclado(),
        "4" => LeerArchivo(),
        _ => null
    };

    if (opcion == "5") MostrarTablas();
    else if (opcion == "6") Exportar(sqlGenerado);
    else if (codigo != null) sqlGenerado.AddRange(MostrarCompilacion(codigo));

    Console.Write("\n  Presiona Enter para volver al menú...");
    Console.ReadLine();
}

// --------------------------------------------------------------------------

static void Encabezado()
{
    Console.Clear();
    Pantalla.Escribir(ConsoleColor.Green, "\n  { } nexo.  Mini-Compilador 10 · Consultas simples");
    Pantalla.Nota("Consultas en español  →  SQL");
    Pantalla.Nota("BUSCAR tabla [DONDE condición] [ORDENAR POR campo]  ·  Tablas: estudiante, curso\n");
}

/// <summary>Compila, muestra cada fase y devuelve el SQL de las consultas válidas.</summary>
static List<string> MostrarCompilacion(string codigo)
{
    var r = Compilador.Compilar(codigo);
    var sqls = new List<string>();

    // Código fuente
    Console.WriteLine();
    Pantalla.Escribir(ConsoleColor.Green, "  CÓDIGO FUENTE");
    Pantalla.Escribir(ConsoleColor.DarkGray, "  " + new string('─', 60));
    var conError = r.Errores.Select(e => e.Linea).ToHashSet();
    for (int i = 0; i < r.LineasFuente.Length; i++)
    {
        if (i == r.LineasFuente.Length - 1 && r.LineasFuente[i].Trim() == "") break;
        Pantalla.Escribir(conError.Contains(i + 1) ? ConsoleColor.Red : ConsoleColor.White, $"  {i + 1,3} │ {r.LineasFuente[i]}");
    }

    // FASE 1
    Pantalla.Titulo("1", "Análisis léxico (tokens)");
    Pantalla.Tabla(new[] { "Línea", "Lexema", "Token" },
        r.Tokens.Select(t => new[] { $"{t.Linea}:{t.Columna}", t.Lexema, t.Nombre }));
    MostrarErrores(r, Fase.Lexico);

    // FASE 2
    Pantalla.Titulo("2", "Análisis sintáctico (árbol)");
    foreach (var q in r.Consultas) DibujarArbol(q);
    MostrarErrores(r, Fase.Sintactico);

    // FASE 3
    Pantalla.Titulo("3", "Análisis semántico");
    foreach (var v in r.Semantico.Verificaciones) Pantalla.Ok(v);
    MostrarErrores(r, Fase.Semantico);

    // FASE 4 y 5: solo si todo el programa compiló sin errores
    Pantalla.Titulo("4", "Generación de código SQL");
    if (!r.Exito)
    {
        Pantalla.Omitida();
        Pantalla.Titulo("5", "Resultado de la consulta");
        Pantalla.Omitida();
        Console.WriteLine();
        Pantalla.Escribir(ConsoleColor.Red, r.Consultas.Count == 0 && r.Errores.Count == 0
            ? "  No hay consultas para compilar."
            : $"  ✗ Compilación fallida: {r.Errores.Count} error(es).");
        return sqls;
    }

    foreach (var q in r.Consultas)
    {
        string sql = GeneradorSQL.GenerarSQL(q);
        sqls.Add(sql);
        Pantalla.Nota($"Línea {q.Linea}:  {q.Fuente}");
        Pantalla.Escribir(ConsoleColor.Yellow, "    " + sql);
        Pantalla.Escribir(ConsoleColor.DarkCyan, "    C# (LINQ): " + GeneradorSQL.GenerarLinq(q));
        Console.WriteLine();
    }

    Pantalla.Titulo("5", "Resultado de la consulta");
    foreach (var q in r.Consultas)
    {
        var (columnas, filas) = Ejecutor.Ejecutar(q);
        Pantalla.Nota($"Línea {q.Linea}:  {GeneradorSQL.GenerarSQL(q)}");
        if (filas.Count == 0) Pantalla.Escribir(ConsoleColor.White, "  (0 filas: ningún registro cumple la condición)");
        else Pantalla.Tabla(columnas, filas);
        Pantalla.Nota($"{filas.Count} fila(s)\n");
    }
    Pantalla.Ok($"Compilación exitosa: {r.Consultas.Count} consulta(s) traducidas a SQL.");
    return sqls;
}

static void MostrarErrores(ResultadoCompilacion r, Fase fase)
{
    var errores = r.Errores.Where(e => e.Fase == fase).ToList();
    if (errores.Count == 0) { Pantalla.Ok("Sin errores en esta fase."); return; }
    foreach (var e in errores)
    {
        Pantalla.Escribir(ConsoleColor.Red, "  ✗ " + e);
        if (e.Sugerencia != "") Pantalla.Escribir(ConsoleColor.DarkYellow, "    → " + e.Sugerencia);
    }
}

static void DibujarArbol(Consulta q)
{
    Pantalla.Escribir(ConsoleColor.Cyan, $"  Consulta (línea {q.Linea})");
    var ramas = new List<(string Texto, List<string> Hijos)>
    {
        ("BUSCAR → SELECT", new() { q.Campos.Count == 0 ? "Campos: * (todos)" : "Campos: " + string.Join(", ", q.Campos.Select(c => c.Lexema)) }),
        ("Tabla → FROM", new() { q.Tabla.Lexema })
    };
    if (q.Condiciones.Count > 0)
    {
        var hijos = new List<string>();
        for (int i = 0; i < q.Condiciones.Count; i++)
        {
            var c = q.Condiciones[i];
            string conector = i == 0 ? "" : q.Conectores[i - 1].Lexema + " ";
            hijos.Add($"{conector}Condición: {c.Campo.Lexema} {c.Operador.Lexema} {c.Valor.Lexema}");
        }
        ramas.Add(("DONDE → WHERE", hijos));
    }
    if (q.OrdenCampo != null)
        ramas.Add(("ORDENAR POR → ORDER BY", new() { q.OrdenCampo.Lexema + (q.OrdenDescendente ? " DESC" : " ASC") }));

    for (int i = 0; i < ramas.Count; i++)
    {
        bool ultima = i == ramas.Count - 1;
        Console.WriteLine($"  {(ultima ? "└─" : "├─")} {ramas[i].Texto}");
        for (int j = 0; j < ramas[i].Hijos.Count; j++)
            Console.WriteLine($"  {(ultima ? "   " : "│  ")}{(j == ramas[i].Hijos.Count - 1 ? "└─" : "├─")} {ramas[i].Hijos[j]}");
    }
    Console.WriteLine();
}

static void MostrarTablas()
{
    foreach (var t in BaseDeDatos.Tablas)
    {
        Console.WriteLine();
        Pantalla.Escribir(ConsoleColor.Green, $"  Tabla: {t.Nombre}");
        Pantalla.Nota(string.Join("  ·  ", t.Campos.Select(c => $"{c.Nombre} ({c.Tipo.ToString().ToLowerInvariant()})")));
        Pantalla.Tabla(t.Campos.Select(c => c.Nombre).ToArray(),
            t.Filas.Select(f => f.Select(BaseDeDatos.Formato).ToArray()));
    }
}

static void Exportar(List<string> sqls)
{
    string ruta = Path.Combine(AppContext.BaseDirectory, "consultas_generadas.sql");
    var sb = new StringBuilder(BaseDeDatos.ScriptCreacion());
    sb.AppendLine();
    sb.AppendLine("-- Consultas generadas por el compilador");
    if (sqls.Count == 0) sb.AppendLine("-- (todavía no se ha compilado ninguna consulta)");
    foreach (var s in sqls.Distinct()) sb.AppendLine(s);
    File.WriteAllText(ruta, sb.ToString());
    Console.WriteLine();
    Pantalla.Ok($"Script guardado en: {ruta}");
    Pantalla.Nota("Puedes ejecutarlo en SQL Server, MySQL o SQLite para comprobar los resultados.");
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
    Console.WriteLine("\n  Escribe una consulta por línea. Ejemplo: BUSCAR estudiante DONDE edad > 18");
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
