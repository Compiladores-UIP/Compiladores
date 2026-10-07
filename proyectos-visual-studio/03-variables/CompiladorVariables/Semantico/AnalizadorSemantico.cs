using CompiladorVariables.Lexico;
using CompiladorVariables.Sintactico;

namespace CompiladorVariables.Semantico
{
    /// <summary>Fila de la tabla de símbolos.</summary>
    public class Simbolo
    {
        public string Nombre { get; init; } = "";
        public string TipoFuente { get; init; } = "";   // ENTERO, TEXTO...
        public string TipoCSharp { get; init; } = "";   // int, string...
        public string Valor { get; set; } = "";         // valor tal como se muestra
        public int Linea { get; init; }
    }

    /// <summary>
    /// FASE 3 · Análisis semántico.
    /// Revisa el significado del programa y llena la tabla de símbolos:
    ///  - una variable no se puede declarar dos veces;
    ///  - una variable debe declararse antes de usarse;
    ///  - el valor debe ser compatible con el tipo (DECIMAL acepta ENTERO);
    ///  - el nombre no puede ser una palabra reservada de C#.
    /// </summary>
    public class AnalizadorSemantico
    {
        public static readonly Dictionary<string, string> TiposCSharp = new()
        {
            { "ENTERO", "int" },
            { "DECIMAL", "double" },
            { "TEXTO", "string" },
            { "BOOLEANO", "bool" }
        };

        private static readonly HashSet<string> ReservadasCSharp = new()
        {
            "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const",
            "continue","decimal","default","delegate","do","double","else","enum","event","explicit","extern",
            "false","finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface",
            "internal","is","lock","long","namespace","new","null","object","operator","out","override",
            "params","private","protected","public","readonly","ref","return","sbyte","sealed","short",
            "sizeof","stackalloc","static","string","struct","switch","this","throw","true","try","typeof",
            "uint","ulong","unchecked","unsafe","ushort","using","virtual","void","volatile","while"
        };

        public List<ErrorCompilacion> Errores { get; } = new();
        public Dictionary<string, Simbolo> Tabla { get; } = new();

        /// <summary>Notas de lo que se verificó en cada línea (para mostrar el proceso).</summary>
        public List<string> Verificaciones { get; } = new();

        public void Analizar(Programa programa)
        {
            foreach (var sentencia in programa.Sentencias)
            {
                if (sentencia is Declaracion d) AnalizarDeclaracion(d);
                else if (sentencia is Impresion i) AnalizarImpresion(i);
            }
        }

        private void AnalizarDeclaracion(Declaracion d)
        {
            string nombre = d.Nombre.Lexema;
            string tipo = d.Tipo.Lexema;

            if (Tabla.TryGetValue(nombre, out var previo))
            {
                Error(d.Nombre, $"La variable \"{nombre}\" ya fue declarada en la línea {previo.Linea}.",
                    "Cada variable se declara una sola vez. Usa otro nombre.");
                return;
            }

            if (ReservadasCSharp.Contains(nombre))
            {
                Error(d.Nombre, $"\"{nombre}\" es una palabra reservada de C# y el código generado no compilaría.",
                    "Elige otro nombre para la variable.");
                return;
            }

            string? tipoValor = TipoDelValor(d.Valor);
            if (tipoValor == null) return;   // variable no declarada (ya se reportó)

            bool compatible = tipoValor == tipo || (tipo == "DECIMAL" && tipoValor == "ENTERO");
            if (!compatible)
            {
                Error(d.Valor, $"No se puede guardar un valor {tipoValor} en una variable {tipo}.", Sugerencia(tipo, tipoValor));
                return;
            }

            Tabla[nombre] = new Simbolo
            {
                Nombre = nombre,
                TipoFuente = tipo,
                TipoCSharp = TiposCSharp[tipo],
                Valor = ValorMostrado(d.Valor),
                Linea = d.Linea
            };

            string conversion = tipoValor != tipo ? " (ENTERO se convierte a DECIMAL sin perder datos)" : "";
            Verificaciones.Add($"Línea {d.Linea}: \"{nombre}\" es nueva y el valor es {tipoValor}, compatible con {tipo}{conversion}.");
        }

        private void AnalizarImpresion(Impresion i)
        {
            string? tipo = TipoDelValor(i.Valor);
            if (tipo == null) return;
            string que = i.Valor.Tipo == TipoToken.Identificador ? $"la variable \"{i.Valor.Lexema}\" existe" : $"el valor es {tipo}";
            Verificaciones.Add($"Línea {i.Linea}: {i.Palabra.Lexema} es válido porque {que}.");
        }

        /// <summary>Devuelve el tipo de un literal o de una variable ya declarada.</summary>
        private string? TipoDelValor(Token valor)
        {
            switch (valor.Tipo)
            {
                case TipoToken.ValorEntero: return "ENTERO";
                case TipoToken.ValorDecimal: return "DECIMAL";
                case TipoToken.ValorTexto: return "TEXTO";
                case TipoToken.ValorBooleano: return "BOOLEANO";
                case TipoToken.Identificador:
                    if (Tabla.TryGetValue(valor.Lexema, out var s)) return s.TipoFuente;
                    bool pareceTexto = valor.Lexema.Length > 1 && char.IsUpper(valor.Lexema[0]) && valor.Lexema.Skip(1).Any(char.IsLower);
                    Error(valor, $"La variable \"{valor.Lexema}\" no ha sido declarada.",
                        pareceTexto ? $"Si es un texto, escríbelo entre comillas: \"{valor.Lexema}\"."
                                    : $"Declárala antes de usarla, por ejemplo: ENTERO {valor.Lexema} = 0");
                    return null;
                default: return null;
            }
        }

        private string ValorMostrado(Token valor) => valor.Tipo switch
        {
            TipoToken.ValorTexto => valor.Lexema.Trim('"'),
            TipoToken.Identificador => Tabla[valor.Lexema].Valor,
            _ => valor.Lexema
        };

        private static string Sugerencia(string destino, string origen) => (destino, origen) switch
        {
            ("ENTERO", "DECIMAL") => "Un ENTERO no guarda decimales. Usa DECIMAL o quita la parte decimal.",
            ("TEXTO", _) => "Para que sea TEXTO, escribe el valor entre comillas dobles.",
            ("BOOLEANO", _) => "Un BOOLEANO solo acepta VERDADERO o FALSO, sin comillas.",
            ("ENTERO", _) or ("DECIMAL", _) => "Escribe un número sin comillas.",
            _ => $"Usa un valor de tipo {destino}."
        };

        private void Error(Token token, string mensaje, string sugerencia) =>
            Errores.Add(new ErrorCompilacion(Fase.Semantico, token.Linea, token.Columna, mensaje, sugerencia));
    }
}
