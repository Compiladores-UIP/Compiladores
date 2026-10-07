using System.Globalization;

namespace CompiladorConsultas.Semantico
{
    public enum TipoCampo { Entero, Decimal, Texto }

    public class Campo
    {
        public string Nombre { get; init; } = "";
        public TipoCampo Tipo { get; init; }
        public string TipoSql => Tipo switch
        {
            TipoCampo.Entero => "INT",
            TipoCampo.Decimal => "DECIMAL(4,1)",
            _ => "VARCHAR(50)"
        };
    }

    public class Tabla
    {
        public string Nombre { get; init; } = "";
        public List<Campo> Campos { get; init; } = new();
        public List<object[]> Filas { get; init; } = new();

        public Campo? BuscarCampo(string nombre) => Campos.FirstOrDefault(c => c.Nombre == nombre);
        public int Indice(string nombre) => Campos.FindIndex(c => c.Nombre == nombre);
    }

    /// <summary>
    /// Base de datos de ejemplo en memoria. El analizador semántico la usa para saber
    /// qué tablas y campos existen, y el ejecutor la usa para mostrar el resultado.
    /// </summary>
    public static class BaseDeDatos
    {
        public static readonly List<Tabla> Tablas = new()
        {
            new Tabla
            {
                Nombre = "estudiante",
                Campos = new()
                {
                    new Campo { Nombre = "id", Tipo = TipoCampo.Entero },
                    new Campo { Nombre = "nombre", Tipo = TipoCampo.Texto },
                    new Campo { Nombre = "edad", Tipo = TipoCampo.Entero },
                    new Campo { Nombre = "carrera", Tipo = TipoCampo.Texto },
                    new Campo { Nombre = "promedio", Tipo = TipoCampo.Decimal }
                },
                Filas = new()
                {
                    new object[] { 1, "Ana", 20, "Sistemas", 91.5 },
                    new object[] { 2, "Luis", 17, "Industrial", 78.0 },
                    new object[] { 3, "Marta", 18, "Sistemas", 85.0 },
                    new object[] { 4, "Pedro", 22, "Civil", 69.5 },
                    new object[] { 5, "Sofía", 19, "Industrial", 88.0 },
                    new object[] { 6, "Diego", 21, "Sistemas", 74.5 }
                }
            },
            new Tabla
            {
                Nombre = "curso",
                Campos = new()
                {
                    new Campo { Nombre = "codigo", Tipo = TipoCampo.Texto },
                    new Campo { Nombre = "nombre", Tipo = TipoCampo.Texto },
                    new Campo { Nombre = "creditos", Tipo = TipoCampo.Entero },
                    new Campo { Nombre = "cupos", Tipo = TipoCampo.Entero }
                },
                Filas = new()
                {
                    new object[] { "INF-301", "Compiladores", 4, 30 },
                    new object[] { "INF-210", "Base de Datos", 4, 25 },
                    new object[] { "MAT-101", "Cálculo I", 5, 40 },
                    new object[] { "INF-150", "Programación I", 3, 35 }
                }
            }
        };

        public static Tabla? BuscarTabla(string nombre) => Tablas.FirstOrDefault(t => t.Nombre == nombre);

        public static string Formato(object valor) => valor switch
        {
            double d => d.ToString("0.0", CultureInfo.InvariantCulture),
            _ => valor.ToString() ?? ""
        };

        /// <summary>Script SQL (CREATE TABLE + INSERT) para probar las consultas en un gestor real.</summary>
        public static string ScriptCreacion()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("-- Datos de ejemplo del Mini-Compilador 10 · Consultas simples");
            foreach (var t in Tablas)
            {
                sb.AppendLine();
                sb.AppendLine($"CREATE TABLE {t.Nombre} (");
                sb.AppendLine(string.Join(",\n", t.Campos.Select(c => $"    {c.Nombre} {c.TipoSql}")));
                sb.AppendLine(");");
                foreach (var f in t.Filas)
                {
                    var valores = f.Select(v => v is string s ? $"'{s}'" : Formato(v));
                    sb.AppendLine($"INSERT INTO {t.Nombre} VALUES ({string.Join(", ", valores)});");
                }
            }
            return sb.ToString();
        }
    }
}
