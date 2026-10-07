using CompiladorConsultas.Lexico;

namespace CompiladorConsultas.Sintactico
{
    /// <summary>
    /// FASE 2 · Análisis sintáctico (descendente recursivo).
    ///
    /// Gramática (BNF):
    ///   &lt;consulta&gt;  ::= BUSCAR &lt;origen&gt; [ DONDE &lt;filtro&gt; ] [ ORDENAR POR IDENTIFICADOR [ ASC | DESC ] ] [ ";" ]
    ///   &lt;origen&gt;    ::= IDENTIFICADOR                               (todos los campos)
    ///                | &lt;campos&gt; DE IDENTIFICADOR                    (campos elegidos)
    ///   &lt;campos&gt;    ::= IDENTIFICADOR { "," IDENTIFICADOR }
    ///   &lt;filtro&gt;    ::= &lt;condicion&gt; { ( Y | O ) &lt;condicion&gt; }
    ///   &lt;condicion&gt; ::= IDENTIFICADOR &lt;operador&gt; &lt;valor&gt;
    ///   &lt;operador&gt;  ::= "&gt;" | "&lt;" | "&gt;=" | "&lt;=" | "=" | "&lt;&gt;" | "!="
    ///   &lt;valor&gt;     ::= NUMERO | DECIMAL | TEXTO
    /// </summary>
    public class AnalizadorSintactico
    {
        public List<ErrorCompilacion> Errores { get; } = new();

        private List<Token> t = new();
        private int p;
        private int linea;
        private int finColumna;

        /// <summary>Señal interna para cortar el análisis de la línea al primer error.</summary>
        private class ErrorDeLinea : Exception { }

        public List<Consulta> Analizar(List<Token> tokens, string[] lineasFuente, HashSet<int> lineasConErrorLexico)
        {
            var consultas = new List<Consulta>();
            foreach (var grupo in tokens.GroupBy(x => x.Linea).OrderBy(g => g.Key))
            {
                if (lineasConErrorLexico.Contains(grupo.Key)) continue;
                t = grupo.ToList();
                p = 0;
                linea = grupo.Key;
                finColumna = t[^1].Columna + t[^1].Lexema.Length;
                try { consultas.Add(AnalizarConsulta(lineasFuente[linea - 1].Trim())); }
                catch (ErrorDeLinea) { /* el error ya quedó registrado */ }
            }
            return consultas;
        }

        private Token? Actual => p < t.Count ? t[p] : null;

        private Consulta AnalizarConsulta(string fuente)
        {
            var q = new Consulta { Linea = linea, Fuente = fuente };

            // BUSCAR
            if (Actual!.Tipo != TipoToken.Buscar)
            {
                if (Actual.Lexema.Equals("BUSCAR", StringComparison.OrdinalIgnoreCase))
                    Fallar(Actual, $"\"{Actual.Lexema}\" no se reconoce como palabra clave.", "Las palabras clave van en mayúsculas: BUSCAR.");
                Fallar(Actual, $"Una consulta debe empezar con BUSCAR y empieza con \"{Actual.Lexema}\".",
                    "Ejemplo: BUSCAR estudiante DONDE edad > 18");
            }
            p++;

            // <origen>
            Token primero = Esperar(TipoToken.Identificador, "Falta el nombre de la tabla después de BUSCAR.",
                "Ejemplo: BUSCAR estudiante");

            if (Actual?.Tipo is TipoToken.Coma or TipoToken.De)
            {
                q.Campos.Add(primero);
                while (Actual?.Tipo == TipoToken.Coma)
                {
                    p++;
                    q.Campos.Add(Esperar(TipoToken.Identificador, "Falta un nombre de campo después de la coma.",
                        "Ejemplo: BUSCAR nombre, edad DE estudiante"));
                }
                if (Actual?.Tipo != TipoToken.De)
                    Fallar(Actual, "Después de la lista de campos se esperaba DE.", "Ejemplo: BUSCAR nombre, edad DE estudiante");
                p++;
                q.Tabla = Esperar(TipoToken.Identificador, "Falta el nombre de la tabla después de DE.",
                    "Ejemplo: BUSCAR nombre DE estudiante");
            }
            else q.Tabla = primero;

            // [ DONDE <filtro> ]
            if (Actual?.Tipo == TipoToken.Donde)
            {
                p++;
                q.Condiciones.Add(AnalizarCondicion());
                while (Actual?.Tipo is TipoToken.Y or TipoToken.O)
                {
                    q.Conectores.Add(Actual);
                    p++;
                    q.Condiciones.Add(AnalizarCondicion());
                }
            }

            // [ ORDENAR POR campo [ASC|DESC] ]
            if (Actual?.Tipo == TipoToken.Ordenar)
            {
                p++;
                if (Actual?.Tipo != TipoToken.Por) Fallar(Actual, "Después de ORDENAR se esperaba POR.", "Ejemplo: ORDENAR POR edad DESC");
                p++;
                q.OrdenCampo = Esperar(TipoToken.Identificador, "Falta el campo por el que se va a ordenar.", "Ejemplo: ORDENAR POR edad");
                if (Actual?.Tipo is TipoToken.Asc or TipoToken.Desc)
                {
                    q.OrdenDescendente = Actual.Tipo == TipoToken.Desc;
                    p++;
                }
            }

            if (Actual?.Tipo == TipoToken.FinSentencia) p++;

            if (Actual != null)
            {
                string pista = Actual.Lexema.ToUpperInvariant() switch
                {
                    "DONDE" or "Y" or "O" or "ORDENAR" or "DE" => $"Las palabras clave van en mayúsculas: {Actual.Lexema.ToUpperInvariant()}.",
                    _ when q.Condiciones.Count > 0 => "Para unir condiciones usa Y u O.",
                    _ => "Para filtrar usa DONDE, por ejemplo: BUSCAR estudiante DONDE edad > 18"
                };
                Fallar(Actual, $"No se esperaba \"{Actual.Lexema}\" en esta posición.", pista);
            }
            return q;
        }

        private Condicion AnalizarCondicion()
        {
            Token campo = Esperar(TipoToken.Identificador, "Se esperaba el nombre de un campo en la condición.",
                "Ejemplo: DONDE edad > 18");
            if (Actual?.Tipo != TipoToken.Operador)
                Fallar(Actual, $"Falta el operador de comparación después de \"{campo.Lexema}\".",
                    "Usa uno de estos: >  <  >=  <=  =  <>");
            Token op = t[p++];
            if (Actual == null || !Actual.EsValor)
            {
                string pista = Actual?.Tipo == TipoToken.Identificador
                    ? $"Si \"{Actual.Lexema}\" es un texto, escríbelo entre comillas: \"{Actual.Lexema}\"."
                    : "El valor puede ser un número o un texto entre comillas.";
                Fallar(Actual, $"Falta el valor después de \"{op.Lexema}\".", pista);
            }
            Token valor = t[p++];
            return new Condicion { Campo = campo, Operador = op, Valor = valor };
        }

        private Token Esperar(TipoToken tipo, string mensaje, string pista)
        {
            if (Actual?.Tipo != tipo)
            {
                if (Actual != null && Actual.EsPalabraReservada)
                    Fallar(Actual, $"\"{Actual.Lexema}\" es una palabra reservada y no puede usarse aquí. {mensaje}", pista);
                Fallar(Actual, mensaje, pista);
            }
            return t[p++];
        }

        private void Fallar(Token? token, string mensaje, string pista)
        {
            Errores.Add(new ErrorCompilacion(Fase.Sintactico, linea, token?.Columna ?? finColumna, mensaje, pista));
            throw new ErrorDeLinea();
        }
    }
}
