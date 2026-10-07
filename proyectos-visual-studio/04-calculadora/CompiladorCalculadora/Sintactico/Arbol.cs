using CompiladorCalculadora.Lexico;

namespace CompiladorCalculadora.Sintactico
{
    /// <summary>Tipos numéricos del lenguaje. El análisis semántico los anota en cada expresión.</summary>
    public enum TipoDato { Entero, Decimal }

    // ---------------------------------------------------------------- Sentencias

    /// <summary>Raíz del árbol: la lista de instrucciones del programa.</summary>
    public class Programa
    {
        public List<Sentencia> Sentencias { get; } = new();
    }

    public abstract class Sentencia
    {
        public int Linea { get; init; }
        /// <summary>Texto original de la línea (se usa para comentar el C# generado).</summary>
        public string Fuente { get; init; } = "";
    }

    /// <summary>ENTERO a = 10   ·   DECIMAL total = a * 1.07</summary>
    public class Declaracion : Sentencia
    {
        public required Token Tipo { get; init; }
        public required Token Nombre { get; init; }
        public required Expresion Valor { get; init; }
        public TipoDato TipoDeclarado => Tipo.Lexema == "ENTERO" ? TipoDato.Entero : TipoDato.Decimal;
    }

    /// <summary>a = a + 1</summary>
    public class Asignacion : Sentencia
    {
        public required Token Nombre { get; init; }
        public required Expresion Valor { get; init; }
    }

    /// <summary>IMPRIMIR a + b   ·   IMPRIMIR "Suma:", a + b   ·   IMPRIMIR "Listo"</summary>
    public class Impresion : Sentencia
    {
        public Token? Etiqueta { get; init; }
        public Expresion? Valor { get; init; }
        public string? TextoEtiqueta => Etiqueta?.Lexema.Trim('"');
    }

    // ---------------------------------------------------------------- Expresiones

    public abstract class Expresion
    {
        /// <summary>Token que ubica la expresión en el código (para los mensajes de error).</summary>
        public required Token Token { get; init; }
        /// <summary>Lo completa el análisis semántico.</summary>
        public TipoDato Tipo { get; set; }
    }

    /// <summary>12 · 4.5</summary>
    public class Literal : Expresion
    {
        public bool EsDecimal => Token.Tipo == TipoToken.NumeroDecimal;
    }

    /// <summary>a · total</summary>
    public class Variable : Expresion
    {
        public string Nombre => Token.Lexema;
    }

    /// <summary>a + b · a / b · a ^ 2  (Token es el operador)</summary>
    public class Binaria : Expresion
    {
        public required Expresion Izquierda { get; init; }
        public required Expresion Derecha { get; init; }
        public string Operador => Token.Lexema;
    }

    /// <summary>-a</summary>
    public class Negacion : Expresion
    {
        public required Expresion Operando { get; init; }
    }

    /// <summary>( a + b ) — se conserva para respetar los paréntesis del usuario en el C# generado.</summary>
    public class Grupo : Expresion
    {
        public required Expresion Interior { get; init; }
    }

    /// <summary>RAIZ(16) · ABS(a - b)</summary>
    public class Llamada : Expresion
    {
        public required Expresion Argumento { get; init; }
        public string Funcion => Token.Lexema;
    }
}
