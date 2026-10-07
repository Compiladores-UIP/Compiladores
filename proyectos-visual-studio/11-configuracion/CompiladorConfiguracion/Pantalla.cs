namespace CompiladorConfiguracion
{
    /// <summary>Ayudas para mostrar títulos, tablas y mensajes con color en la consola.</summary>
    public static class Pantalla
    {
        public static void Titulo(string numero, string texto)
        {
            Console.WriteLine();
            Escribir(ConsoleColor.Green, $"  FASE {numero} · {texto.ToUpperInvariant()}");
            Escribir(ConsoleColor.DarkGray, "  " + new string('─', 60));
        }

        public static void Escribir(ConsoleColor color, string texto)
        {
            var anterior = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(texto);
            Console.ForegroundColor = anterior;
        }

        public static void Ok(string texto) => Escribir(ConsoleColor.Green, "  ✓ " + texto);
        public static void Nota(string texto) => Escribir(ConsoleColor.DarkGray, "  " + texto);
        public static void Omitida() => Escribir(ConsoleColor.DarkGray, "  (fase omitida: hay errores en las fases anteriores)");

        /// <summary>Dibuja una tabla con bordes a partir de encabezados y filas.</summary>
        public static void Tabla(string[] encabezados, IEnumerable<string[]> filas)
        {
            var lista = filas.ToList();
            int[] ancho = encabezados.Select((h, i) => Math.Max(h.Length, lista.Count == 0 ? 0 : lista.Max(f => f[i].Length))).ToArray();

            string Linea(char izq, char medio, char der) =>
                "  " + izq + string.Join(medio, ancho.Select(a => new string('─', a + 2))) + der;
            string Fila(string[] celdas) =>
                "  │" + string.Join("│", celdas.Select((c, i) => " " + c.PadRight(ancho[i]) + " ")) + "│";

            Console.WriteLine(Linea('┌', '┬', '┐'));
            Escribir(ConsoleColor.Cyan, Fila(encabezados));
            Console.WriteLine(Linea('├', '┼', '┤'));
            foreach (var f in lista) Console.WriteLine(Fila(f));
            Console.WriteLine(Linea('└', '┴', '┘'));
        }

        public static void Codigo(string codigo)
        {
            foreach (var linea in codigo.Split('\n'))
                Escribir(ConsoleColor.Yellow, "    " + linea);
        }
    }
}
