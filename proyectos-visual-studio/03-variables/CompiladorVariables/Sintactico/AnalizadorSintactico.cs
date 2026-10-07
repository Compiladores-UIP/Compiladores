using CompiladorVariables.Lexico;

namespace CompiladorVariables.Sintactico
{
    /// <summary>
    /// FASE 2 · Análisis sintáctico.
    /// Comprueba que cada línea siga la gramática y construye el árbol sintáctico.
    ///
    /// Gramática (BNF):
    ///   &lt;programa&gt;    ::= { &lt;sentencia&gt; }
    ///   &lt;sentencia&gt;   ::= &lt;declaracion&gt; | &lt;impresion&gt;
    ///   &lt;declaracion&gt; ::= &lt;tipo&gt; IDENTIFICADOR "=" &lt;valor&gt; [ ";" ]
    ///   &lt;impresion&gt;   ::= ( IMPRIMIR | MOSTRAR ) &lt;valor&gt; [ ";" ]
    ///   &lt;tipo&gt;        ::= ENTERO | DECIMAL | TEXTO | BOOLEANO
    ///   &lt;valor&gt;       ::= NUMERO_ENTERO | NUMERO_DECIMAL | CADENA | VERDADERO | FALSO | IDENTIFICADOR
    /// </summary>
    public class AnalizadorSintactico
    {
        public List<ErrorCompilacion> Errores { get; } = new();

        public Programa Analizar(List<Token> tokens, HashSet<int> lineasConErrorLexico)
        {
            var programa = new Programa();

            // Una instrucción por línea: se agrupan los tokens por número de línea
            foreach (var grupo in tokens.GroupBy(t => t.Linea).OrderBy(g => g.Key))
            {
                if (lineasConErrorLexico.Contains(grupo.Key)) continue;   // ya se reportó en la fase léxica
                var sentencia = AnalizarLinea(grupo.ToList(), grupo.Key);
                if (sentencia != null) programa.Sentencias.Add(sentencia);
            }
            return programa;
        }

        private Sentencia? AnalizarLinea(List<Token> t, int linea)
        {
            Token primero = t[0];
            int finColumna = t[^1].Columna + t[^1].Lexema.Length;

            // ---------- Declaración ----------
            if (primero.Tipo == TipoToken.TipoDato)
            {
                if (t.Count < 2)
                    return Error(linea, finColumna, $"Falta el nombre de la variable después de {primero.Lexema}.",
                        $"Ejemplo: {primero.Lexema} edad = 25");

                if (t[1].Tipo != TipoToken.Identificador)
                {
                    string motivo = t[1].Tipo is TipoToken.TipoDato or TipoToken.Imprimir or TipoToken.ValorBooleano
                        ? $"\"{t[1].Lexema}\" es una palabra reservada y no puede usarse como nombre."
                        : $"Se esperaba el nombre de la variable y se encontró \"{t[1].Lexema}\".";
                    return Error(linea, t[1].Columna, motivo, "Usa un nombre que empiece con letra, como edad o nombre.");
                }

                if (t.Count < 3 || t[2].Tipo != TipoToken.Asignacion)
                    return Error(linea, t.Count < 3 ? finColumna : t[2].Columna,
                        $"Se esperaba \"=\" después de \"{t[1].Lexema}\".",
                        $"Ejemplo: {primero.Lexema} {t[1].Lexema} = ...");

                if (t.Count < 4 || t[3].Tipo == TipoToken.FinSentencia)
                    return Error(linea, finColumna, "Falta el valor después de \"=\".",
                        "Escribe el valor inicial de la variable.");

                if (!t[3].EsValor && t[3].Tipo != TipoToken.Identificador)
                    return Error(linea, t[3].Columna, $"\"{t[3].Lexema}\" no es un valor válido.",
                        "Un valor puede ser un número, un texto entre comillas, VERDADERO/FALSO o una variable.");

                if (!VerificarCierre(t, 4, linea)) return null;

                return new Declaracion { Linea = linea, Tipo = t[0], Nombre = t[1], Valor = t[3] };
            }

            // ---------- Impresión ----------
            if (primero.Tipo == TipoToken.Imprimir)
            {
                if (t.Count < 2 || t[1].Tipo == TipoToken.FinSentencia)
                    return Error(linea, finColumna, $"Se esperaba un valor o una variable después de {primero.Lexema}.",
                        $"Ejemplo: {primero.Lexema} edad");

                if (!t[1].EsValor && t[1].Tipo != TipoToken.Identificador)
                    return Error(linea, t[1].Columna, $"No se puede imprimir \"{t[1].Lexema}\".",
                        "Imprime una variable o un valor, por ejemplo: IMPRIMIR nombre");

                if (!VerificarCierre(t, 2, linea)) return null;

                return new Impresion { Linea = linea, Palabra = t[0], Valor = t[1] };
            }

            // ---------- Línea que no empieza correctamente ----------
            if (primero.Tipo == TipoToken.Identificador)
            {
                string mayus = primero.Lexema.ToUpperInvariant();
                if (mayus is "ENTERO" or "DECIMAL" or "TEXTO" or "BOOLEANO" or "IMPRIMIR" or "MOSTRAR")
                    return Error(linea, primero.Columna, $"\"{primero.Lexema}\" no se reconoce como palabra clave.",
                        $"Las palabras clave se escriben en mayúsculas: {mayus}.");

                return Error(linea, primero.Columna, $"\"{primero.Lexema}\" no es un tipo de dato.",
                    "Una declaración empieza con ENTERO, DECIMAL, TEXTO o BOOLEANO.");
            }

            return Error(linea, primero.Columna, $"Una instrucción no puede empezar con \"{primero.Lexema}\".",
                "Empieza con un tipo de dato (ENTERO, DECIMAL, TEXTO, BOOLEANO) o con IMPRIMIR.");
        }

        /// <summary>Después de la sentencia solo puede venir un ";" opcional.</summary>
        private bool VerificarCierre(List<Token> t, int posicion, int linea)
        {
            if (posicion < t.Count && t[posicion].Tipo == TipoToken.FinSentencia) posicion++;
            if (posicion < t.Count)
            {
                Error(linea, t[posicion].Columna, $"Sobra \"{t[posicion].Lexema}\" al final de la instrucción.",
                    "Escribe una sola instrucción por línea.");
                return false;
            }
            return true;
        }

        private Sentencia? Error(int linea, int columna, string mensaje, string sugerencia)
        {
            Errores.Add(new ErrorCompilacion(Fase.Sintactico, linea, columna, mensaje, sugerencia));
            return null;
        }
    }
}
