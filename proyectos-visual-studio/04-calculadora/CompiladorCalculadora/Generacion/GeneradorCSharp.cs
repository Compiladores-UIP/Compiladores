using System.Text;
using CompiladorCalculadora.Sintactico;

namespace CompiladorCalculadora.Generacion
{
    /// <summary>
    /// FASE 4 · Generación de código C#.
    /// Recorre el árbol (ya anotado con tipos por el análisis semántico) y escribe un programa de consola.
    ///   ENTERO → int · DECIMAL → double · ^ → Math.Pow · RAIZ → Math.Sqrt · ABS → Math.Abs
    ///   ENTERO / ENTERO → (double)a / b   (en C#, 7 / 2 da 3; en la calculadora da 3.5)
    /// </summary>
    public class GeneradorCSharp
    {
        public List<(string Fuente, string CSharp)> Traducciones { get; } = new();

        public string Generar(Programa programa)
        {
            var cuerpo = new List<string>();
            foreach (var s in programa.Sentencias)
            {
                string cs = Sentencia(s);
                Traducciones.Add((s.Fuente, cs));
                cuerpo.Add(cs);
            }

            var sb = new StringBuilder();
            sb.AppendLine("// Programa generado por el Mini-Compilador 04 · Calculadora (Portal Nexo, UIP)");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Globalization;");
            sb.AppendLine();
            sb.AppendLine("class Program");
            sb.AppendLine("{");
            sb.AppendLine("    static void Main()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Punto como separador decimal, igual que en el lenguaje fuente");
            sb.AppendLine("        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;");
            sb.AppendLine();
            foreach (var linea in cuerpo) sb.AppendLine("        " + linea);
            sb.AppendLine("    }");
            sb.Append('}');
            return sb.ToString();
        }

        private static string Sentencia(Sentencia s) => s switch
        {
            Declaracion d => $"{(d.TipoDeclarado == TipoDato.Entero ? "int" : "double")} {d.Nombre.Lexema} = {Expr(d.Valor)};",
            Asignacion a => $"{a.Nombre.Lexema} = {Expr(a.Valor)};",
            Impresion { Etiqueta: not null, Valor: not null } i => $"Console.WriteLine({Cadena(i.TextoEtiqueta + " ")} + {Atomo(i.Valor)});",
            Impresion { Etiqueta: not null } i => $"Console.WriteLine({Cadena(i.TextoEtiqueta!)});",
            Impresion i => $"Console.WriteLine({Expr(i.Valor!)});",
            _ => throw new InvalidOperationException()
        };

        public static string Expr(Expresion e)
        {
            switch (e)
            {
                case Literal l: return l.Token.Lexema;
                case Variable v: return v.Nombre;
                case Grupo g: return "(" + Expr(g.Interior) + ")";
                case Negacion n:
                    // -(-a) para no escribir "--a", que en C# es el decremento
                    return n.Operando is Negacion ? $"-({Expr(n.Operando)})" : "-" + Expr(n.Operando);
                case Llamada f:
                    return $"Math.{(f.Funcion == "RAIZ" ? "Sqrt" : "Abs")}({Expr(f.Argumento)})";
                case Binaria b when b.Operador == "^":
                    return $"Math.Pow({Expr(b.Izquierda)}, {Expr(b.Derecha)})";
                case Binaria b when b.Operador == "/" && b.Izquierda.Tipo == TipoDato.Entero && b.Derecha.Tipo == TipoDato.Entero:
                    return $"(double){Atomo(b.Izquierda)} / {Expr(b.Derecha)}";
                case Binaria b:
                    return $"{Expr(b.Izquierda)} {b.Operador} {Expr(b.Derecha)}";
            }
            throw new InvalidOperationException();
        }

        // El cast (double) y la concatenación con texto solo afectan al operando inmediato:
        // si es compuesto, va entre paréntesis.
        private static string Atomo(Expresion e) =>
            e is Literal or Variable or Grupo or Llamada ? Expr(e) : "(" + Expr(e) + ")";

        private static string Cadena(string texto) =>
            "\"" + texto.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
