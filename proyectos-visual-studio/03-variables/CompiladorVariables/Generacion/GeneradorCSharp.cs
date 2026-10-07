using CompiladorVariables.Lexico;
using CompiladorVariables.Semantico;
using CompiladorVariables.Sintactico;

namespace CompiladorVariables.Generacion
{
    /// <summary>
    /// FASE 4 · Generación de código.
    /// Traduce el árbol validado a un programa de consola completo en C#.
    /// </summary>
    public class GeneradorCSharp
    {
        /// <summary>Instrucciones traducidas, una por cada línea del código fuente.</summary>
        public List<(int Linea, string Fuente, string CSharp)> Traducciones { get; } = new();

        public string Generar(Programa programa, string[] lineasFuente)
        {
            var cuerpo = new List<string>();
            foreach (var s in programa.Sentencias)
            {
                string cs = s switch
                {
                    Declaracion d => $"{AnalizadorSemantico.TiposCSharp[d.Tipo.Lexema]} {d.Nombre.Lexema} = {Traducir(d.Valor)};",
                    Impresion i => $"Console.WriteLine({Traducir(i.Valor)});",
                    _ => ""
                };
                cuerpo.Add(cs);
                Traducciones.Add((s.Linea, lineasFuente[s.Linea - 1].Trim(), cs));
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("class Programa");
            sb.AppendLine("{");
            sb.AppendLine("    static void Main()");
            sb.AppendLine("    {");
            foreach (var linea in cuerpo) sb.AppendLine("        " + linea);
            sb.AppendLine("    }");
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Convierte un valor del lenguaje fuente a su forma en C#.</summary>
        private static string Traducir(Token valor) => valor.Tipo switch
        {
            TipoToken.ValorBooleano => valor.Lexema == "VERDADERO" ? "true" : "false",
            _ => valor.Lexema   // números, textos entre comillas e identificadores quedan igual
        };
    }
}
