namespace CompiladorCalculadora.Lexico
{
    /// <summary>
    /// FASE 1 · Análisis léxico.
    /// Recorre el código carácter por carácter y lo divide en tokens.
    /// Si encuentra algo que no pertenece al lenguaje, registra un error léxico y sigue leyendo.
    /// </summary>
    public class AnalizadorLexico
    {
        private static readonly Dictionary<string, TipoToken> PalabrasClave = new()
        {
            ["ENTERO"] = TipoToken.TipoDato,
            ["DECIMAL"] = TipoToken.TipoDato,
            ["IMPRIMIR"] = TipoToken.Imprimir,
            ["MOSTRAR"] = TipoToken.Imprimir,
            ["RAIZ"] = TipoToken.Funcion,
            ["ABS"] = TipoToken.Funcion
        };

        private static readonly Dictionary<char, TipoToken> Simbolos = new()
        {
            ['='] = TipoToken.Asignacion,
            ['+'] = TipoToken.Suma,
            ['-'] = TipoToken.Resta,
            ['*'] = TipoToken.Multiplicacion,
            ['/'] = TipoToken.Division,
            ['%'] = TipoToken.Modulo,
            ['^'] = TipoToken.Potencia,
            ['('] = TipoToken.ParenAbre,
            [')'] = TipoToken.ParenCierra,
            [','] = TipoToken.Coma,
            [';'] = TipoToken.FinSentencia
        };

        public List<Token> Tokens { get; } = new();
        public List<ErrorCompilacion> Errores { get; } = new();

        public static bool EsPalabraClave(string palabra) => PalabrasClave.ContainsKey(palabra);

        public void Analizar(string codigo)
        {
            string[] lineas = codigo.Replace("\r", "").Split('\n');
            for (int i = 0; i < lineas.Length; i++)
                AnalizarLinea(lineas[i], i + 1);
        }

        private void AnalizarLinea(string texto, int linea)
        {
            int c = 0;
            while (c < texto.Length)
            {
                char ch = texto[c];
                int columna = c + 1;

                if (char.IsWhiteSpace(ch)) { c++; continue; }

                // Comentario: // hasta el final de la línea
                if (ch == '/' && c + 1 < texto.Length && texto[c + 1] == '/') break;

                // Palabra clave o identificador
                if (char.IsLetter(ch) || ch == '_')
                {
                    int ini = c;
                    while (c < texto.Length && (char.IsLetterOrDigit(texto[c]) || texto[c] == '_')) c++;
                    string palabra = texto[ini..c];
                    var tipo = PalabrasClave.TryGetValue(palabra, out var t) ? t : TipoToken.Identificador;
                    Tokens.Add(new Token(tipo, palabra, linea, columna));
                    continue;
                }

                // Número entero o decimal (punto como separador)
                if (char.IsDigit(ch))
                {
                    int ini = c;
                    while (c < texto.Length && char.IsDigit(texto[c])) c++;
                    bool esDecimal = false;
                    if (c < texto.Length && texto[c] == '.')
                    {
                        if (c + 1 < texto.Length && char.IsDigit(texto[c + 1]))
                        {
                            esDecimal = true;
                            c++;
                            while (c < texto.Length && char.IsDigit(texto[c])) c++;
                        }
                        else
                        {
                            c++;
                            Error(linea, columna, $"El número \"{texto[ini..c]}\" está incompleto.",
                                  $"Escribe al menos un dígito después del punto, por ejemplo {texto[ini..(c - 1)]}.0");
                            Tokens.Add(new Token(TipoToken.Desconocido, texto[ini..c], linea, columna));
                            continue;
                        }
                    }
                    if (c < texto.Length && (char.IsLetter(texto[c]) || texto[c] == '_'))
                    {
                        while (c < texto.Length && (char.IsLetterOrDigit(texto[c]) || texto[c] == '_')) c++;
                        Error(linea, columna, $"\"{texto[ini..c]}\" no es un identificador válido.",
                              "Un nombre de variable no puede empezar con un número. Si es una multiplicación, escribe el operador: 2 * a");
                        Tokens.Add(new Token(TipoToken.Desconocido, texto[ini..c], linea, columna));
                        continue;
                    }
                    Tokens.Add(new Token(esDecimal ? TipoToken.NumeroDecimal : TipoToken.NumeroEntero, texto[ini..c], linea, columna));
                    continue;
                }

                // Texto entre comillas dobles (solo para las etiquetas de IMPRIMIR)
                if (ch == '"')
                {
                    int cierre = texto.IndexOf('"', c + 1);
                    if (cierre == -1)
                    {
                        Error(linea, columna, "Texto sin cerrar: falta la comilla doble final.", "Cierra el texto con \" al final.");
                        Tokens.Add(new Token(TipoToken.Desconocido, texto[c..], linea, columna));
                        return;
                    }
                    Tokens.Add(new Token(TipoToken.Texto, texto[c..(cierre + 1)], linea, columna));
                    c = cierre + 1;
                    continue;
                }

                // Operadores y signos de puntuación
                if (Simbolos.TryGetValue(ch, out var simbolo))
                {
                    Tokens.Add(new Token(simbolo, ch.ToString(), linea, columna));
                    c++;
                    continue;
                }

                // Cualquier otro carácter no pertenece al lenguaje
                string pista = ch switch
                {
                    '×' => "Para multiplicar usa *.",
                    '÷' or ':' => "Para dividir usa /.",
                    '.' => "Un número decimal empieza con un dígito: escribe 0.5 en lugar de .5",
                    '[' or '{' => "Para agrupar operaciones usa paréntesis ( ).",
                    _ => "Los operadores válidos son + - * / % ^ y los paréntesis ( )."
                };
                Error(linea, columna, $"Símbolo no reconocido: '{ch}'.", pista);
                Tokens.Add(new Token(TipoToken.Desconocido, ch.ToString(), linea, columna));
                c++;
            }
        }

        private void Error(int linea, int columna, string mensaje, string sugerencia) =>
            Errores.Add(new ErrorCompilacion(Fase.Lexico, linea, columna, mensaje, sugerencia));
    }
}
