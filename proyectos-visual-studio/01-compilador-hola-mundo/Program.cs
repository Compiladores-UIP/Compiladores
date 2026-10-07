using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("Mini-compilador \"Hola Mundo\"");
Console.WriteLine();
Console.WriteLine("Escriba una instrucción con este formato:");
Console.WriteLine("IMPRIMIR \"texto\"");
Console.WriteLine();
Console.WriteLine("Ejemplo:");
Console.WriteLine("IMPRIMIR \"Hola Mundo\"");
Console.WriteLine();
Console.Write("Código > ");

string fuente = Console.ReadLine()?.Trim() ?? string.Empty;

try
{
    List<(string Texto, string Tipo)> tokens = AnalizarLexico(fuente);
    string mensaje = AnalizarSintaxis(tokens);
    string codigoGenerado = GenerarCodigo(mensaje);

    Console.WriteLine();
    Console.WriteLine("Código fuente recibido:");
    Console.WriteLine(fuente);

    Console.WriteLine();
    Console.WriteLine("Tokens encontrados:");
    foreach (var token in tokens)
    {
        Console.WriteLine($"{token.Texto,-24}{token.Tipo}");
    }

    Console.WriteLine();
    Console.WriteLine("Análisis sintáctico: correcto.");

    Console.WriteLine();
    Console.WriteLine("Código C# generado:");
    Console.WriteLine(codigoGenerado);

    (string rutaCodigo, string rutaEjecutable) = GenerarArchivos(codigoGenerado);

    Console.WriteLine();
    Console.WriteLine("Archivos generados:");
    Console.WriteLine($"Código C#: {rutaCodigo}");
    Console.WriteLine($"Ejecutable: {rutaEjecutable}");

    Console.WriteLine();
    Console.WriteLine("Resultado:");
    Console.WriteLine(EjecutarPrograma(rutaEjecutable));
}
catch (Exception error)
{
    Console.WriteLine();
    Console.WriteLine(error.Message);
    Environment.ExitCode = 1;
}

static List<(string Texto, string Tipo)> AnalizarLexico(string fuente)
{
    var tokens = new List<(string Texto, string Tipo)>();

    foreach (Match coincidencia in Regex.Matches(fuente, "\"[^\"]*\"|\\S+"))
    {
        string texto = coincidencia.Value;

        if (texto == "IMPRIMIR")
        {
            tokens.Add((texto, "PALABRA_RESERVADA"));
        }
        else if (Regex.IsMatch(texto, "^\"[^\"]*\"$"))
        {
            tokens.Add((texto, "CADENA"));
        }
        else
        {
            throw new Exception($"Error léxico: '{texto}' no pertenece al lenguaje.");
        }
    }

    return tokens;
}

static string AnalizarSintaxis(List<(string Texto, string Tipo)> tokens)
{
    if (tokens.Count == 0 || tokens[0].Texto != "IMPRIMIR")
    {
        throw new Exception("Error sintáctico: se esperaba la palabra reservada IMPRIMIR.");
    }

    if (tokens.Count < 2 || tokens[1].Tipo != "CADENA")
    {
        throw new Exception("Error sintáctico: se esperaba una cadena después de IMPRIMIR.");
    }

    if (tokens.Count > 2)
    {
        throw new Exception("Error sintáctico: hay texto adicional después de la instrucción válida.");
    }

    return tokens[1].Texto[1..^1];
}

static string GenerarCodigo(string mensaje)
{
    string textoSeguro = mensaje.Replace("\\", "\\\\").Replace("\"", "\\\"");

    var codigo = new StringBuilder();
    codigo.AppendLine("using System;");
    codigo.AppendLine();
    codigo.AppendLine("class Program");
    codigo.AppendLine("{");
    codigo.AppendLine("    static void Main()");
    codigo.AppendLine("    {");
    codigo.AppendLine($"        Console.WriteLine(\"{textoSeguro}\");");
    codigo.AppendLine("    }");
    codigo.AppendLine("}");

    return codigo.ToString();
}

static (string RutaCodigo, string RutaEjecutable) GenerarArchivos(string codigo)
{
    string carpetaProyecto = EncontrarCarpetaProyecto();
    string carpetaSalida = Path.Combine(carpetaProyecto, "salida", "hola-mundo");
    Directory.CreateDirectory(carpetaSalida);

    string rutaCodigo = Path.Combine(carpetaSalida, "ProgramaGenerado.cs");
    string rutaEjecutable = Path.Combine(carpetaSalida, "ProgramaGenerado.exe");

    File.WriteAllText(rutaCodigo, codigo, new UTF8Encoding(false));

    string carpetaTemporal = Path.Combine(
        Path.GetTempPath(),
        "MiniCompiladorHolaMundo",
        Guid.NewGuid().ToString("N"));

    Directory.CreateDirectory(carpetaTemporal);

    try
    {
        File.WriteAllText(
            Path.Combine(carpetaTemporal, "Program.cs"),
            codigo,
            new UTF8Encoding(false));

        const string proyectoGenerado = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
""";

        File.WriteAllText(
            Path.Combine(carpetaTemporal, "ProgramaGenerado.csproj"),
            proyectoGenerado,
            new UTF8Encoding(false));

        string carpetaPublicacion = Path.Combine(carpetaTemporal, "publicado");

        var inicio = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = carpetaTemporal,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        inicio.ArgumentList.Add("publish");
        inicio.ArgumentList.Add("ProgramaGenerado.csproj");
        inicio.ArgumentList.Add("-c");
        inicio.ArgumentList.Add("Release");
        inicio.ArgumentList.Add("-r");
        inicio.ArgumentList.Add("win-x64");
        inicio.ArgumentList.Add("--self-contained");
        inicio.ArgumentList.Add("false");
        inicio.ArgumentList.Add("-p:PublishSingleFile=true");
        inicio.ArgumentList.Add("--nologo");
        inicio.ArgumentList.Add("-o");
        inicio.ArgumentList.Add(carpetaPublicacion);

        using Process proceso = Process.Start(inicio)
            ?? throw new Exception("No fue posible iniciar la compilación del programa generado.");

        string salida = proceso.StandardOutput.ReadToEnd();
        string errores = proceso.StandardError.ReadToEnd();
        proceso.WaitForExit();

        if (proceso.ExitCode != 0)
        {
            string detalle = string.IsNullOrWhiteSpace(errores) ? salida : errores;
            throw new Exception($"Error al generar el ejecutable.{Environment.NewLine}{detalle.Trim()}");
        }

        string ejecutablePublicado = Path.Combine(carpetaPublicacion, "ProgramaGenerado.exe");
        if (!File.Exists(ejecutablePublicado))
        {
            throw new Exception("La compilación terminó, pero no se encontró ProgramaGenerado.exe.");
        }

        File.Copy(ejecutablePublicado, rutaEjecutable, true);
    }
    finally
    {
        if (Directory.Exists(carpetaTemporal))
        {
            Directory.Delete(carpetaTemporal, true);
        }
    }

    return (rutaCodigo, rutaEjecutable);
}

static string EjecutarPrograma(string rutaEjecutable)
{
    var inicio = new ProcessStartInfo(rutaEjecutable)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using Process proceso = Process.Start(inicio)
        ?? throw new Exception("No fue posible ejecutar ProgramaGenerado.exe.");

    string salida = proceso.StandardOutput.ReadToEnd();
    string errores = proceso.StandardError.ReadToEnd();
    proceso.WaitForExit();

    if (proceso.ExitCode != 0)
    {
        throw new Exception($"El programa generado terminó con error.{Environment.NewLine}{errores.Trim()}");
    }

    return salida.TrimEnd();
}

static string EncontrarCarpetaProyecto()
{
    DirectoryInfo? actual = new DirectoryInfo(AppContext.BaseDirectory);

    while (actual is not null)
    {
        if (File.Exists(Path.Combine(actual.FullName, "MiniCompiladorHolaMundo.csproj")))
        {
            return actual.FullName;
        }

        actual = actual.Parent;
    }

    actual = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (actual is not null)
    {
        if (File.Exists(Path.Combine(actual.FullName, "MiniCompiladorHolaMundo.csproj")))
        {
            return actual.FullName;
        }

        actual = actual.Parent;
    }

    throw new Exception("No fue posible localizar la carpeta del proyecto.");
}
