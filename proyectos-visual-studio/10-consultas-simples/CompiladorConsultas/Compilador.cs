using CompiladorConsultas.Lexico;
using CompiladorConsultas.Semantico;
using CompiladorConsultas.Sintactico;

namespace CompiladorConsultas
{
    public class ResultadoCompilacion
    {
        public string[] LineasFuente { get; init; } = Array.Empty<string>();
        public List<Token> Tokens { get; init; } = new();
        public List<Consulta> Consultas { get; init; } = new();
        public AnalizadorSemantico Semantico { get; init; } = new();
        public List<ErrorCompilacion> Errores { get; init; } = new();

        public bool ErroresLexicos => Errores.Any(e => e.Fase == Fase.Lexico);
        public bool Exito => Errores.Count == 0 && Consultas.Count > 0;
    }

    /// <summary>Une las fases: léxico → sintáctico → semántico (la generación se hace por consulta).</summary>
    public static class Compilador
    {
        public static ResultadoCompilacion Compilar(string codigo)
        {
            string[] lineas = codigo.Replace("\r", "").Split('\n');

            var lexico = new AnalizadorLexico();
            lexico.Analizar(codigo);

            var sintactico = new AnalizadorSintactico();
            var consultas = sintactico.Analizar(lexico.Tokens, lineas, lexico.Errores.Select(e => e.Linea).ToHashSet());

            var semantico = new AnalizadorSemantico();
            semantico.Analizar(consultas);

            return new ResultadoCompilacion
            {
                LineasFuente = lineas,
                Tokens = lexico.Tokens,
                Consultas = consultas,
                Semantico = semantico,
                Errores = lexico.Errores.Concat(sintactico.Errores).Concat(semantico.Errores)
                    .OrderBy(e => e.Linea).ThenBy(e => e.Columna).ToList()
            };
        }
    }
}
