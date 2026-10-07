using CompiladorConsultas.Lexico;

namespace CompiladorConsultas.Sintactico
{
    /// <summary>campo operador valor   (ej. edad &gt; 18)</summary>
    public class Condicion
    {
        public Token Campo { get; init; } = null!;
        public Token Operador { get; init; } = null!;
        public Token Valor { get; init; } = null!;
    }

    /// <summary>Árbol de una consulta completa.</summary>
    public class Consulta
    {
        public int Linea { get; init; }
        public string Fuente { get; init; } = "";
        public Token Tabla { get; set; } = null!;
        public List<Token> Campos { get; } = new();          // vacío = todos los campos (*)
        public List<Condicion> Condiciones { get; } = new();
        public List<Token> Conectores { get; } = new();      // Y / O entre condiciones
        public Token? OrdenCampo { get; set; }
        public bool OrdenDescendente { get; set; }
    }
}
