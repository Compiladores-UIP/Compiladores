namespace CompiladorConsultas.Lexico
{
    /// <summary>
    /// FASE 1 · Análisis léxico.
    /// Convierte cada línea en tokens: palabras clave, nombres, operadores y valores.
    /// Los comentarios empiezan con // y se ignoran.
    /// </summary>
    public class AnalizadorLexico
    {
        private static readonly Dictionary<string, TipoToken> PalabrasClave = new()
        {
            { "BUSCAR", TipoToken.Buscar },
            { "DE", TipoToken.De },
            { "DONDE", TipoToken.Donde },
            { "Y", TipoToken.Y },
            { "O", TipoToken.O },
            { "ORDENAR", TipoToken.Ordenar },
            { "POR", TipoToken.Por },
            { "ASC", TipoToken.Asc },
            { "DESC", TipoToken.Desc }
        };

        public List<Token> Tokens { get; } = new();
        public List<ErrorCompilacion> Errores { get; } = new();

        public void Analizar(string codigo)
        {
            string[] lineas = codigo.Replace("\r", "").Split('\n');
            for (int i = 0; i < lineas.Length; i++) AnalizarLinea(lineas[i], i + 1);
        }

        private void AnalizarLinea(string texto, int linea)
        {
            int c = 0;
            while (c < texto.Length)
            {
                char actual = texto[c];
                int col = c + 1;

                if (char.IsWhiteSpace(actual)) { c++; continue; }
                if (actual == '/' && c + 1 < texto.Length && texto[c + 1] == '/') break;

                // Palabras clave o identificadores
                if (char.IsLetter(actual) || actual == '_')
                {
                    int inicio = c;
                    while (c < texto.Length && (char.IsLetterOrDigit(texto[c]) || texto[c] == '_')) c++;
                    string palabra = texto[inicio..c];
                    Agregar(PalabrasClave.TryGetValue(palabra, out var tipo) ? tipo : TipoToken.Identificador, palabra, linea, col);
                    continue;
                }

                // Números
                if (char.IsDigit(actual))
                {
                    int inicio = c;
                    while (c < texto.Length && char.IsDigit(texto[c])) c++;
                    bool dec = false;
                    if (c + 1 < texto.Length && texto[c] == '.' && char.IsDigit(texto[c + 1]))
                    {
                        dec = true; c++;
                        while (c < texto.Length && char.IsDigit(texto[c])) c++;
                    }
                    if (c < texto.Length && char.IsLetter(texto[c]))
                    {
                        while (c < texto.Length && char.IsLetterOrDigit(texto[c])) c++;
                        Errores.Add(new ErrorCompilacion(Fase.Lexico, linea, col,
                            $"\"{texto[inicio..c]}\" no es válido: un nombre no puede empezar con un número.",
                            "Revisa que haya un espacio entre el número y la palabra siguiente."));
                        Agregar(TipoToken.Desconocido, texto[inicio..c], linea, col);
                        continue;
                    }
                    Agregar(dec ? TipoToken.NumeroDecimal : TipoToken.NumeroEntero, texto[inicio..c], linea, col);
                    continue;
                }

                // Textos entre comillas dobles
                if (actual == '"')
                {
                    int cierre = texto.IndexOf('"', c + 1);
                    if (cierre == -1)
                    {
                        Errores.Add(new ErrorCompilacion(Fase.Lexico, linea, col,
                            "Texto sin cerrar: falta la comilla doble final.", "Cierra el texto con \" al final."));
                        Agregar(TipoToken.Desconocido, texto[c..], linea, col);
                        break;
                    }
                    Agregar(TipoToken.Texto, texto[c..(cierre + 1)], linea, col);
                    c = cierre + 1;
                    continue;
                }

                // Operadores de comparación de uno o dos caracteres
                if (actual is '>' or '<' or '=' or '!')
                {
                    string dos = c + 1 < texto.Length ? texto.Substring(c, 2) : "";
                    if (dos is ">=" or "<=" or "<>" or "!=")
                    {
                        Agregar(TipoToken.Operador, dos, linea, col); c += 2; continue;
                    }
                    if (actual != '!')
                    {
                        Agregar(TipoToken.Operador, actual.ToString(), linea, col); c++; continue;
                    }
                }

                if (actual == ',') { Agregar(TipoToken.Coma, ",", linea, col); c++; continue; }
                if (actual == ';') { Agregar(TipoToken.FinSentencia, ";", linea, col); c++; continue; }

                Errores.Add(new ErrorCompilacion(Fase.Lexico, linea, col,
                    $"Símbolo no reconocido: '{actual}'.",
                    "Solo se admiten palabras, números, textos entre comillas, comas y los operadores > < >= <= = <>."));
                Agregar(TipoToken.Desconocido, actual.ToString(), linea, col);
                c++;
            }
        }

        private void Agregar(TipoToken tipo, string lexema, int linea, int col) =>
            Tokens.Add(new Token(tipo, lexema, linea, col));
    }
}
