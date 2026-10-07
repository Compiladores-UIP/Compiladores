namespace CompiladorVariables.Lexico
{
    /// <summary>
    /// Categorías léxicas (tipos de token) que reconoce el compilador.
    /// </summary>
    public enum TipoToken
    {
        TipoDato,       // ENTERO, DECIMAL, TEXTO, BOOLEANO
        Imprimir,       // IMPRIMIR o MOSTRAR
        Identificador,  // edad, nombre, promedio...
        Asignacion,     // =
        ValorEntero,    // 25
        ValorDecimal,   // 91.5
        ValorTexto,     // "Carlos"
        ValorBooleano,  // VERDADERO, FALSO
        FinSentencia,   // ; (opcional)
        Desconocido     // cualquier símbolo que no pertenece al lenguaje
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

        /// <summary>Nombre del token con el formato que se muestra en la landing page.</summary>
        public string Nombre => Tipo switch
        {
            TipoToken.TipoDato => "TOKEN_TIPO",
            TipoToken.Imprimir => "TOKEN_IMPRIMIR",
            TipoToken.Identificador => "TOKEN_IDENTIFICADOR",
            TipoToken.Asignacion => "TOKEN_ASIGNACION",
            TipoToken.ValorEntero => "TOKEN_ENTERO",
            TipoToken.ValorDecimal => "TOKEN_DECIMAL",
            TipoToken.ValorTexto => "TOKEN_TEXTO",
            TipoToken.ValorBooleano => "TOKEN_BOOLEANO",
            TipoToken.FinSentencia => "TOKEN_FIN",
            _ => "TOKEN_DESCONOCIDO"
        };

        public bool EsValor =>
            Tipo == TipoToken.ValorEntero || Tipo == TipoToken.ValorDecimal ||
            Tipo == TipoToken.ValorTexto || Tipo == TipoToken.ValorBooleano;

        public override string ToString() => $"{Nombre}({Lexema})";
    }
}
