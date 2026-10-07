using CompiladorConfiguracion.Lexico;

namespace CompiladorConfiguracion.Sintactico
{
    /// <summary>Tipos de valor del lenguaje. El análisis semántico los anota en cada clave.</summary>
    public enum TipoValor { Texto, Entero, Decimal, Booleano, Lista }

    /// <summary>Raíz del árbol: el formato de salida y los elementos del nivel principal.</summary>
    public class Archivo
    {
        public Token? Formato { get; set; }
        public List<Elemento> Elementos { get; } = new();
        public string NombreFormato => Formato?.Lexema ?? "JSON";
    }

    public abstract class Elemento
    {
        public required Token Nombre { get; init; }
        public int Linea => Nombre.Linea;
    }

    /// <summary>CONFIG puerto = 3100   ·   CONFIG puerto : ENTERO = 3100</summary>
    public class Clave : Elemento
    {
        public Token? TipoAnotado { get; init; }
        public required Valor Valor { get; init; }

        /// <summary>Tipo del valor; lo completa el análisis semántico.</summary>
        public TipoValor Tipo { get; set; }
        /// <summary>Para las listas: tipo de sus elementos.</summary>
        public TipoValor TipoElementos { get; set; }
    }

    /// <summary>SECCION servidor … FINSECCION</summary>
    public class Seccion : Elemento
    {
        public List<Elemento> Elementos { get; } = new();
        public int LineaFin { get; set; }
    }

    public abstract class Valor
    {
        public required Token Token { get; init; }
    }

    /// <summary>"Nexo" · 3100 · 1.5 · verdadero</summary>
    public class ValorSimple : Valor { }

    /// <summary>[ "es", "en" ]  (Token es el corchete de apertura)</summary>
    public class ValorLista : Valor
    {
        public List<Token> Elementos { get; } = new();
    }
}
