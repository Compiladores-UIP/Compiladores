using System.Globalization;
using CompiladorConfiguracion.Lexico;
using CompiladorConfiguracion.Sintactico;

namespace CompiladorConfiguracion.Semantico
{
    /// <summary>Entrada de la tabla de símbolos: una clave con su ruta completa.</summary>
    public class Entrada
    {
        public required string Ruta { get; init; }
        public required string Tipo { get; init; }
        public required string Valor { get; init; }
        public required int Linea { get; init; }
    }

    /// <summary>
    /// FASE 3 · Análisis semántico.
    /// - Cada clave es única dentro de su sección (la tabla de símbolos usa la ruta completa: servidor.puerto).
    /// - Deduce el tipo de cada valor y lo compara con el tipo anotado (CONFIG puerto : ENTERO = …).
    /// - Las listas no pueden estar vacías ni mezclar tipos (ENTERO y DECIMAL se combinan como DECIMAL).
    /// - Comprueba rangos (ENTERO cabe en int) y las reglas de nombres del formato elegido.
    /// </summary>
    public class AnalizadorSemantico
    {
        public List<Entrada> Tabla { get; } = new();
        public List<string> Verificaciones { get; } = new();
        public List<ErrorCompilacion> Errores { get; } = new();

        private bool _xml;

        public void Analizar(Archivo archivo)
        {
            if (archivo.Formato == null) return;
            _xml = archivo.NombreFormato == "XML";
            Verificaciones.Add($"Formato de salida: {archivo.NombreFormato} (archivo configuracion.{archivo.NombreFormato.ToLowerInvariant()}).");
            Recorrer(archivo.Elementos, "", "el nivel principal");
            if (archivo.Elementos.Count == 0)
                Verificaciones.Add("Aviso: el archivo no tiene claves; se generará una configuración vacía.");
        }

        private void Recorrer(List<Elemento> elementos, string prefijo, string ambito)
        {
            var vistas = new Dictionary<string, int>();
            foreach (var el in elementos)
            {
                string nombre = el.Nombre.Lexema;
                string ruta = prefijo + nombre;

                if (vistas.TryGetValue(nombre, out int lineaPrevia))
                {
                    Error(el.Nombre, $"La clave \"{nombre}\" ya existe en {ambito} (línea {lineaPrevia}).",
                          "Cada clave debe ser única dentro de su sección. Cambia el nombre o borra una de las dos.");
                    continue;
                }
                vistas[nombre] = el.Linea;

                if (_xml && nombre.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
                    Error(el.Nombre, $"En XML los nombres que empiezan con \"xml\" están reservados (\"{nombre}\").",
                          "Cambia el nombre, por ejemplo: config_" + nombre);

                switch (el)
                {
                    case Clave c:
                        AnalizarClave(c, ruta);
                        break;
                    case Seccion s:
                        Verificaciones.Add($"Línea {s.Linea}: sección \"{ruta}\" con {s.Elementos.Count} elemento(s), cerrada en la línea {s.LineaFin}.");
                        if (s.Elementos.Count == 0)
                            Verificaciones.Add($"Aviso: la sección \"{ruta}\" está vacía.");
                        Recorrer(s.Elementos, ruta + ".", $"la sección \"{ruta}\"");
                        break;
                }
            }
        }

        private void AnalizarClave(Clave c, string ruta)
        {
            TipoValor? tipo;
            if (c.Valor is ValorLista lista)
            {
                tipo = TipoValor.Lista;
                var elementos = TipoDeLista(lista, ruta);
                if (elementos == null) return;
                c.TipoElementos = elementos.Value;
            }
            else
            {
                tipo = TipoDe(c.Valor.Token);
                if (tipo == null) return;
            }

            // Tipo anotado: DECIMAL acepta un ENTERO; lo demás debe coincidir
            if (c.TipoAnotado != null)
            {
                TipoValor anotado = Enum.Parse<TipoValor>(Capitalizar(c.TipoAnotado.Lexema));
                bool ok = anotado == tipo || (anotado == TipoValor.Decimal && tipo == TipoValor.Entero);
                if (!ok)
                {
                    Error(c.Valor.Token, $"La clave \"{ruta}\" se declaró como {c.TipoAnotado.Lexema} y su valor es {Nombre(tipo.Value)}.",
                          Pista(anotado, c.Valor.Token));
                    return;
                }
                tipo = anotado;
                Verificaciones.Add($"Línea {c.Linea}: \"{ruta}\" cumple el tipo anotado {c.TipoAnotado.Lexema}.");
            }

            c.Tipo = tipo.Value;
            string tipoTexto = c.Tipo == TipoValor.Lista ? $"LISTA de {Nombre(c.TipoElementos)}" : Nombre(c.Tipo);
            Tabla.Add(new Entrada { Ruta = ruta, Tipo = tipoTexto, Valor = Mostrar(c), Linea = c.Linea });
            if (c.TipoAnotado == null)
                Verificaciones.Add($"Línea {c.Linea}: \"{ruta}\" es {tipoTexto}.");
        }

        /// <summary>Tipo de un valor simple, con control de rangos. null si hay error.</summary>
        private TipoValor? TipoDe(Token t)
        {
            switch (t.Tipo)
            {
                case TipoToken.Texto: return TipoValor.Texto;
                case TipoToken.Booleano: return TipoValor.Booleano;
                case TipoToken.Entero:
                    if (!int.TryParse(t.Lexema, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
                    {
                        Error(t, $"El número {t.Lexema} no cabe en un ENTERO (de {int.MinValue} a {int.MaxValue}).",
                              $"Escríbelo como decimal ({t.Lexema}.0) o como texto (\"{t.Lexema}\").");
                        return null;
                    }
                    return TipoValor.Entero;
                case TipoToken.Decimal:
                    if (double.IsInfinity(double.Parse(t.Lexema, CultureInfo.InvariantCulture)))
                    {
                        Error(t, $"El número {t.Lexema} es demasiado grande para un DECIMAL.", "Escríbelo como texto entre comillas.");
                        return null;
                    }
                    return TipoValor.Decimal;
            }
            return null;
        }

        private TipoValor? TipoDeLista(ValorLista lista, string ruta)
        {
            if (lista.Elementos.Count == 0)
            {
                Error(lista.Token, $"La lista \"{ruta}\" está vacía y no se puede saber de qué tipo es.",
                      "Agrega al menos un valor o borra la clave.");
                return null;
            }
            var tipos = new List<TipoValor>();
            foreach (var e in lista.Elementos)
            {
                var t = TipoDe(e);
                if (t == null) return null;
                tipos.Add(t.Value);
            }
            var distintos = tipos.Distinct().ToList();
            if (distintos.Count == 1) return distintos[0];
            if (distintos.All(t => t is TipoValor.Entero or TipoValor.Decimal))
            {
                Verificaciones.Add($"Línea {lista.Token.Linea}: la lista \"{ruta}\" combina ENTERO y DECIMAL: se toma como DECIMAL.");
                return TipoValor.Decimal;
            }
            static string Familia(TipoValor t) => t is TipoValor.Entero or TipoValor.Decimal ? "número" : t.ToString();
            int i = tipos.FindIndex(t => Familia(t) != Familia(tipos[0]));
            Token malo = lista.Elementos[i];
            Error(malo, $"La lista \"{ruta}\" mezcla {Nombre(tipos[0])} y {Nombre(tipos[i])}.",
                  "Todos los valores de una lista deben ser del mismo tipo.");
            return null;
        }

        private static string Pista(TipoValor anotado, Token valor) => anotado switch
        {
            TipoValor.Texto => $"Pon el valor entre comillas: \"{valor.Valor}\"",
            TipoValor.Entero when valor.Tipo == TipoToken.Decimal => "Un ENTERO no lleva decimales. Quita la parte decimal o anota DECIMAL.",
            TipoValor.Entero or TipoValor.Decimal when valor.Tipo == TipoToken.Texto => $"Quita las comillas si es un número: {valor.Valor}",
            TipoValor.Booleano => "Un BOOLEANO solo puede ser verdadero o falso.",
            TipoValor.Lista => "Una LISTA se escribe entre corchetes: [1, 2, 3]",
            _ => "Corrige el valor o el tipo anotado."
        };

        // ------------------------------------------------------------ Formato de valores

        public static string Nombre(TipoValor t) => t.ToString().ToUpperInvariant();
        private static string Capitalizar(string s) => s[0] + s[1..].ToLowerInvariant();

        /// <summary>Valor tal como se muestra en la tabla de símbolos.</summary>
        public static string Mostrar(Clave c) => c.Valor switch
        {
            ValorLista l => "[" + string.Join(", ", l.Elementos.Select(e => Literal(e, c.TipoElementos))) + "]",
            _ => Literal(c.Valor.Token, c.Tipo)
        };

        private static string Literal(Token t, TipoValor tipo) => tipo switch
        {
            TipoValor.Texto => t.Lexema,
            TipoValor.Booleano => Booleano(t) ? "verdadero" : "falso",
            _ => Numero(t, tipo)
        };

        public static bool Booleano(Token t) => t.Lexema.Equals("verdadero", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Número normalizado para JSON/XML: sin ceros a la izquierda y, si es DECIMAL, siempre con punto
        /// (3100 como DECIMAL se escribe 3100.0 para conservar el tipo).
        /// </summary>
        public static string Numero(Token t, TipoValor tipo)
        {
            if (tipo == TipoValor.Entero)
                return int.Parse(t.Lexema, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            string d = double.Parse(t.Lexema, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);
            return d.Contains('.') || d.Contains('E') ? d : d + ".0";
        }

        private void Error(Token t, string mensaje, string sugerencia) =>
            Errores.Add(new ErrorCompilacion(Fase.Semantico, t.Linea, t.Columna, mensaje, sugerencia));
    }
}
