using CompiladorCalculadora.Lexico;

namespace CompiladorCalculadora.Sintactico
{
    /// <summary>
    /// FASE 2 · Análisis sintáctico (descendente recursivo).
    /// Cada regla de la gramática es un método. La precedencia de los operadores
    /// sale del orden en que se llaman: Expresion → Termino → Unario → Potencia → Primario.
    ///
    ///   &lt;expresion&gt; ::= &lt;termino&gt;  { ("+" | "-") &lt;termino&gt; }
    ///   &lt;termino&gt;   ::= &lt;unario&gt;   { ("*" | "/" | "%") &lt;unario&gt; }
    ///   &lt;unario&gt;    ::= "-" &lt;unario&gt; | &lt;potencia&gt;
    ///   &lt;potencia&gt;  ::= &lt;primario&gt; [ "^" &lt;unario&gt; ]
    ///   &lt;primario&gt;  ::= NUMERO | DECIMAL | IDENT | "(" &lt;expresion&gt; ")" | FUNCION "(" &lt;expresion&gt; ")"
    /// </summary>
    public class AnalizadorSintactico
    {
        public List<ErrorCompilacion> Errores { get; } = new();

        private List<Token> _tokens = new();
        private int _pos;
        private string _fuente = "";

        /// <summary>Error que corta el análisis de la línea actual.</summary>
        private class ErrorDeLinea : Exception
        {
            public ErrorCompilacion Error { get; }
            public ErrorDeLinea(ErrorCompilacion e) { Error = e; }
        }

        public Programa Analizar(List<Token> tokens, string[] lineasFuente, HashSet<int> lineasConErrorLexico)
        {
            var programa = new Programa();

            // Una instrucción por línea: se analiza cada línea por separado para poder
            // informar varios errores y seguir con las demás.
            foreach (var grupo in tokens.GroupBy(t => t.Linea).OrderBy(g => g.Key))
            {
                if (lineasConErrorLexico.Contains(grupo.Key)) continue;
                _tokens = grupo.ToList();
                _pos = 0;
                _fuente = lineasFuente[grupo.Key - 1].Trim();
                try
                {
                    programa.Sentencias.Add(Sentencia());
                }
                catch (ErrorDeLinea e)
                {
                    Errores.Add(e.Error);
                }
            }
            return programa;
        }

        // ------------------------------------------------------------ Sentencias

        private Sentencia Sentencia()
        {
            Token t = Actual!;
            switch (t.Tipo)
            {
                case TipoToken.TipoDato:
                {
                    Avanzar();
                    Token nombre = Esperar(TipoToken.Identificador,
                        $"Se esperaba el nombre de la variable después de {t.Lexema}",
                        "Ejemplo: ENTERO a = 10");
                    Esperar(TipoToken.Asignacion, $"Se esperaba \"=\" después de \"{nombre.Lexema}\"",
                        $"Toda variable empieza con un valor: {t.Lexema} {nombre.Lexema} = 0");
                    Expresion valor = Expresion();
                    Fin();
                    return new Declaracion { Linea = t.Linea, Fuente = _fuente, Tipo = t, Nombre = nombre, Valor = valor };
                }

                case TipoToken.Imprimir:
                {
                    Avanzar();
                    if (Actual == null || Actual.Tipo == TipoToken.FinSentencia)
                        throw Falla(t.Linea, t.ColumnaFinal, $"Falta lo que se quiere mostrar después de {t.Lexema}.",
                            $"Ejemplo: {t.Lexema} a + b   o   {t.Lexema} \"Suma:\", a + b");

                    Token? etiqueta = null;
                    Expresion? valor = null;
                    if (Actual.Tipo == TipoToken.Texto)
                    {
                        etiqueta = Avanzar();
                        if (Actual?.Tipo == TipoToken.Coma)
                        {
                            Avanzar();
                            valor = Expresion();
                        }
                        else if (Actual != null && Actual.Tipo != TipoToken.FinSentencia)
                        {
                            throw Falla(Actual.Linea, Actual.Columna, $"Falta una coma entre el texto y \"{Actual.Lexema}\".",
                                $"Escribe: {t.Lexema} {etiqueta.Lexema}, {Actual.Lexema} …");
                        }
                    }
                    else valor = Expresion();
                    Fin();
                    return new Impresion { Linea = t.Linea, Fuente = _fuente, Etiqueta = etiqueta, Valor = valor };
                }

                case TipoToken.Identificador:
                {
                    if (Siguiente?.Tipo == TipoToken.Asignacion)
                    {
                        Avanzar(); Avanzar();
                        Expresion valor = Expresion();
                        Fin();
                        return new Asignacion { Linea = t.Linea, Fuente = _fuente, Nombre = t, Valor = valor };
                    }
                    string mayus = t.Lexema.ToUpperInvariant();
                    if (AnalizadorLexico.EsPalabraClave(mayus))
                        throw Falla(t.Linea, t.Columna, $"\"{t.Lexema}\" no se reconoce como palabra clave.",
                            $"Las palabras clave van en mayúsculas: {mayus}");
                    throw Falla(t.Linea, t.ColumnaFinal, $"Se esperaba \"=\" después de \"{t.Lexema}\".",
                        $"Para mostrar el valor escribe IMPRIMIR {t.Lexema}");
                }

                default:
                    throw Falla(t.Linea, t.Columna, $"Una instrucción no puede empezar con \"{t.Lexema}\".",
                        "Empieza con ENTERO, DECIMAL, IMPRIMIR / MOSTRAR o el nombre de una variable.");
            }
        }

        /// <summary>El \";\" final es opcional; después no puede quedar nada.</summary>
        private void Fin()
        {
            if (Actual?.Tipo == TipoToken.FinSentencia) Avanzar();
            if (Actual == null) return;

            Token t = Actual;
            Token? previo = _pos > 0 ? _tokens[_pos - 1] : null;
            if (t.Tipo == TipoToken.Coma && previo?.Tipo == TipoToken.NumeroEntero && Siguiente?.Tipo == TipoToken.NumeroEntero)
                throw Falla(t.Linea, t.Columna, $"\"{previo.Lexema},{Siguiente.Lexema}\" no es un número válido.",
                    $"El separador decimal es el punto: {previo.Lexema}.{Siguiente.Lexema}");
            if (t.Tipo == TipoToken.ParenCierra)
                throw Falla(t.Linea, t.Columna, "Sobra un paréntesis de cierre \")\".", "Cada \")\" debe tener su \"(\".");
            if (t.Tipo is TipoToken.Identificador or TipoToken.NumeroEntero or TipoToken.NumeroDecimal or TipoToken.ParenAbre)
                throw Falla(t.Linea, t.Columna, $"Falta un operador antes de \"{t.Lexema}\".",
                    "Une los valores con + - * / % o ^. Para multiplicar escribe 2 * a, no 2a ni 2(a).");
            throw Falla(t.Linea, t.Columna, $"Sobra \"{t.Lexema}\" al final de la instrucción.", "Escribe una sola instrucción por línea.");
        }

        // ------------------------------------------------------------ Expresiones

        private Expresion Expresion()
        {
            Expresion izq = Termino();
            while (Actual?.Tipo is TipoToken.Suma or TipoToken.Resta)
            {
                Token op = Avanzar();
                izq = new Binaria { Token = op, Izquierda = izq, Derecha = Termino() };
            }
            return izq;
        }

        private Expresion Termino()
        {
            Expresion izq = Unario();
            while (Actual?.Tipo is TipoToken.Multiplicacion or TipoToken.Division or TipoToken.Modulo)
            {
                Token op = Avanzar();
                izq = new Binaria { Token = op, Izquierda = izq, Derecha = Unario() };
            }
            return izq;
        }

        private Expresion Unario()
        {
            if (Actual?.Tipo == TipoToken.Resta)
            {
                Token op = Avanzar();
                return new Negacion { Token = op, Operando = Unario() };
            }
            return Potencia();
        }

        private Expresion Potencia()
        {
            Expresion baseExp = Primario();
            if (Actual?.Tipo == TipoToken.Potencia)
            {
                Token op = Avanzar();
                return new Binaria { Token = op, Izquierda = baseExp, Derecha = Unario() }; // asociativa a la derecha
            }
            return baseExp;
        }

        private Expresion Primario()
        {
            Token? t = Actual;
            Token previo = _tokens[Math.Max(0, _pos - 1)];

            if (t == null || t.Tipo == TipoToken.FinSentencia)
                throw Falla(previo.Linea, previo.ColumnaFinal, $"Falta un valor después de \"{previo.Lexema}\".",
                    "Completa la operación con un número, una variable o una expresión entre paréntesis.");

            switch (t.Tipo)
            {
                case TipoToken.NumeroEntero:
                case TipoToken.NumeroDecimal:
                    Avanzar();
                    return new Literal { Token = t };

                case TipoToken.Identificador:
                    Avanzar();
                    return new Variable { Token = t };

                case TipoToken.ParenAbre:
                {
                    Avanzar();
                    Expresion interior = Expresion();
                    if (Actual?.Tipo != TipoToken.ParenCierra)
                    {
                        Token ult = Actual ?? _tokens[_pos - 1];
                        throw Falla(ult.Linea, Actual == null ? ult.ColumnaFinal : ult.Columna,
                            $"Falta cerrar el paréntesis abierto en la columna {t.Columna}.", "Agrega \")\" donde termina la operación agrupada.");
                    }
                    Avanzar();
                    return new Grupo { Token = t, Interior = interior };
                }

                case TipoToken.Funcion:
                {
                    Avanzar();
                    if (Actual?.Tipo != TipoToken.ParenAbre)
                        throw Falla(t.Linea, t.ColumnaFinal, $"{t.Lexema} necesita su valor entre paréntesis.", $"Ejemplo: {t.Lexema}(16)");
                    Token abre = Avanzar();
                    Expresion arg = Expresion();
                    if (Actual?.Tipo != TipoToken.ParenCierra)
                    {
                        Token ult = Actual ?? _tokens[_pos - 1];
                        throw Falla(ult.Linea, Actual == null ? ult.ColumnaFinal : ult.Columna,
                            $"Falta cerrar el paréntesis de {t.Lexema} abierto en la columna {abre.Columna}.", $"Ejemplo: {t.Lexema}(a + b)");
                    }
                    Avanzar();
                    return new Llamada { Token = t, Argumento = arg };
                }

                case TipoToken.Texto:
                    throw Falla(t.Linea, t.Columna, "Los textos no se pueden usar en una operación.",
                        "Para mostrar un texto con un resultado escribe: IMPRIMIR \"Suma:\", a + b");

                default:
                    if (t.Tipo is TipoToken.Suma or TipoToken.Multiplicacion or TipoToken.Division or TipoToken.Modulo or TipoToken.Potencia)
                        throw Falla(t.Linea, t.Columna, $"Falta un valor antes de \"{t.Lexema}\".",
                            "Hay dos operadores seguidos o la operación empieza con un operador.");
                    throw Falla(t.Linea, t.Columna, $"Se esperaba un número, una variable o \"(\" y se encontró \"{t.Lexema}\".", "");
            }
        }

        // ------------------------------------------------------------ Utilidades

        private Token? Actual => _pos < _tokens.Count ? _tokens[_pos] : null;
        private Token? Siguiente => _pos + 1 < _tokens.Count ? _tokens[_pos + 1] : null;
        private Token Avanzar() => _tokens[_pos++];

        private Token Esperar(TipoToken tipo, string mensaje, string sugerencia)
        {
            if (Actual?.Tipo == tipo) return Avanzar();
            Token referencia = Actual ?? _tokens[_pos - 1];
            string encontrado = Actual == null ? " y la línea terminó" : $" y se encontró \"{Actual.Lexema}\"";
            throw Falla(referencia.Linea, Actual == null ? referencia.ColumnaFinal : referencia.Columna, mensaje + encontrado + ".", sugerencia);
        }

        private static ErrorDeLinea Falla(int linea, int columna, string mensaje, string sugerencia) =>
            new(new ErrorCompilacion(Fase.Sintactico, linea, columna, mensaje, sugerencia));
    }
}
