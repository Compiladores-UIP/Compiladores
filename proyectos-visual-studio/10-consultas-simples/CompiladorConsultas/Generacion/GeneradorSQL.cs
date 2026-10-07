using CompiladorConsultas.Lexico;
using CompiladorConsultas.Sintactico;

namespace CompiladorConsultas.Generacion
{
    /// <summary>
    /// FASE 4 · Generación de código.
    /// Traduce cada consulta validada a SQL y, como referencia, a C# con LINQ.
    ///
    ///   BUSCAR   → SELECT      DONDE → WHERE     Y → AND     O → OR
    ///   DE       → FROM        ORDENAR POR → ORDER BY        "texto" → 'texto'
    /// </summary>
    public static class GeneradorSQL
    {
        public static string GenerarSQL(Consulta q)
        {
            string campos = q.Campos.Count == 0 ? "*" : string.Join(", ", q.Campos.Select(c => c.Lexema));
            string sql = $"SELECT {campos} FROM {q.Tabla.Lexema}";

            if (q.Condiciones.Count > 0)
            {
                var partes = new List<string> { CondicionSQL(q.Condiciones[0]) };
                for (int i = 0; i < q.Conectores.Count; i++)
                    partes.Add((q.Conectores[i].Tipo == TipoToken.Y ? "AND " : "OR ") + CondicionSQL(q.Condiciones[i + 1]));
                sql += " WHERE " + string.Join(" ", partes);
            }

            if (q.OrdenCampo != null)
                sql += $" ORDER BY {q.OrdenCampo.Lexema}" + (q.OrdenDescendente ? " DESC" : " ASC");

            return sql + ";";
        }

        private static string CondicionSQL(Condicion c)
        {
            string op = c.Operador.Lexema == "!=" ? "<>" : c.Operador.Lexema;
            string valor = c.Valor.Tipo == TipoToken.Texto ? "'" + c.Valor.Lexema.Trim('"').Replace("'", "''") + "'" : c.Valor.Lexema;
            return $"{c.Campo.Lexema} {op} {valor}";
        }

        /// <summary>La misma consulta escrita con LINQ en C#.</summary>
        public static string GenerarLinq(Consulta q)
        {
            string linq = q.Tabla.Lexema;
            if (q.Condiciones.Count > 0)
            {
                var partes = new List<string> { CondicionLinq(q.Condiciones[0]) };
                for (int i = 0; i < q.Conectores.Count; i++)
                    partes.Add((q.Conectores[i].Tipo == TipoToken.Y ? "&& " : "|| ") + CondicionLinq(q.Condiciones[i + 1]));
                linq += $".Where(x => {string.Join(" ", partes)})";
            }
            if (q.OrdenCampo != null)
                linq += (q.OrdenDescendente ? ".OrderByDescending" : ".OrderBy") + $"(x => x.{q.OrdenCampo.Lexema})";
            if (q.Campos.Count > 0)
                linq += $".Select(x => new {{ {string.Join(", ", q.Campos.Select(c => "x." + c.Lexema))} }})";
            return "var resultado = " + linq + ";";
        }

        private static string CondicionLinq(Condicion c)
        {
            string op = c.Operador.Lexema switch { "=" => "==", "<>" => "!=", var o => o };
            return $"x.{c.Campo.Lexema} {op} {c.Valor.Lexema}";
        }
    }
}
