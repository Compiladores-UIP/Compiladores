using System.Text;
using CompiladorConfiguracion.Semantico;
using CompiladorConfiguracion.Sintactico;

namespace CompiladorConfiguracion.Generacion
{
    /// <summary>
    /// FASE 4 · Generación del documento de configuración.
    /// Recorre el árbol (anotado con tipos) y escribe JSON o XML conservando los tipos:
    ///   JSON:  "puerto": 3100   ·   "activo": true   ·   "idiomas": ["es", "en"]   ·   SECCION → objeto
    ///   XML:   &lt;puerto tipo="entero"&gt;3100&lt;/puerto&gt;   ·   SECCION → elemento con hijos
    /// </summary>
    public class GeneradorDocumento
    {
        public List<(string Fuente, string Destino)> Traducciones { get; } = new();
        public string Formato { get; private set; } = "JSON";
        public string NombreArchivo => "configuracion." + Formato.ToLowerInvariant();

        private string[] _lineas = Array.Empty<string>();

        public string Generar(Archivo archivo, string[] lineasFuente)
        {
            Formato = archivo.NombreFormato;
            _lineas = lineasFuente;
            Traducciones.Add((Fuente(archivo.Formato!.Linea), Formato == "JSON" ? "{ … }  (objeto JSON)" : "<?xml …?> <configuracion> … </configuracion>"));

            var sb = new StringBuilder();
            if (Formato == "JSON")
            {
                if (archivo.Elementos.Count == 0) sb.Append("{}");
                else { sb.AppendLine("{"); Json(sb, archivo.Elementos, 1); sb.Append('}'); }
            }
            else
            {
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                if (archivo.Elementos.Count == 0) sb.Append("<configuracion />");
                else { sb.AppendLine("<configuracion>"); Xml(sb, archivo.Elementos, 1); sb.Append("</configuracion>"); }
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------ JSON

        private void Json(StringBuilder sb, List<Elemento> elementos, int nivel)
        {
            string sangria = new(' ', nivel * 2);
            for (int i = 0; i < elementos.Count; i++)
            {
                string coma = i < elementos.Count - 1 ? "," : "";
                string clave = Cadena(elementos[i].Nombre.Lexema) + ": ";
                switch (elementos[i])
                {
                    case Clave c:
                        string valor = ValorJson(c);
                        sb.AppendLine(sangria + clave + valor + coma);
                        Traducciones.Add((Fuente(c.Linea), clave + valor));
                        break;
                    case Seccion s when s.Elementos.Count == 0:
                        sb.AppendLine(sangria + clave + "{}" + coma);
                        Traducciones.Add((Fuente(s.Linea), clave + "{"));
                        Traducciones.Add((Fuente(s.LineaFin), "}"));
                        break;
                    case Seccion s:
                        sb.AppendLine(sangria + clave + "{");
                        Traducciones.Add((Fuente(s.Linea), clave + "{"));
                        Json(sb, s.Elementos, nivel + 1);
                        sb.AppendLine(sangria + "}" + coma);
                        Traducciones.Add((Fuente(s.LineaFin), "}"));
                        break;
                }
            }
        }

        private static string ValorJson(Clave c) => c.Valor switch
        {
            ValorLista l => "[" + string.Join(", ", l.Elementos.Select(e => SimpleJson(e, c.TipoElementos))) + "]",
            _ => SimpleJson(c.Valor.Token, c.Tipo)
        };

        private static string SimpleJson(Lexico.Token t, TipoValor tipo) => tipo switch
        {
            TipoValor.Texto => Cadena(t.Valor),
            TipoValor.Booleano => AnalizadorSemantico.Booleano(t) ? "true" : "false",
            _ => AnalizadorSemantico.Numero(t, tipo)
        };

        /// <summary>Texto JSON con comillas y secuencias de escape.</summary>
        private static string Cadena(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\r': sb.Append("\\r"); break;
                    default:
                        if (ch < ' ') sb.Append($"\\u{(int)ch:x4}");
                        else sb.Append(ch);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        // ------------------------------------------------------------ XML

        private void Xml(StringBuilder sb, List<Elemento> elementos, int nivel)
        {
            string sangria = new(' ', nivel * 2);
            foreach (var el in elementos)
            {
                string n = el.Nombre.Lexema;
                switch (el)
                {
                    case Clave { Valor: ValorLista l } c:
                    {
                        string tipo = AnalizadorSemantico.Nombre(c.TipoElementos).ToLowerInvariant();
                        sb.AppendLine($"{sangria}<{n} tipo=\"lista\" elementos=\"{tipo}\">");
                        foreach (var e in l.Elementos)
                            sb.AppendLine($"{sangria}  <elemento>{Escapar(SimpleXml(e, c.TipoElementos))}</elemento>");
                        sb.AppendLine($"{sangria}</{n}>");
                        Traducciones.Add((Fuente(c.Linea), $"<{n} tipo=\"lista\" elementos=\"{tipo}\"> {l.Elementos.Count} × <elemento> </{n}>"));
                        break;
                    }
                    case Clave c:
                    {
                        string linea = $"<{n} tipo=\"{AnalizadorSemantico.Nombre(c.Tipo).ToLowerInvariant()}\">{Escapar(SimpleXml(c.Valor.Token, c.Tipo))}</{n}>";
                        sb.AppendLine(sangria + linea);
                        Traducciones.Add((Fuente(c.Linea), linea));
                        break;
                    }
                    case Seccion s when s.Elementos.Count == 0:
                        sb.AppendLine($"{sangria}<{n} />");
                        Traducciones.Add((Fuente(s.Linea), $"<{n}>"));
                        Traducciones.Add((Fuente(s.LineaFin), $"</{n}>"));
                        break;
                    case Seccion s:
                        sb.AppendLine($"{sangria}<{n}>");
                        Traducciones.Add((Fuente(s.Linea), $"<{n}>"));
                        Xml(sb, s.Elementos, nivel + 1);
                        sb.AppendLine($"{sangria}</{n}>");
                        Traducciones.Add((Fuente(s.LineaFin), $"</{n}>"));
                        break;
                }
            }
        }

        private static string SimpleXml(Lexico.Token t, TipoValor tipo) => tipo switch
        {
            TipoValor.Texto => t.Valor,
            TipoValor.Booleano => AnalizadorSemantico.Booleano(t) ? "true" : "false",
            _ => AnalizadorSemantico.Numero(t, tipo)
        };

        private static string Escapar(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private string Fuente(int linea) => _lineas[linea - 1].Trim();
    }
}
