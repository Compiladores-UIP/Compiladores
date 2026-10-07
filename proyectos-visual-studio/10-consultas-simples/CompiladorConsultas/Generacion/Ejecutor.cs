using System.Globalization;
using CompiladorConsultas.Lexico;
using CompiladorConsultas.Semantico;
using CompiladorConsultas.Sintactico;

namespace CompiladorConsultas.Generacion
{
    /// <summary>
    /// FASE 5 · Resultado.
    /// Ejecuta la consulta sobre la base de datos de ejemplo en memoria,
    /// igual que lo haría un gestor SQL con la sentencia generada.
    /// </summary>
    public static class Ejecutor
    {
        public static (string[] Columnas, List<string[]> Filas) Ejecutar(Consulta q)
        {
            var tabla = BaseDeDatos.BuscarTabla(q.Tabla.Lexema)!;
            IEnumerable<object[]> filas = tabla.Filas.Where(f => Cumple(tabla, f, q));

            if (q.OrdenCampo != null)
            {
                int i = tabla.Indice(q.OrdenCampo.Lexema);
                filas = q.OrdenDescendente
                    ? filas.OrderByDescending(f => f[i], Comparer<object>.Create(Comparar))
                    : filas.OrderBy(f => f[i], Comparer<object>.Create(Comparar));
            }

            string[] columnas = q.Campos.Count == 0 ? tabla.Campos.Select(c => c.Nombre).ToArray() : q.Campos.Select(c => c.Lexema).ToArray();
            int[] indices = columnas.Select(tabla.Indice).ToArray();

            return (columnas, filas.Select(f => indices.Select(i => BaseDeDatos.Formato(f[i])).ToArray()).ToList());
        }

        /// <summary>
        /// Evalúa el filtro respetando la precedencia de SQL: AND se resuelve antes que OR.
        /// Se separan las condiciones en grupos unidos por O; basta con que un grupo cumpla todas sus Y.
        /// </summary>
        private static bool Cumple(Tabla tabla, object[] fila, Consulta q)
        {
            if (q.Condiciones.Count == 0) return true;

            var grupos = new List<List<Condicion>> { new() { q.Condiciones[0] } };
            for (int i = 0; i < q.Conectores.Count; i++)
            {
                if (q.Conectores[i].Tipo == TipoToken.O) grupos.Add(new());
                grupos[^1].Add(q.Condiciones[i + 1]);
            }
            return grupos.Any(g => g.All(c => Evaluar(tabla, fila, c)));
        }

        private static bool Evaluar(Tabla tabla, object[] fila, Condicion c)
        {
            object dato = fila[tabla.Indice(c.Campo.Lexema)];
            object valor = c.Valor.Tipo == TipoToken.Texto
                ? c.Valor.Lexema.Trim('"')
                : double.Parse(c.Valor.Lexema, CultureInfo.InvariantCulture);

            int cmp = Comparar(dato, valor);
            return c.Operador.Lexema switch
            {
                ">" => cmp > 0,
                "<" => cmp < 0,
                ">=" => cmp >= 0,
                "<=" => cmp <= 0,
                "=" => cmp == 0,
                _ => cmp != 0      // <> y !=
            };
        }

        private static int Comparar(object? a, object? b)
        {
            if (a is string sa && b is string sb)
                return string.Compare(sa, sb, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            return Convert.ToDouble(a, CultureInfo.InvariantCulture).CompareTo(Convert.ToDouble(b, CultureInfo.InvariantCulture));
        }
    }
}
