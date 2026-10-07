using System.Diagnostics;

namespace MensajesPersonalizados.Compilador;

/// <summary>
/// Fase 5. Guarda el C# generado dentro de un proyecto .NET, lo compila con el SDK
/// y ejecuta el programa resultante. Si existe, se reutiliza la carpeta.
/// </summary>
public static class Ejecutor
{
    private const string Proyecto = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net8.0</TargetFramework>
            <Nullable>enable</Nullable>
            <AssemblyName>ProgramaGenerado</AssemblyName>
          </PropertyGroup>
        </Project>
        """;

    public static void GuardarArtefactos(string carpeta, ResultadoCompilacion resultado)
    {
        Directory.CreateDirectory(carpeta);
        File.WriteAllText(Path.Combine(carpeta, "Program.cs"), resultado.CodigoCSharp);
        File.WriteAllText(Path.Combine(carpeta, "ProgramaGenerado.csproj"), Proyecto);
        File.WriteAllText(Path.Combine(carpeta, "tokens.txt"), CompiladorMensajes.TablaTokens(resultado.Tokens));
        if (resultado.Arbol is not null)
        {
            File.WriteAllText(Path.Combine(carpeta, "arbol.txt"), CompiladorMensajes.DibujarArbol(resultado.Arbol));
        }
        File.WriteAllText(Path.Combine(carpeta, "simbolos.txt"), CompiladorMensajes.TablaSimbolos(resultado.Simbolos));
    }

    /// <returns>Código de salida del programa generado, o 1 si no se pudo compilar.</returns>
    public static int CompilarYEjecutar(string carpeta)
    {
        string csproj = Path.Combine(carpeta, "ProgramaGenerado.csproj");
        string binarios = Path.Combine(carpeta, "bin");

        var build = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string a in new[] { "build", csproj, "-c", "Release", "-o", binarios, "--nologo", "-v", "q", "-clp:ErrorsOnly" })
        {
            build.ArgumentList.Add(a);
        }

        Process proceso;
        try
        {
            proceso = Process.Start(build)!;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Console.WriteLine("No se encontró el comando 'dotnet'. Instale el SDK de .NET 8 para compilar el programa generado.");
            return 1;
        }

        string salida = proceso.StandardOutput.ReadToEnd() + proceso.StandardError.ReadToEnd();
        proceso.WaitForExit();
        if (proceso.ExitCode != 0)
        {
            Console.WriteLine("El compilador de C# rechazó el programa generado:");
            Console.WriteLine(salida.Trim());
            return 1;
        }

        Console.WriteLine($"Compilación con dotnet build: correcta → {Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.Combine(binarios, "ProgramaGenerado.dll"))}");
        Console.WriteLine();
        Console.WriteLine("──────── Salida del programa ────────");

        // La entrada y salida se heredan de esta consola para que PEDIR pueda leer del usuario.
        var ejecucion = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        ejecucion.ArgumentList.Add(Path.Combine(binarios, "ProgramaGenerado.dll"));
        using Process programa = Process.Start(ejecucion)!;
        programa.WaitForExit();
        Console.WriteLine("─────────────────────────────────────");
        return programa.ExitCode;
    }
}
