namespace CompiladorConfiguracion.Lexico
{
    /// <summary>
    /// Categorías léxicas (tipos de token) del lenguaje de configuración.
    /// </summary>
    public enum TipoToken
    {
        Formato,          // FORMATO
        NombreFormato,    // JSON, XML
        Config,           // CONFIG
        Seccion,          // SECCION
        FinSeccion,       // FINSECCION
        Tipo,             // TEXTO, ENTERO, DECIMAL, BOOLEANO, LISTA (anotación opcional)
        Clave,            // nombre, puerto, servidor...
        Asignacion,       // =
        DosPuntos,        // :
        Texto,            // "Nexo"
        Entero,           // 3100, -5
        Decimal,          // 1.5
        Booleano,         // verdadero, falso
        CorcheteAbre,     // [
        CorcheteCierra,   // ]
        Coma,             // ,
        FinSentencia,     // ; (opcional)
        Desconocido       // cualquier símbolo que no pertenece al lenguaje
    }

    /// <summary>
    /// Unidad léxica: el tipo de token, el texto original (lexema) y su posición.
    /// En los textos, Valor guarda el contenido ya sin comillas y con las secuencias \" \\ \n \t resueltas.
    /// </summary>
    public class Token
    {
        public TipoToken Tipo { get; }
        public string Lexema { get; }
        public string Valor { get; }
        public int Linea { get; }
        public int Columna { get; }

        public Token(TipoToken tipo, string lexema, int linea, int columna, string? valor = null)
        {
            Tipo = tipo;
            Lexema = lexema;
            Valor = valor ?? lexema;
            Linea = linea;
            Columna = columna;
        }

        public int ColumnaFinal => Columna + Lexema.Length;

        public bool EsValorSimple => Tipo is TipoToken.Texto or TipoToken.Entero or TipoToken.Decimal or TipoToken.Booleano;

        /// <summary>Nombre del token con el formato que se muestra en la landing page.</summary>
        public string Nombre => Tipo switch
        {
            TipoToken.Formato => "TOKEN_FORMATO",
            TipoToken.NombreFormato => "TOKEN_NOMBRE_FORMATO",
            TipoToken.Config => "TOKEN_CONFIG",
            TipoToken.Seccion => "TOKEN_SECCION",
            TipoToken.FinSeccion => "TOKEN_FINSECCION",
            TipoToken.Tipo => "TOKEN_TIPO",
            TipoToken.Clave => "TOKEN_CLAVE",
            TipoToken.Asignacion => "TOKEN_ASIGNACION",
            TipoToken.DosPuntos => "TOKEN_DOS_PUNTOS",
            TipoToken.Texto => "TOKEN_TEXTO",
            TipoToken.Entero => "TOKEN_ENTERO",
            TipoToken.Decimal => "TOKEN_DECIMAL",
            TipoToken.Booleano => "TOKEN_BOOLEANO",
            TipoToken.CorcheteAbre => "TOKEN_CORCHETE_ABRE",
            TipoToken.CorcheteCierra => "TOKEN_CORCHETE_CIERRA",
            TipoToken.Coma => "TOKEN_COMA",
            TipoToken.FinSentencia => "TOKEN_FIN",
            _ => "TOKEN_DESCONOCIDO"
        };

        public override string ToString() => $"{Nombre}({Lexema})";
    }
}
