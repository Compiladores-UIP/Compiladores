namespace MensajesPersonalizados.Compilador;

public enum TipoToken
{
    PALABRA_RESERVADA,
    FUNCION,
    IDENTIFICADOR,
    CADENA,
    NUMERO_ENTERO,
    NUMERO_DECIMAL,
    OPERADOR,
    ASIGNACION,
    PARENTESIS_IZQ,
    PARENTESIS_DER,
    FIN_LINEA,
    FIN_ARCHIVO
}

/// <summary>Unidad mínima reconocida por el analizador léxico.</summary>
/// <param name="Lexema">Texto tal como aparece en el código fuente.</param>
/// <param name="Valor">Contenido procesado (en cadenas, ya sin comillas y con escapes resueltos).</param>
public sealed record Token(TipoToken Tipo, string Lexema, string Valor, int Linea, int Columna)
{
    public override string ToString() => $"{Linea,3}:{Columna,-3} {Tipo,-18} {Lexema}";
}
