using System.Diagnostics;
using CompiladorCalculadora.Semantico;
using CompiladorCalculadora.Sintactico;

namespace CompiladorCalculadora.Generacion
{
    /// <summary>
    /// FASE 5 · Resultado.
    /// - Simular: recorre el árbol y calcula lo que imprimiría el programa (con las mismas reglas que C#).
    /// - EjecutarConDotnet: guarda el C# generado en un proyecto real, lo compila y lo ejecuta con .NET.
    /// </summary>
    public static class Ejecutor
    {
        public static List<string> Simular(Programa programa)
        {
            var memoria = new Dictionary<string, Valor>();
            var salida = new List<string>();

            foreach (var s in programa.Sentencias)
            {
                switch (s)
                {
                    case Declaracion d:
                        memoria[d.Nombre.Lexema] = Evaluar(d.Valor, memoria) with { Tipo = d.TipoDeclarado };
                        break;
                    case Asignacion a:
                        memoria[a.Nombre.Lexema] = Evaluar(a.Valor, memoria) with { Tipo = memoria[a.Nombre.Lexema].Tipo };
                        break;
                    case Impresion i:
                        string valor = i.Valor == null ? "" : Evaluar(i.Valor, memoria).ToString();
                        salida.Add(i.Etiqueta == null ? valor : i.Valor == null ? i.TextoEtiqueta! : i.TextoEtiqueta + " " + valor);
                        break;
                }
            }
            return salida;
        }

        private static Valor Evaluar(Expresion e, Dictionary<string, Valor> memoria) => e switch
        {
            Literal l => new Valor(l.Tipo, double.Parse(l.Token.Lexema, System.Globalization.CultureInfo.InvariantCulture)),
            Variable v => memoria[v.Nombre],
            Grupo g => Evaluar(g.Interior, memoria),
            Negacion n => Aritmetica.Negar(Evaluar(n.Operando, memoria)),
            Llamada f => Aritmetica.Funcion(f.Funcion, Evaluar(f.Argumento, memoria)).Resultado!.Value,
            Binaria b => Aritmetica.Binaria(b.Operador, Evaluar(b.Izquierda, memoria), Evaluar(b.Derecha, memoria), "").Resultado!.Value,
            _ => throw new InvalidOperationException()
        };

        /// <summary>
        /// Crea la carpeta "ProgramaGenerado" junto al ejecutable, escribe Program.cs y su .csproj,
        /// y ejecuta "dotnet run". Devuelve la salida real del programa.
        /// </summary>
        public static (bool Ok, string Salida, string Carpeta) EjecutarConDotnet(string codigoCSharp)
        {
            string carpeta = Path.Combine(AppContext.BaseDirectory, "ProgramaGenerado");
            Directory.CreateDirectory(carpeta);

            File.WriteAllText(Path.Combine(carpeta, "Program.cs"), codigoCSharp);
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
