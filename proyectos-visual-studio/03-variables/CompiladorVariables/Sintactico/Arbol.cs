using CompiladorVariables.Lexico;

namespace CompiladorVariables.Sintactico
{
    /// <summary>Nodo base del árbol sintáctico (AST).</summary>
    public abstract class Sentencia
    {
        public int Linea { get; init; }
    }

    /// <summary>TIPO identificador = valor</summary>
    public class Declaracion : Sentencia
    {
        public Token Tipo { get; init; } = null!;
        public Token Nombre { get; init; } = null!;
        public Token Valor { get; init; } = null!;   // un literal o un identificador
    }

    /// <summary>IMPRIMIR valor</summary>
    public class Impresion : Sentencia
    {
        public Token Palabra { get; init; } = null!;
        public Token Valor { get; init; } = null!;
    }

    /// <summary>Raíz del árbol: la lista de sentencias del programa.</summary>
    public class Programa
    {
        public List<Sentencia> Sentencias { get; } = new();
    }
}
