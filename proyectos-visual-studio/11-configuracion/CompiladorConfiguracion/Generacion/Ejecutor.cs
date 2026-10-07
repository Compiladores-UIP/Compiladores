using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using CompiladorConfiguracion.Sintactico;

namespace CompiladorConfiguracion.Generacion
{
    /// <summary>
    /// FASE 5 · Resultado.
    /// - Guardar: escribe el documento en la carpeta "salida" junto al ejecutable.
    /// - LeerDeVuelta: abre el documento con un lector real de .NET (System.Text.Json o XDocument)
    ///   y muestra cada clave con su tipo, igual que lo hará el programa C# generado.
    /// - EjecutarConDotnet: compila y ejecuta ese programa lector con .NET.
    /// </summary>
    public static class Ejecutor
    {
        public static string Guardar(string documento, string nombreArchivo)
        {
            string carpeta = Path.Combine(AppContext.BaseDirectory, "salida");
            Directory.CreateDirectory(carpeta);
            string ruta = Path.Combine(carpeta, nombreArchivo);
            File.WriteAllText(ruta, documento);
            return ruta;
        }

        /// <summary>
        /// Lee el documento generado y devuelve las líneas que imprimiría el programa C# lector.
        /// Si el documento no fuera válido, el lector lanzaría una excepción: así se comprueba la generación.
        /// </summary>
        public static List<string> LeerDeVuelta(Archivo archivo, string documento, string nombreArchivo)
        {
            var salida = new List<string> { $"Configuración leída de {nombreArchivo}:" };
            if (archivo.NombreFormato == "JSON")
            {
                using var doc = JsonDocument.Parse(documento);
                RecorrerJson(archivo.Elementos, doc.RootElement, "", salida);
            }
            else
            {
                var raiz = XDocument.Parse(documento, LoadOptions.PreserveWhitespace).Root!;
                RecorrerXml(archivo.Elementos, raiz, "", salida);
            }
            return salida;
        }

        private static void RecorrerJson(List<Elemento> elementos, JsonElement nodo, string prefijo, List<string> salida)
        {
            foreach (var el in elementos)
            {
                JsonElement hijo = nodo.GetProperty(el.Nombre.Lexema);
                string ruta = prefijo + el.Nombre.Lexema;
                if (el is Seccion s) { RecorrerJson(s.Elementos, hijo, ruta + ".", salida); continue; }
                var c = (Clave)el;
                string valor = c.Tipo == TipoValor.Lista
                    ? "[" + string.Join(", ", hijo.EnumerateArray().Select(e => Json(e, c.TipoElementos))) + "]"
                    : Json(hijo, c.Tipo);
                salida.Add($"  {ruta} = {valor}");
            }
        }

        private static string Json(JsonElement e, TipoValor t) => t switch
        {
            TipoValor.Texto => e.GetString()!,
            TipoValor.Entero => e.GetInt32().ToString(CultureInfo.InvariantCulture),
            TipoValor.Decimal => e.GetDouble().ToString(CultureInfo.InvariantCulture),
            _ => e.GetBoolean().ToString()
        };

        private static void RecorrerXml(List<Elemento> elementos, XElement nodo, string prefijo, List<string> salida)
        {
            foreach (var el in elementos)
            {
                XElement hijo = nodo.Element(el.Nombre.Lexema)!;
                string ruta = prefijo + el.Nombre.Lexema;
                if (el is Seccion s) { RecorrerXml(s.Elementos, hijo, ruta + ".", salida); continue; }
                var c = (Clave)el;
                string valor = c.Tipo == TipoValor.Lista
                    ? "[" + string.Join(", ", hijo.Elements("elemento").Select(e => Xml(e, c.TipoElementos))) + "]"
                    : Xml(hijo, c.Tipo);
                salida.Add($"  {ruta} = {valor}");
            }
        }

        private static string Xml(XElement e, TipoValor t) => t switch
        {
            TipoValor.Texto => (string)e,
            TipoValor.Entero => ((int)e).ToString(CultureInfo.InvariantCulture),
            TipoValor.Decimal => ((double)e).ToString(CultureInfo.InvariantCulture),
            _ => ((bool)e).ToString()
        };

        /// <summary>
        /// Crea la carpeta "ProgramaGenerado" junto al ejecutable con Program.cs, su .csproj y el documento,
        /// y ejecuta "dotnet run". Devuelve la salida real del programa lector.
        /// </summary>
        public static (bool Ok, string Salida, string Carpeta) EjecutarConDotnet(string codigoCSharp, string documento, string nombreArchivo)
        {
            string carpeta = Path.Combine(AppContext.BaseDirectory, "ProgramaGenerado");
            Directory.CreateDirectory(carpeta);
            foreach (var viejo in Directory.GetFiles(carpeta, "configuracion.*")) File.Delete(viejo);

            File.WriteAllText(Path.Combine(carpeta, "Program.cs"), codigoCSharp);
            File.WriteAllText(Path.Combine(carpeta, nombreArchivo), documento);
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
