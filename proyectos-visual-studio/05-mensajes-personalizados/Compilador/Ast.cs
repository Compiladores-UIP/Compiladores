namespace MensajesPersonalizados.Compilador;

public enum TipoDato
{
    TEXTO,
    ENTERO,
    DECIMAL
}

// ---------- Instrucciones ----------

public abstract record Instruccion(int Linea, int Columna);

/// <summary>TEXTO nombre = "Ana"  ·  ENTERO edad</summary>
public sealed record Declaracion(TipoDato Tipo, string Nombre, Expresion? Valor, int Linea, int Columna)
    : Instruccion(Linea, Columna);

/// <summary>nombre = "Luis"</summary>
public sealed record Asignacion(string Nombre, Expresion Valor, int Linea, int Columna)
    : Instruccion(Linea, Columna);

/// <summary>MOSTRAR "Hola, " + nombre</summary>
public sealed record Mostrar(Expresion Valor, int Linea, int Columna)
    : Instruccion(Linea, Columna);

/// <summary>PEDIR nombre "¿Cómo te llamas?"</summary>
public sealed record Pedir(string Nombre, string? Pregunta, int Linea, int Columna)
    : Instruccion(Linea, Columna);

// ---------- Expresiones ----------

public abstract record Expresion(int Linea, int Columna);

public sealed record LiteralNumero(string Lexema, bool EsDecimal, int Linea, int Columna)
    : Expresion(Linea, Columna);

/// <summary>Cadena simple o plantilla con marcadores {variable}.</summary>
public sealed record LiteralCadena(IReadOnlyList<SegmentoCadena> Segmentos, int Linea, int Columna)
    : Expresion(Linea, Columna)
{
    public bool EsPlantilla => Segmentos.Any(s => s is SegmentoVariable);
}

public sealed record Variable(string Nombre, int Linea, int Columna)
    : Expresion(Linea, Columna);

public sealed record Binaria(Expresion Izquierda, string Operador, Expresion Derecha, int Linea, int Columna)
    : Expresion(Linea, Columna);

public sealed record LlamadaFuncion(string Funcion, Expresion Argumento, int Linea, int Columna)
    : Expresion(Linea, Columna);

public sealed record Agrupacion(Expresion Interna, int Linea, int Columna)
    : Expresion(Linea, Columna);

public abstract record SegmentoCadena;
public sealed record SegmentoTexto(string Texto) : SegmentoCadena;
public sealed record SegmentoVariable(string Nombre, int Linea, int Columna) : SegmentoCadena;

public sealed record ProgramaFuente(IReadOnlyList<Instruccion> Instrucciones);
