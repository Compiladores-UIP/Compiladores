using System.Diagnostics;
using CompiladorVariables.Lexico;
using CompiladorVariables.Semantico;
using CompiladorVariables.Sintactico;

namespace CompiladorVariables.Generacion
{
    /// <summary>
    /// FASE 5 · Resultado.
    /// - Simular: recorre el árbol y muestra lo que imprimiría el programa.
    /// - EjecutarConDotnet: guarda el C# generado en un proyecto real y lo compila con .NET.
    /// </summary>
    public static class Ejecutor
    {
        public static List<string> Simular(Programa programa)
        {
            var valores = new Dictionary<string, string>();
            var salida = new List<string>();

            foreach (var s in programa.Sentencias)
            {
                if (s is Declaracion d) valores[d.Nombre.Lexema] = Valor(d.Valor, valores);
                else if (s is Impresion i) salida.Add(Valor(i.Valor, valores));
            }
            return salida;
        }

        // Muestra el valor tal como lo imprimiría Console.WriteLine en C#
        private static string Valor(Token t, Dictionary<string, string> valores) => t.Tipo switch
        {
            TipoToken.ValorTexto => t.Lexema.Trim('"'),
            TipoToken.ValorBooleano => t.Lexema == "VERDADERO" ? "True" : "False",
            TipoToken.Identificador => valores[t.Lexema],
            _ => t.Lexema
        };

        /// <summary>
        /// Crea la carpeta "ProgramaGenerado" junto al ejecutable, escribe Programa.cs y su .csproj,
        /// y ejecuta "dotnet run". Devuelve la salida real del programa.
        /// </summary>
        public static (bool Ok, string Salida, string Carpeta) EjecutarConDotnet(string codigoCSharp)
        {
            string carpeta = Path.Combine(AppContext.BaseDirectory, "ProgramaGenerado");
            Directory.CreateDirectory(carpeta);

            File.WriteAllText(Path.Combine(carpeta, "Programa.cs"), codigoCSharp);
            File.WriteAllText(Path.Combine(carpeta, "ProgramaGenerado.csproj"),
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
</Project>");

            try
            {
                var info = new ProcessStartInfo("dotnet", "run")
                {
                    WorkingDirectory = carpeta,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var proceso = Process.Start(info)!;
                string salida = proceso.StandardOutput.ReadToEnd() + proceso.StandardError.ReadToEnd();
                proceso.WaitForExit();
                return (proceso.ExitCode == 0, salida.TrimEnd(), carpeta);
            }
            catch (Exception ex)
            {
                return (false, "No se pudo ejecutar \"dotnet\": " + ex.Message +
                               "\nVerifica que el .NET SDK esté instalado (dotnet --version).", carpeta);
            }
        }
    }
}
