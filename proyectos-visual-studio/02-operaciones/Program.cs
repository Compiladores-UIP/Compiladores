using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Nexo;

Console.OutputEncoding = new UTF8Encoding(false);
const int Module = 2;
const string Title = "Operaciones matemáticas";
try
{
    if (args.Contains("--ayuda"))
    {
        Console.WriteLine($"Nexo · módulo {Module:00} · {Title}\n" +
            "dotnet run -- [--entrada archivo.nexo] [--salida carpeta] [--exe] [--pruebas]\n" +
            "Sin argumentos se compila ejemplos/principal.nexo. --exe publica el destino C#/WinForms para Windows; SQL/JSON/XML producen archivos.");
        return 0;
    }
    if (args.Contains("--pruebas"))
    {
        var cases = JsonSerializer.Deserialize<List<TestCase>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ejemplos", "casos.json")))!;
        int failures = 0;
        foreach (var test in cases)
        {
            try
            {
                var result = CompilerEngine.Compile(test.Source, Module);
                if (test.Error || result.Output.Replace("\r\n", "\n") != test.Expected)
                { Console.Error.WriteLine($"FALLO: {test.Name}"); failures++; }
                else Console.WriteLine($"OK: {test.Name}");
            }
            catch (Exception e)
            { if (test.Error) Console.WriteLine($"OK: {test.Name} → {e.Message}"); else { Console.Error.WriteLine($"FALLO: {test.Name} → {e.Message}"); failures++; } }
        }
        return failures == 0 ? 0 : 1;
    }
    string input = Path.Combine(AppContext.BaseDirectory, "ejemplos", "principal.nexo");
    string output = Path.GetFullPath("salida"); bool executable = false;
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--entrada": input = ReadOption(args, ref i); break;
            case "--salida": output = Path.GetFullPath(ReadOption(args, ref i)); break;
            case "--exe": executable = true; break;
            default: throw new ArgumentException($"Opción desconocida: {args[i]}. Usa --ayuda.");
        }
    }
    if (!File.Exists(input)) throw new ArgumentException($"No se encontró el archivo {Path.GetFileName(input)}. Comprueba --entrada.");
    var compilation = CompilerEngine.Compile(File.ReadAllText(input, Encoding.UTF8), Module);
    if (executable && compilation.Project == null) throw new InvalidOperationException("Este módulo genera archivos SQL/JSON/XML; no produce un ejecutable.");
    Directory.CreateDirectory(output);
    File.WriteAllText(Path.Combine(output, "tokens.txt"), string.Join('\n', compilation.Tokens), new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(output, "arbol.json"), JsonSerializer.Serialize(compilation.Tree, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(output, compilation.Filename), compilation.Generated, new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(output, "resultado.txt"), compilation.Output, new UTF8Encoding(false));
    if (compilation.Project != null) File.WriteAllText(Path.Combine(output, "ProgramaGenerado.csproj"), compilation.Project, new UTF8Encoding(false));
    Console.WriteLine($"{Title}\nEntrada: {Path.GetFileName(input)}\n\nResultado:\n{compilation.Output}\n\nArchivos guardados en: {Path.GetFileName(Path.TrimEndingDirectorySeparator(output))}");
    if (executable)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
        foreach (string arg in new[] { "publish", Path.Combine(output, "ProgramaGenerado.csproj"), "-c", "Release", "-r", "win-x64", "--self-contained", "false", "-o", Path.Combine(output, "binario") }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar dotnet publish.");
        process.WaitForExit(); if (process.ExitCode != 0) throw new InvalidOperationException("La publicación del ejecutable falló. Revisa los diagnósticos anteriores.");
        Console.WriteLine("Ejecutable generado: binario/ProgramaGenerado.exe");
    }
    return 0;
}
catch (Exception error)
{
    string message = error switch
    {
        UnauthorizedAccessException => "No tienes permiso para leer la entrada o guardar la salida. Elige otra carpeta.",
        IOException => "No se pudo leer o guardar un archivo. Comprueba que no esté bloqueado y que haya espacio disponible.",
        System.ComponentModel.Win32Exception => "No se pudo iniciar dotnet. Instala el SDK .NET 8 y comprueba que esté disponible en la terminal.",
        JsonException => "El contenido JSON no es válido. Revisa las comillas, los valores y las comas.",
        ArgumentException or InvalidOperationException => error.Message,
        _ => "Ocurrió un error inesperado. Usa --ayuda para comprobar las opciones."
    };
    Console.Error.WriteLine($"No se pudo compilar: {message}");
    return 1;
}

finally
{
    ConsoleWindow.WaitIfOpenedSeparately();
}

static string ReadOption(string[] options, ref int index)
{
    string option = options[index];
    if (index + 1 >= options.Length || string.IsNullOrWhiteSpace(options[index + 1]) || options[index + 1].StartsWith("--"))
        throw new ArgumentException($"Falta el valor de {option}. Usa --ayuda para ver un ejemplo.");
    return options[++index];
}

internal static class ConsoleWindow
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint GetConsoleProcessList([System.Runtime.InteropServices.Out] uint[] processes, uint count);

    public static void WaitIfOpenedSeparately()
    {
        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected || Console.IsOutputRedirected) return;
        // Una consola con un solo proceso se cierra cuando termina este programa.
        if (GetConsoleProcessList(new uint[2], 2) != 1) return;
        Console.WriteLine("\nPulsa una tecla para cerrar...");
        try { Console.ReadKey(true); }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }
}

internal record TestCase(string Name, string Source, string Expected, bool Error = false);
