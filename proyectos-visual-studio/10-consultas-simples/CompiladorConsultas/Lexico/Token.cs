namespace CompiladorConsultas.Lexico
{
    /// <summary>Categorías léxicas que reconoce el compilador de consultas.</summary>
    public enum TipoToken
    {
        Buscar,         // BUSCAR       → SELECT
        De,             // DE           → FROM
        Donde,          // DONDE        → WHERE
        Y,              // Y            → AND
        O,              // O            → OR
        Ordenar,        // ORDENAR      → ORDER
        Por,            // POR          → BY
        Asc,            // ASC
        Desc,           // DESC
        Identificador,  // estudiante, edad, nombre...
        Operador,       // >  <  >=  <=  =  <>  !=
        NumeroEntero,   // 18
        NumeroDecimal,  // 8.5
        Texto,          // "Ana"
        Coma,           // ,
        FinSentencia,   // ; (opcional)
        Desconocido
    }

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

        public bool EsPalabraReservada => Tipo is TipoToken.Buscar or TipoToken.De or TipoToken.Donde or TipoToken.Y
            or TipoToken.O or TipoToken.Ordenar or TipoToken.Por or TipoToken.Asc or TipoToken.Desc;

        public bool EsValor => Tipo is TipoToken.NumeroEntero or TipoToken.NumeroDecimal or TipoToken.Texto;

        /// <summary>Nombre del token tal como se muestra en la landing page.</summary>
        public string Nombre => Tipo switch
        {
            TipoToken.Identificador => "TOKEN_IDENTIFICADOR",
            TipoToken.Operador => "TOKEN_OPERADOR",
            TipoToken.NumeroEntero => "TOKEN_NUMERO",
            TipoToken.NumeroDecimal => "TOKEN_DECIMAL",
            TipoToken.Texto => "TOKEN_TEXTO",
            TipoToken.Coma => "TOKEN_COMA",
            TipoToken.FinSentencia => "TOKEN_FIN",
            TipoToken.Desconocido => "TOKEN_DESCONOCIDO",
            _ => "TOKEN_" + Tipo.ToString().ToUpperInvariant()   // TOKEN_BUSCAR, TOKEN_DONDE...
        };
    }
}
