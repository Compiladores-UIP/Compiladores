using System.Text;

namespace CompiladorConfiguracion.Lexico
{
    /// <summary>
    /// FASE 1 · Análisis léxico.
    /// Recorre el archivo carácter por carácter y lo divide en tokens.
    /// Si encuentra algo que no pertenece al lenguaje, registra un error léxico y sigue leyendo.
    /// </summary>
    public class AnalizadorLexico
    {
        private static readonly Dictionary<string, TipoToken> PalabrasClave = new()
        {
            ["FORMATO"] = TipoToken.Formato,
            ["JSON"] = TipoToken.NombreFormato,
            ["XML"] = TipoToken.NombreFormato,
            ["CONFIG"] = TipoToken.Config,
            ["SECCION"] = TipoToken.Seccion,
            ["FINSECCION"] = TipoToken.FinSeccion,
            ["TEXTO"] = TipoToken.Tipo,
            ["ENTERO"] = TipoToken.Tipo,
            ["DECIMAL"] = TipoToken.Tipo,
            ["BOOLEANO"] = TipoToken.Tipo,
            ["LISTA"] = TipoToken.Tipo,
            ["verdadero"] = TipoToken.Booleano,
            ["falso"] = TipoToken.Booleano,
            ["VERDADERO"] = TipoToken.Booleano,
            ["FALSO"] = TipoToken.Booleano
        };

        private static readonly Dictionary<char, TipoToken> Simbolos = new()
        {
            ['='] = TipoToken.Asignacion,
            [':'] = TipoToken.DosPuntos,
            ['['] = TipoToken.CorcheteAbre,
            [']'] = TipoToken.CorcheteCierra,
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

                // Comentarios: // o # hasta el final de la línea
                if (ch == '#' || (ch == '/' && c + 1 < texto.Length && texto[c + 1] == '/')) break;

                // Palabra clave, booleano o clave
                if (char.IsLetter(ch) || ch == '_')
                {
                    int ini = c;
                    while (c < texto.Length && (char.IsLetterOrDigit(texto[c]) || texto[c] == '_')) c++;
                    string palabra = texto[ini..c];
                    var tipo = PalabrasClave.TryGetValue(palabra, out var t) ? t : TipoToken.Clave;
                    Tokens.Add(new Token(tipo, palabra, linea, columna));
                    continue;
                }

                // Número (puede llevar signo negativo)
                if (char.IsDigit(ch) || (ch == '-' && c + 1 < texto.Length && char.IsDigit(texto[c + 1])))
                {
                    int ini = c;
                    if (ch == '-') c++;
                    while (c < texto.Length && char.IsDigit(texto[c])) c++;
                    bool esDecimal = false;
                    if (c < texto.Length && texto[c] == '.' && c + 1 < texto.Length && char.IsDigit(texto[c + 1]))
                    {
                        esDecimal = true;
                        c++;
                        while (c < texto.Length && char.IsDigit(texto[c])) c++;
                    }
                    if (c < texto.Length && (char.IsLetter(texto[c]) || texto[c] == '_' || texto[c] == '.'))
                    {
                        while (c < texto.Length && !char.IsWhiteSpace(texto[c]) && texto[c] != ',' && texto[c] != ']') c++;
                        string malo = texto[ini..c];
                        Error(linea, columna, $"\"{malo}\" no es un valor válido.",
                              char.IsDigit(malo[0]) && malo.Any(char.IsLetter)
                                ? "Una clave no puede empezar con un número. Si es un texto, escríbelo entre comillas."
                                : "Un número decimal lleva un solo punto y dígitos a ambos lados: 1.5");
                        Tokens.Add(new Token(TipoToken.Desconocido, malo, linea, columna));
                        continue;
                    }
                    Tokens.Add(new Token(esDecimal ? TipoToken.Decimal : TipoToken.Entero, texto[ini..c], linea, columna));
                    continue;
                }

                // Texto entre comillas dobles, con secuencias de escape
                if (ch == '"')
                {
                    if (!LeerTexto(texto, ref c, linea)) return;
                    continue;
                }

                // Texto con comillas simples: un solo error para todo el texto
                if (ch == '\'')
                {
                    int cierre = texto.IndexOf('\'', c + 1);
                    int fin = cierre == -1 ? texto.Length : cierre + 1;
                    string malo = texto[c..fin];
                    Error(linea, columna, $"Los textos van entre comillas dobles, no simples: {malo}",
                          $"Escribe \"{malo.Trim('\'')}\"");
                    Tokens.Add(new Token(TipoToken.Desconocido, malo, linea, columna));
                    c = fin;
                    continue;
                }

                if (Simbolos.TryGetValue(ch, out var simbolo))
                {
                    Tokens.Add(new Token(simbolo, ch.ToString(), linea, columna));
                    c++;
                    continue;
                }

                string pista = ch switch
                {
                    '{' or '}' => "Para agrupar claves usa SECCION nombre … FINSECCION.",
                    '-' => "Las claves solo pueden tener letras, números y _. Usa nombre_compuesto.",
                    '.' => "Un número decimal empieza con un dígito: 0.5",
                    _ => "Los símbolos válidos son = : [ ] , y las comillas dobles."
                };
                Error(linea, columna, $"Símbolo no reconocido: '{ch}'.", pista);
                Tokens.Add(new Token(TipoToken.Desconocido, ch.ToString(), linea, columna));
                c++;
            }
        }

        /// <summary>Lee un texto desde la comilla inicial. Devuelve false si la línea terminó sin cerrarlo.</summary>
        private bool LeerTexto(string texto, ref int c, int linea)
        {
            int ini = c, columna = c + 1;
            var valor = new StringBuilder();
            c++;
            while (c < texto.Length && texto[c] != '"')
            {
                if (texto[c] == '\\')
                {
                    if (c + 1 >= texto.Length) break;
                    char sig = texto[c + 1];
                    string? traducido = sig switch { '"' => "\"", '\\' => "\\", 'n' => "\n", 't' => "\t", _ => null };
                    if (traducido == null)
                    {
                        Error(linea, c + 1, $"Secuencia de escape no válida: \\{sig}.", "Dentro de un texto puedes usar \\\" \\\\ \\n y \\t.");
                        // se conserva tal cual para seguir leyendo
                        traducido = "\\" + sig;
                    }
                    valor.Append(traducido);
                    c += 2;
                    continue;
                }
                valor.Append(texto[c]);
                c++;
            }
            if (c >= texto.Length)
            {
                Error(linea, columna, "Texto sin cerrar: falta la comilla doble final.", "Cierra el texto con \" al final.");
                Tokens.Add(new Token(TipoToken.Desconocido, texto[ini..], linea, columna));
                return false;
            }
            c++;
            Tokens.Add(new Token(TipoToken.Texto, texto[ini..c], linea, columna, valor.ToString()));
            return true;
        }

        private void Error(int linea, int columna, string mensaje, string sugerencia) =>
            Errores.Add(new ErrorCompilacion(Fase.Lexico, linea, columna, mensaje, sugerencia));
    }
}
