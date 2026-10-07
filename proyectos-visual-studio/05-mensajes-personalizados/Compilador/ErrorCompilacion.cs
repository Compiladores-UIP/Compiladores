namespace MensajesPersonalizados.Compilador;

public enum FaseError
{
    Lexico,
    Sintactico,
    Semantico
}

public sealed record Diagnostico(FaseError Fase, string Mensaje, int Linea, int Columna)
{
    public string NombreFase => Fase switch
    {
        FaseError.Lexico => "léxico",
        FaseError.Sintactico => "sintáctico",
        _ => "semántico"
    };

    public override string ToString() => $"Error {NombreFase} (línea {Linea}, columna {Columna}): {Mensaje}";
}

/// <summary>Detiene una fase cuando no tiene sentido seguir analizando.</summary>
public sealed class ErrorCompilacion : Exception
{
    public Diagnostico Diagnostico { get; }

    public ErrorCompilacion(FaseError fase, string mensaje, int linea, int columna)
        : base(mensaje)
    {
        Diagnostico = new Diagnostico(fase, mensaje, linea, columna);
    }
}
