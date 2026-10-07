using CompiladorConsultas.Lexico;
using CompiladorConsultas.Sintactico;

namespace CompiladorConsultas.Semantico
{
    /// <summary>
    /// FASE 3 · Análisis semántico.
    /// Revisa el significado de cada consulta contra la base de datos:
    ///  - la tabla existe;
    ///  - cada campo usado pertenece a esa tabla;
    ///  - el valor de cada condición es del mismo tipo que el campo;
    ///  - los textos solo se comparan con = o &lt;&gt;.
    /// </summary>
    public class AnalizadorSemantico
    {
        public List<ErrorCompilacion> Errores { get; } = new();
        public List<string> Verificaciones { get; } = new();

        /// <summary>Consultas que pasaron todas las validaciones.</summary>
        public List<Consulta> Validas { get; } = new();

        public void Analizar(List<Consulta> consultas)
        {
            foreach (var q in consultas)
            {
                int erroresAntes = Errores.Count;
                var notas = new List<string>();
                Revisar(q, notas);
                if (Errores.Count == erroresAntes)
                {
                    Validas.Add(q);
                    Verificaciones.AddRange(notas.Select(n => $"Línea {q.Linea}: {n}"));
                }
            }
        }

        private void Revisar(Consulta q, List<string> notas)
        {
            var tabla = BaseDeDatos.BuscarTabla(q.Tabla.Lexema);
            if (tabla == null)
            {
                string disponibles = string.Join(", ", BaseDeDatos.Tablas.Select(t => t.Nombre));
                string pista = BaseDeDatos.BuscarTabla(q.Tabla.Lexema.TrimEnd('s')) is { } parecida
                    ? $"¿Quisiste decir \"{parecida.Nombre}\"? Las tablas disponibles son: {disponibles}."
                    : $"Las tablas disponibles son: {disponibles}.";
                Error(q.Tabla, $"La tabla \"{q.Tabla.Lexema}\" no existe.", pista);
                return;
            }
            notas.Add($"la tabla \"{tabla.Nombre}\" existe.");

            foreach (var campo in q.Campos)
                if (CampoValido(tabla, campo)) notas.Add($"el campo \"{campo.Lexema}\" pertenece a {tabla.Nombre}.");

            foreach (var c in q.Condiciones)
            {
                if (!CampoValido(tabla, c.Campo)) continue;
                var campo = tabla.BuscarCampo(c.Campo.Lexema)!;
                bool esNumero = c.Valor.Tipo is TipoToken.NumeroEntero or TipoToken.NumeroDecimal;

                if (campo.Tipo == TipoCampo.Texto && esNumero)
                {
                    Error(c.Valor, $"El campo \"{campo.Nombre}\" es de texto y se compara con un número.",
                        $"Escribe el valor entre comillas, por ejemplo: {campo.Nombre} = \"{c.Valor.Lexema}\"");
                    continue;
                }
                if (campo.Tipo != TipoCampo.Texto && !esNumero)
                {
                    Error(c.Valor, $"El campo \"{campo.Nombre}\" es numérico y se compara con un texto.",
                        $"Quita las comillas y escribe un número, por ejemplo: {campo.Nombre} > 18");
                    continue;
                }
                if (campo.Tipo == TipoCampo.Entero && c.Valor.Tipo == TipoToken.NumeroDecimal)
                {
                    Error(c.Valor, $"El campo \"{campo.Nombre}\" es ENTERO y el valor {c.Valor.Lexema} tiene decimales.",
                        "Usa un número entero.");
                    continue;
                }
                if (campo.Tipo == TipoCampo.Texto && c.Operador.Lexema is not ("=" or "<>" or "!="))
                {
                    Error(c.Operador, $"El operador \"{c.Operador.Lexema}\" no se puede usar con el texto \"{campo.Nombre}\".",
                        "Para textos usa = (igual) o <> (diferente).");
                    continue;
                }
                notas.Add($"la condición \"{c.Campo.Lexema} {c.Operador.Lexema} {c.Valor.Lexema}\" compara tipos compatibles.");
            }

            if (q.OrdenCampo != null && CampoValido(tabla, q.OrdenCampo))
                notas.Add($"se puede ordenar por \"{q.OrdenCampo.Lexema}\".");
        }

        private bool CampoValido(Tabla tabla, Token campo)
        {
            if (tabla.BuscarCampo(campo.Lexema) != null) return true;
            Error(campo, $"La tabla \"{tabla.Nombre}\" no tiene un campo llamado \"{campo.Lexema}\".",
                $"Campos disponibles: {string.Join(", ", tabla.Campos.Select(c => c.Nombre))}.");
            return false;
        }

        private void Error(Token token, string mensaje, string pista) =>
            Errores.Add(new ErrorCompilacion(Fase.Semantico, token.Linea, token.Columna, mensaje, pista));
    }
}
