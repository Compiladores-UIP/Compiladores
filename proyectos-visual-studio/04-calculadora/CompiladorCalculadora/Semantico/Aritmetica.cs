using System.Globalization;
using CompiladorCalculadora.Sintactico;

namespace CompiladorCalculadora.Semantico
{
    /// <summary>Un valor numérico con su tipo. Los ENTERO se guardan como double sin parte decimal.</summary>
    public readonly record struct Valor(TipoDato Tipo, double Numero)
    {
        public static Valor Entero(long n) => new(TipoDato.Entero, n);
        public static Valor Decimal(double n) => new(TipoDato.Decimal, n);

        /// <summary>Escribe el valor igual que Console.WriteLine en C# (cultura invariante).</summary>
        public override string ToString() => Tipo == TipoDato.Entero
            ? ((long)Numero).ToString(CultureInfo.InvariantCulture)
            : Numero.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reglas aritméticas del lenguaje, idénticas a las del C# generado.
    /// Las usa el análisis semántico (para detectar errores antes de ejecutar)
    /// y el ejecutor (para mostrar el resultado).
    /// Cada operación devuelve el valor o un mensaje de error.
    /// </summary>
    public static class Aritmetica
    {
        public static (Valor? Resultado, string? Error, string Pista) Binaria(string op, Valor a, Valor b, string textoDerecha)
        {
            bool enteros = a.Tipo == TipoDato.Entero && b.Tipo == TipoDato.Entero;
            switch (op)
            {
                case "+": case "-": case "*":
                    if (enteros)
                    {
                        long x = (long)a.Numero, y = (long)b.Numero;
                        long r = op == "+" ? x + y : op == "-" ? x - y : x * y;
                        if (r > int.MaxValue || r < int.MinValue)
                            return (null, $"El resultado ({r}) no cabe en un ENTERO: el rango es de {int.MinValue} a {int.MaxValue}.",
                                    "Usa DECIMAL para trabajar con números tan grandes.");
                        return (Valor.Entero(r), null, "");
                    }
                    double d = op == "+" ? a.Numero + b.Numero : op == "-" ? a.Numero - b.Numero : a.Numero * b.Numero;
                    return Finito(d);

                case "/":
                    if (b.Numero == 0)
                        return (null, $"División entre cero: {Describir(textoDerecha, b)}.",
                                "El divisor debe ser distinto de cero. Revisa el valor antes de dividir.");
                    // En este lenguaje la división siempre da DECIMAL: 7 / 2 = 3.5
                    return Finito(a.Numero / b.Numero);

                case "%":
                    if (b.Numero == 0)
                        return (null, $"Residuo de una división entre cero: {Describir(textoDerecha, b)}.",
                                "El divisor de % debe ser distinto de cero.");
                    if (enteros) return (Valor.Entero((long)a.Numero % (long)b.Numero), null, "");
                    return Finito(a.Numero % b.Numero);

                case "^":
                    double p = Math.Pow(a.Numero, b.Numero);
                    if (double.IsNaN(p))
                        return (null, $"La potencia {a} ^ {b} no tiene un resultado real.",
                                "Un número negativo solo se puede elevar a un exponente entero.");
                    if (a.Numero == 0 && b.Numero < 0)
                        return (null, $"La potencia 0 ^ {b} equivale a dividir entre cero.", "Usa un exponente positivo cuando la base es 0.");
                    return Finito(p);
            }
            throw new InvalidOperationException("Operador desconocido: " + op);
        }

        public static (Valor? Resultado, string? Error, string Pista) Funcion(string nombre, Valor x)
        {
            if (nombre == "RAIZ")
            {
                if (x.Numero < 0)
                    return (null, $"No existe la raíz cuadrada real de un número negativo ({x}).", "Usa ABS si necesitas la raíz del valor absoluto: RAIZ(ABS(x)).");
                return (Valor.Decimal(Math.Sqrt(x.Numero)), null, "");
            }
            return (x with { Numero = Math.Abs(x.Numero) }, null, ""); // ABS conserva el tipo
        }

        // Igual que en C#: -(0.0) es -0 y se imprime "-0"; en ENTERO es 0.
        public static Valor Negar(Valor x) => x with { Numero = -x.Numero };

        private static (Valor?, string?, string) Finito(double d) => double.IsInfinity(d)
            ? (null, "El resultado es demasiado grande para un DECIMAL.", "Revisa la operación: el valor supera el máximo de double.")
            : (Valor.Decimal(d), null, "");

        private static string Describir(string texto, Valor v) =>
            texto == v.ToString() ? $"el divisor es {v}" : $"\"{texto}\" vale {v}";
    }
}
