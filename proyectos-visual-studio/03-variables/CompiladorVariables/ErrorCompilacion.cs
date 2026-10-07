namespace CompiladorVariables
{
    public enum Fase { Lexico, Sintactico, Semantico }

    /// <summary>
    /// Error controlado: indica la fase que lo detectó, la posición y cómo corregirlo.
    /// </summary>
    public class ErrorCompilacion
    {
        public Fase Fase { get; }
        public int Linea { get; }
        public int Columna { get; }
        public string Mensaje { get; }
        public string Sugerencia { get; }

        public ErrorCompilacion(Fase fase, int linea, int columna, string mensaje, string sugerencia = "")
        {
            Fase = fase;
            Linea = linea;
            Columna = columna;
            Mensaje = mensaje;
            Sugerencia = sugerencia;
        }

        public string NombreFase => Fase switch
        {
            Fase.Lexico => "Error léxico",
            Fase.Sintactico => "Error sintáctico",
            _ => "Error semántico"
        };

        public override string ToString() =>
            $"{NombreFase} (línea {Linea}, columna {Columna}): {Mensaje}";
    }
}
