using System.Text;
using CompiladorConfiguracion.Sintactico;

namespace CompiladorConfiguracion.Generacion
{
    /// <summary>
    /// Genera un programa de consola en C# que LEE el archivo de configuración y guarda cada clave
    /// en una variable con su tipo de C# (string, int, double, bool o arreglo).
    /// Sirve para comprobar con .NET que el documento generado es válido y que los tipos se conservan.
    ///   JSON → System.Text.Json   ·   XML → System.Xml.Linq
    /// </summary>
    public class GeneradorCSharp
    {
        private static readonly HashSet<string> Reservadas = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        private readonly HashSet<string> _usadas = new() { "doc", "raiz" };

        public string Generar(Archivo archivo, string nombreArchivo)
        {
            bool json = archivo.NombreFormato == "JSON";
            var declaraciones = new List<string>();
            var impresiones = new List<string>();
            Recorrer(archivo.Elementos, new List<string>(), json, declaraciones, impresiones);

            var sb = new StringBuilder();
            sb.AppendLine("// Programa generado por el Mini-Compilador 11 · Lenguaje de configuración (Portal Nexo, UIP)");
            sb.AppendLine($"// Lee {nombreArchivo} y guarda cada clave en una variable de C# con su tipo.");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Globalization;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine(json ? "using System.Text.Json;" : "using System.Xml.Linq;");
            sb.AppendLine();
            sb.AppendLine("class Program");
            sb.AppendLine("{");
            sb.AppendLine("    static void Main()");
            sb.AppendLine("    {");
            sb.AppendLine("        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;");
            if (json)
            {
                sb.AppendLine($"        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(\"{nombreArchivo}\"));");
                sb.AppendLine("        JsonElement raiz = doc.RootElement;");
            }
            else
                sb.AppendLine($"        XElement raiz = XDocument.Load(\"{nombreArchivo}\", LoadOptions.PreserveWhitespace).Root!;");
            sb.AppendLine();
            foreach (var d in declaraciones) sb.AppendLine("        " + d);
            if (declaraciones.Count > 0) sb.AppendLine();
            sb.AppendLine($"        Console.WriteLine(\"Configuración leída de {nombreArchivo}:\");");
            foreach (var p in impresiones) sb.AppendLine("        " + p);
            sb.AppendLine("    }");
            sb.Append('}');
            return sb.ToString();
        }

        private void Recorrer(List<Elemento> elementos, List<string> ruta, bool json, List<string> decl, List<string> imp)
        {
            foreach (var el in elementos)
            {
                var r = new List<string>(ruta) { el.Nombre.Lexema };
                if (el is Seccion s) { Recorrer(s.Elementos, r, json, decl, imp); continue; }

                var c = (Clave)el;
                string variable = NombreVariable(r);
                string acceso = json
                    ? "raiz" + string.Concat(r.Select(n => $".GetProperty(\"{n}\")"))
                    : "raiz" + string.Concat(r.Select(n => $".Element(\"{n}\")!"));

                string tipoCs, lectura;
                if (c.Tipo == TipoValor.Lista)
                {
                    tipoCs = TipoCSharp(c.TipoElementos) + "[]";
                    lectura = json
                        ? $"{acceso}.EnumerateArray().Select(e => {LeerJson("e", c.TipoElementos)}).ToArray()"
                        : $"{acceso}.Elements(\"elemento\").Select(e => {LeerXml("e", c.TipoElementos)}).ToArray()";
                }
                else
                {
                    tipoCs = TipoCSharp(c.Tipo);
                    lectura = json ? LeerJson(acceso, c.Tipo) : LeerXml(acceso, c.Tipo);
                }
                decl.Add($"{tipoCs} {variable} = {lectura};");

                string valor = c.Tipo == TipoValor.Lista ? $"[{{string.Join(\", \", {variable})}}]" : $"{{{variable}}}";
                imp.Add($"Console.WriteLine($\"  {string.Join(".", r)} = {valor}\");");
            }
        }

        private static string TipoCSharp(TipoValor t) => t switch
        {
            TipoValor.Texto => "string",
            TipoValor.Entero => "int",
            TipoValor.Decimal => "double",
            _ => "bool"
        };

        private static string LeerJson(string e, TipoValor t) => t switch
        {
            TipoValor.Texto => $"{e}.GetString()!",
            TipoValor.Entero => $"{e}.GetInt32()",
            TipoValor.Decimal => $"{e}.GetDouble()",
            _ => $"{e}.GetBoolean()"
        };

        // Las conversiones explícitas de XElement usan las reglas de XML (punto decimal, true/false)
        private static string LeerXml(string e, TipoValor t) => $"({TipoCSharp(t)}){e}";

        /// <summary>servidor.puerto → servidor_puerto; evita palabras reservadas y repeticiones.</summary>
        private string NombreVariable(List<string> ruta)
        {
            string n = string.Join("_", ruta);
            if (Reservadas.Contains(n)) n = "@" + n;
            string baseN = n;
            for (int i = 2; _usadas.Contains(n); i++) n = baseN + i;
            _usadas.Add(n);
            return n;
        }
    }
}
