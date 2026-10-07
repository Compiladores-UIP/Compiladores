using System.Text;

namespace MensajesPersonalizados.Compilador;

/// <summary>
/// Fase 1. Recorre el código carácter por carácter y lo divide en tokens.
/// Las palabras reservadas se escriben en mayúsculas.
/// </summary>
public sealed class AnalizadorLexico
{
    public static readonly HashSet<string> PalabrasReservadas = new(StringComparer.Ordinal)
    {
        "TEXTO", "ENTERO", "DECIMAL", "MOSTRAR", "IMPRIMIR", "PEDIR", "LEER"
    };

    public static readonly HashSet<string> Funciones = new(StringComparer.Ordinal)
    {
        "MAYUSCULAS", "MINUSCULAS", "LONGITUD"
    };

    private readonly string _fuente;
    private int _pos;
    private int _linea = 1;
    private int _columna = 1;

    public AnalizadorLexico(string fuente)
    {
        _fuente = fuente.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    public List<Token> Analizar()
    {
        var tokens = new List<Token>();

        while (!FinFuente)
        {
            char c = Actual;

            if (c == ' ' || c == '\t')
            {
                Avanzar();
                continue;
            }

            if (c == '#' || (c == '/' && Siguiente == '/'))
            {
                while (!FinFuente && Actual != '\n')
                {
                    Avanzar();
                }
                continue;
            }

            if (c == '\n')
            {
                // Se evita repetir saltos de línea: una línea vacía no produce una instrucción.
                if (tokens.Count > 0 && tokens[^1].Tipo != TipoToken.FIN_LINEA)
                {
                    tokens.Add(new Token(TipoToken.FIN_LINEA, "\\n", "", _linea, _columna));
                }
                Avanzar();
                continue;
            }

            if (c == '"')
            {
                tokens.Add(LeerCadena());
                continue;
            }

            if (char.IsAsciiDigit(c))
            {
                tokens.Add(LeerNumero());
                continue;
            }

            if (char.IsAsciiLetter(c) || c == '_')
            {
                tokens.Add(LeerPalabra());
                continue;
            }

            int linea = _linea, columna = _columna;
            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                    Avanzar();
                    tokens.Add(new Token(TipoToken.OPERADOR, c.ToString(), c.ToString(), linea, columna));
                    break;
                case '=':
                    Avanzar();
                    tokens.Add(new Token(TipoToken.ASIGNACION, "=", "=", linea, columna));
                    break;
                case '(':
                    Avanzar();
                    tokens.Add(new Token(TipoToken.PARENTESIS_IZQ, "(", "(", linea, columna));
                    break;
                case ')':
                    Avanzar();
                    tokens.Add(new Token(TipoToken.PARENTESIS_DER, ")", ")", linea, columna));
                    break;
                default:
                    throw new ErrorCompilacion(FaseError.Lexico,
                        $"el símbolo '{c}' no pertenece al lenguaje.", linea, columna);
            }
        }

        if (tokens.Count > 0 && tokens[^1].Tipo != TipoToken.FIN_LINEA)
        {
            tokens.Add(new Token(TipoToken.FIN_LINEA, "\\n", "", _linea, _columna));
        }
        tokens.Add(new Token(TipoToken.FIN_ARCHIVO, "<fin>", "", _linea, _columna));
        return tokens;
    }

    private Token LeerCadena()
    {
        int linea = _linea, columna = _columna, inicio = _pos;
        var valor = new StringBuilder();
        Avanzar(); // comilla de apertura

        while (true)
        {
            if (FinFuente || Actual == '\n')
            {
                throw new ErrorCompilacion(FaseError.Lexico,
                    "la cadena no se cerró con comillas dobles antes del fin de línea.", linea, columna);
            }

            char c = Actual;
            if (c == '"')
            {
                Avanzar();
                break;
            }

            if (c == '\\')
            {
                int colEscape = _columna;
                Avanzar();
                if (FinFuente || Actual == '\n')
                {
                    throw new ErrorCompilacion(FaseError.Lexico,
                        "la cadena termina con una barra invertida incompleta.", _linea, colEscape);
                }

                char escape = Actual;
                valor.Append(escape switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    't' => '\t',
                    _ => throw new ErrorCompilacion(FaseError.Lexico,
                        $"la secuencia de escape '\\{escape}' no es válida. Use \\\", \\\\, \\n o \\t.", _linea, colEscape)
                });
                Avanzar();
                continue;
            }

            valor.Append(c);
            Avanzar();
        }

        string lexema = _fuente[inicio.._pos];
        return new Token(TipoToken.CADENA, lexema, valor.ToString(), linea, columna);
    }

    private Token LeerNumero()
    {
        int linea = _linea, columna = _columna, inicio = _pos;
        while (!FinFuente && char.IsAsciiDigit(Actual))
        {
            Avanzar();
        }

        bool esDecimal = false;
        if (!FinFuente && Actual == '.' && Siguiente is char s && char.IsAsciiDigit(s))
        {
            esDecimal = true;
            Avanzar();
            while (!FinFuente && char.IsAsciiDigit(Actual))
            {
                Avanzar();
            }
        }

        if (!FinFuente && (char.IsAsciiLetter(Actual) || Actual == '_'))
        {
            throw new ErrorCompilacion(FaseError.Lexico,
                $"'{_fuente[inicio..(_pos + 1)]}' no es un número ni un nombre válido: los nombres no pueden empezar con un dígito.",
                linea, columna);
        }

        string lexema = _fuente[inicio.._pos];
        if (!esDecimal && !int.TryParse(lexema, out _))
        {
            throw new ErrorCompilacion(FaseError.Lexico,
                $"el número {lexema} supera el límite de ENTERO ({int.MaxValue}).", linea, columna);
        }

        return new Token(esDecimal ? TipoToken.NUMERO_DECIMAL : TipoToken.NUMERO_ENTERO, lexema, lexema, linea, columna);
    }

    private Token LeerPalabra()
    {
        int linea = _linea, columna = _columna, inicio = _pos;
        while (!FinFuente && (char.IsAsciiLetterOrDigit(Actual) || Actual == '_'))
        {
            Avanzar();
        }

        if (!FinFuente && char.IsLetter(Actual))
        {
            throw new ErrorCompilacion(FaseError.Lexico,
                $"el carácter '{Actual}' no se admite en nombres; use solo letras sin tilde, dígitos y _.", _linea, _columna);
        }

        string lexema = _fuente[inicio.._pos];
        TipoToken tipo = PalabrasReservadas.Contains(lexema) ? TipoToken.PALABRA_RESERVADA
            : Funciones.Contains(lexema) ? TipoToken.FUNCION
            : TipoToken.IDENTIFICADOR;
        return new Token(tipo, lexema, lexema, linea, columna);
    }

    private bool FinFuente => _pos >= _fuente.Length;
    private char Actual => _fuente[_pos];
    private char? Siguiente => _pos + 1 < _fuente.Length ? _fuente[_pos + 1] : null;

    private void Avanzar()
    {
        if (_fuente[_pos] == '\n')
        {
            _linea++;
            _columna = 1;
        }
        else
        {
            _columna++;
        }
        _pos++;
    }
}
