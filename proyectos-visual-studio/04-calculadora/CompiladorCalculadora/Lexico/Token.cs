namespace CompiladorCalculadora.Lexico
{
    /// <summary>
    /// Categorías léxicas (tipos de token) que reconoce la calculadora.
    /// </summary>
    public enum TipoToken
    {
        TipoDato,        // ENTERO, DECIMAL
        Imprimir,        // IMPRIMIR o MOSTRAR
        Funcion,         // RAIZ, ABS
        Identificador,   // a, b, total...
        NumeroEntero,    // 12
        NumeroDecimal,   // 4.5
        Texto,           // "Suma:"
        Asignacion,      // =
        Suma,            // +
        Resta,           // -
        Multiplicacion,  // *
        Division,        // /
        Modulo,          // %
        Potencia,        // ^
        ParenAbre,       // (
        ParenCierra,     // )
        Coma,            // ,
        FinSentencia,    // ; (opcional)
        Desconocido      // cualquier símbolo que no pertenece al lenguaje
    }

    /// <summary>
    /// Unidad léxica: el tipo de token, el texto original (lexema) y su posición.
    /// </summary>
    public class Token
    {
        public TipoToken Tipo { get; }
        public string Lexema { get; }
        public int Linea { get; }
        public int Columna { get; }

        public Token(TipoToken tipo, string lexema, int linea, int columna)
        {
            Tipo = tipo;
            Lexema = lexema;
            Linea = linea;
            Columna = columna;
        }

        /// <summary>Columna siguiente al último carácter del token.</summary>
        public int ColumnaFinal => Columna + Lexema.Length;

        /// <summary>Nombre del token con el formato que se muestra en la landing page.</summary>
        public string Nombre => Tipo switch
        {
            TipoToken.TipoDato => "TOKEN_TIPO",
            TipoToken.Imprimir => "TOKEN_IMPRIMIR",
            TipoToken.Funcion => "TOKEN_FUNCION",
            TipoToken.Identificador => "TOKEN_IDENTIFICADOR",
            TipoToken.NumeroEntero => "TOKEN_NUMERO",
            TipoToken.NumeroDecimal => "TOKEN_DECIMAL",
            TipoToken.Texto => "TOKEN_TEXTO",
            TipoToken.Asignacion => "TOKEN_ASIGNACION",
            TipoToken.Suma => "TOKEN_SUMA",
            TipoToken.Resta => "TOKEN_RESTA",
            TipoToken.Multiplicacion => "TOKEN_MULTIPLICACION",
            TipoToken.Division => "TOKEN_DIVISION",
            TipoToken.Modulo => "TOKEN_MODULO",
            TipoToken.Potencia => "TOKEN_POTENCIA",
            TipoToken.ParenAbre => "TOKEN_PAREN_ABRE",
            TipoToken.ParenCierra => "TOKEN_PAREN_CIERRA",
            TipoToken.Coma => "TOKEN_COMA",
            TipoToken.FinSentencia => "TOKEN_FIN",
            _ => "TOKEN_DESCONOCIDO"
        };

        public override string ToString() => $"{Nombre}({Lexema})";
    }
}
