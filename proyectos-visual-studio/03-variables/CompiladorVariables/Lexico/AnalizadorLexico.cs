namespace CompiladorVariables.Lexico
{
    /// <summary>
    /// FASE 1 · Análisis léxico.
    /// Recorre el código carácter por carácter y lo convierte en una lista de tokens.
    /// Los comentarios empiezan con // y se ignoran.
    /// </summary>
    public class AnalizadorLexico
    {
        private static readonly HashSet<string> TiposDato = new() { "ENTERO", "DECIMAL", "TEXTO", "BOOLEANO" };
        private static readonly HashSet<string> PalabrasImprimir = new() { "IMPRIMIR", "MOSTRAR" };

        public List<Token> Tokens { get; } = new();
        public List<ErrorCompilacion> Errores { get; } = new();

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
                char actual = texto[c];
                int columna = c + 1;

                // Espacios en blanco
                if (char.IsWhiteSpace(actual)) { c++; continue; }

                // Comentario hasta el final de la línea
                if (actual == '/' && c + 1 < texto.Length && texto[c + 1] == '/') break;

                // Palabras: tipos, IMPRIMIR, VERDADERO/FALSO o identificadores
                if (char.IsLetter(actual) || actual == '_')
                {
                    int inicio = c;
                    while (c < texto.Length && (char.IsLetterOrDigit(texto[c]) || texto[c] == '_')) c++;
                    string palabra = texto[inicio..c];

                    if (TiposDato.Contains(palabra)) Agregar(TipoToken.TipoDato, palabra, linea, columna);
                    else if (PalabrasImprimir.Contains(palabra)) Agregar(TipoToken.Imprimir, palabra, linea, columna);
                    else if (palabra is "VERDADERO" or "FALSO") Agregar(TipoToken.ValorBooleano, palabra, linea, columna);
                    else Agregar(TipoToken.Identificador, palabra, linea, columna);
                    continue;
                }

                // Números enteros y decimales (se usa punto como separador decimal)
                if (char.IsDigit(actual))
                {
                    int inicio = c;
                    while (c < texto.Length && char.IsDigit(texto[c])) c++;
                    bool esDecimal = false;
                    if (c + 1 < texto.Length && texto[c] == '.' && char.IsDigit(texto[c + 1]))
                    {
                        esDecimal = true;
                        c++;
                        while (c < texto.Length && char.IsDigit(texto[c])) c++;
                    }

                    // Un número pegado a letras (ej. 2dias) no es un identificador válido
                    if (c < texto.Length && (char.IsLetter(texto[c]) || texto[c] == '_'))
                    {
                        while (c < texto.Length && (char.IsLetterOrDigit(texto[c]) || texto[c] == '_')) c++;
                        string malo = texto[inicio..c];
                        Errores.Add(new ErrorCompilacion(Fase.Lexico, linea, columna,
                            $"\"{malo}\" no es un identificador válido: no puede empezar con un número.",
                            "Empieza el nombre con una letra, por ejemplo: dias2."));
                        Agregar(TipoToken.Desconocido, malo, linea, columna);
                        continue;
                    }

                    string numero = texto[inicio..c];
                    Agregar(esDecimal ? TipoToken.ValorDecimal : TipoToken.ValorEntero, numero, linea, columna);
                    continue;
                }

                // Cadenas de texto entre comillas dobles
                if (actual == '"')
                {
                    int cierre = texto.IndexOf('"', c + 1);
                    if (cierre == -1)
                    {
                        Errores.Add(new ErrorCompilacion(Fase.Lexico, linea, columna,
                            "Texto sin cerrar: falta la comilla doble final.",
                            "Cierra el texto con \" al final."));
                        Agregar(TipoToken.Desconocido, texto[c..], linea, columna);
                        break;
                    }
                    Agregar(TipoToken.ValorTexto, texto[c..(cierre + 1)], linea, columna);
                    c = cierre + 1;
                    continue;
                }

                if (actual == '=') { Agregar(TipoToken.Asignacion, "=", linea, columna); c++; continue; }
                if (actual == ';') { Agregar(TipoToken.FinSentencia, ";", linea, columna); c++; continue; }

                // Cualquier otro símbolo no pertenece al lenguaje
                Errores.Add(new ErrorCompilacion(Fase.Lexico, linea, columna,
                    $"Símbolo no reconocido: '{actual}'.",
                    "El lenguaje solo admite letras, números, =, ; y textos entre comillas."));
                Agregar(TipoToken.Desconocido, actual.ToString(), linea, columna);
                c++;
            }
        }

        private void Agregar(TipoToken tipo, string lexema, int linea, int columna) =>
            Tokens.Add(new Token(tipo, lexema, linea, columna));
    }
}
