using CompiladorConfiguracion.Generacion;
using CompiladorConfiguracion.Lexico;
using CompiladorConfiguracion.Semantico;
using CompiladorConfiguracion.Sintactico;

namespace CompiladorConfiguracion
{
    /// <summary>Todo lo que produce una compilación, fase por fase.</summary>
    public class ResultadoCompilacion
    {
        public string[] LineasFuente { get; init; } = Array.Empty<string>();
        public List<Token> Tokens { get; init; } = new();
        public Archivo Arbol { get; init; } = new();
        public AnalizadorSemantico Semantico { get; init; } = new();
        public GeneradorDocumento? Generador { get; init; }
        public List<ErrorCompilacion> Errores { get; init; } = new();
        public string Documento { get; init; } = "";
        public string NombreArchivo { get; init; } = "";
        public string CodigoCSharp { get; init; } = "";
        public List<string> Salida { get; init; } = new();

        public bool ErroresLexicos => Errores.Any(e => e.Fase == Fase.Lexico);
        public bool ErroresSintacticos => Errores.Any(e => e.Fase == Fase.Sintactico);
        public bool Exito => Errores.Count == 0 && Arbol.Formato != null;
    }

    /// <summary>
    /// Une las fases: léxico → sintáctico → semántico → generación JSON/XML → resultado.
    /// </summary>
    public static class Compilador
    {
        public static ResultadoCompilacion Compilar(string codigo)
        {
            string[] lineas = codigo.Replace("\r", "").Split('\n');

            // 1. Léxico
            var lexico = new AnalizadorLexico();
            lexico.Analizar(codigo);

            // 2. Sintáctico (se omiten las líneas que ya tienen error léxico)
            var lineasConError = lexico.Errores.Select(e => e.Linea).ToHashSet();
            var sintactico = new AnalizadorSintactico();
            Archivo arbol = sintactico.Analizar(lexico.Tokens, lineasConError);

            // 3. Semántico (claves únicas, tipos, listas, rangos)
            var semantico = new AnalizadorSemantico();
            semantico.Analizar(arbol);

            var errores = lexico.Errores.Concat(sintactico.Errores).Concat(semantico.Errores)
                .OrderBy(e => e.Linea).ThenBy(e => e.Columna).ToList();

            // 4 y 5. Generación y resultado: solo si no hubo errores
            GeneradorDocumento? generador = null;
            string documento = "", nombre = "", cs = "";
            var salida = new List<string>();
            if (errores.Count == 0 && arbol.Formato != null)
            {
                generador = new GeneradorDocumento();
                documento = generador.Generar(arbol, lineas);
                nombre = generador.NombreArchivo;
                cs = new GeneradorCSharp().Generar(arbol, nombre);
                salida = Ejecutor.LeerDeVuelta(arbol, documento, nombre);
            }

            return new ResultadoCompilacion
            {
                LineasFuente = lineas,
                Tokens = lexico.Tokens,
                Arbol = arbol,
                Semantico = semantico,
                Generador = generador,
                Errores = errores,
                Documento = documento,
                NombreArchivo = nombre,
                CodigoCSharp = cs,
                Salida = salida
            };
        }
    }
}
