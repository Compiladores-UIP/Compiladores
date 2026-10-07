using CompiladorCalculadora.Sintactico;

namespace CompiladorCalculadora.Semantico
{
    /// <summary>Entrada de la tabla de símbolos.</summary>
    public class Simbolo
    {
        public required string Nombre { get; init; }
        public required TipoDato Tipo { get; init; }
        public required int Linea { get; init; }
        /// <summary>Último valor conocido (null si una operación anterior falló).</summary>
        public Valor? Valor { get; set; }
        public int Usos { get; set; }

        public string TipoFuente => Tipo == TipoDato.Entero ? "ENTERO" : "DECIMAL";
        public string TipoCSharp => Tipo == TipoDato.Entero ? "int" : "double";
    }

    /// <summary>
    /// FASE 3 · Análisis semántico.
    /// - Construye la tabla de símbolos (variables, tipo y valor).
    /// - Calcula el tipo de cada expresión: ENTERO op ENTERO = ENTERO; si hay un DECIMAL, el resultado es DECIMAL;
    ///   la división, la potencia y RAIZ siempre dan DECIMAL.
    /// - Como el programa no lee datos del usuario, todos los valores se conocen al compilar.
    ///   Por eso puede detectar la división entre cero, la raíz de un negativo y los desbordamientos.
    /// </summary>
    public class AnalizadorSemantico
    {
        public Dictionary<string, Simbolo> Tabla { get; } = new();
        public List<string> Verificaciones { get; } = new();
        public List<ErrorCompilacion> Errores { get; } = new();

        // Palabras reservadas de C#: no pueden ser nombres de variable en el programa generado.
        private static readonly HashSet<string> ReservadasCSharp = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
            "Console", "Math", "Program", "Main", "CultureInfo"
        };

        public void Analizar(Programa programa)
        {
            foreach (var s in programa.Sentencias)
            {
                switch (s)
                {
                    case Declaracion d: Declarar(d); break;
                    case Asignacion a: Asignar(a); break;
                    case Impresion i:
                        if (i.Valor != null)
                        {
                            var v = Evaluar(i.Valor);
                            if (v != null) Verificaciones.Add($"Línea {i.Linea}: IMPRIMIR {Fuente(i.Valor)} da {v} ({Nombre(i.Valor.Tipo)}).");
                        }
                        break;
                }
            }

            foreach (var s in Tabla.Values.Where(s => s.Usos == 0 && s.Valor != null))
                Verificaciones.Add($"Aviso: la variable \"{s.Nombre}\" se declara pero no se usa.");
        }

        private void Declarar(Declaracion d)
        {
            string nombre = d.Nombre.Lexema;
            if (ReservadasCSharp.Contains(nombre))
            {
                Error(d.Nombre.Linea, d.Nombre.Columna, $"\"{nombre}\" es una palabra reservada de C# y no puede ser nombre de variable.",
                      $"Usa otro nombre, por ejemplo {nombre}1 o mi{char.ToUpperInvariant(nombre[0])}{nombre[1..]}.");
                Evaluar(d.Valor);
                return;
            }
            if (Tabla.TryGetValue(nombre, out var previo))
            {
                Error(d.Nombre.Linea, d.Nombre.Columna, $"La variable \"{nombre}\" ya fue declarada en la línea {previo.Linea}.",
                      $"Para cambiar su valor escribe solo: {nombre} = …");
                Evaluar(d.Valor);
                return;
            }

            Valor? valor = Evaluar(d.Valor);
            var simbolo = new Simbolo { Nombre = nombre, Tipo = d.TipoDeclarado, Linea = d.Linea };
            Tabla[nombre] = simbolo;   // se registra aunque el valor tenga errores, para no repetir "no declarada"

            if (valor == null) return;
            if (!Compatible(d.TipoDeclarado, d.Valor.Tipo, d.Valor, d.Nombre.Linea)) return;

            simbolo.Valor = valor.Value with { Tipo = d.TipoDeclarado };
            Verificaciones.Add($"Línea {d.Linea}: \"{nombre}\" se declara como {simbolo.TipoFuente} ({simbolo.TipoCSharp}) con valor {simbolo.Valor}.");
        }

        private void Asignar(Asignacion a)
        {
            string nombre = a.Nombre.Lexema;
            Valor? valor = Evaluar(a.Valor);
            if (!Tabla.TryGetValue(nombre, out var simbolo))
            {
                Error(a.Nombre.Linea, a.Nombre.Columna, $"La variable \"{nombre}\" no ha sido declarada.",
                      $"Declárala primero: DECIMAL {nombre} = …");
                return;
            }
            if (valor == null) { simbolo.Valor = null; return; }
            if (!Compatible(simbolo.Tipo, a.Valor.Tipo, a.Valor, a.Linea)) return;

            simbolo.Valor = valor.Value with { Tipo = simbolo.Tipo };
            Verificaciones.Add($"Línea {a.Linea}: \"{nombre}\" cambia a {simbolo.Valor}.");
        }

        /// <summary>Un DECIMAL acepta un ENTERO; un ENTERO no acepta un DECIMAL (se perderían los decimales).</summary>
        private bool Compatible(TipoDato destino, TipoDato origen, Expresion e, int linea)
        {
            if (destino == TipoDato.Decimal || origen == TipoDato.Entero) return true;
            string porque = e is Binaria { Operador: "/" } ? " La división siempre da un DECIMAL."
                          : e is Binaria { Operador: "^" } ? " La potencia siempre da un DECIMAL."
                          : e is Llamada { Funcion: "RAIZ" } ? " RAIZ siempre da un DECIMAL." : "";
            Error(linea, Inicio(e).Columna, $"No se puede guardar un valor DECIMAL ({Fuente(e)}) en una variable ENTERO.{porque}",
                  "Declara la variable como DECIMAL.");
            return false;
        }

        /// <summary>
        /// Calcula el tipo de la expresión (lo anota en el árbol) y, si se puede, su valor.
        /// Devuelve null cuando hay un error en la expresión o depende de un valor desconocido.
        /// </summary>
        private Valor? Evaluar(Expresion e)
        {
            switch (e)
            {
                case Literal l:
                {
                    l.Tipo = l.EsDecimal ? TipoDato.Decimal : TipoDato.Entero;
                    double n = double.Parse(l.Token.Lexema, System.Globalization.CultureInfo.InvariantCulture);
                    if (!l.EsDecimal && n > int.MaxValue)
                    {
                        Error(l.Token.Linea, l.Token.Columna, $"El número {l.Token.Lexema} es demasiado grande para un ENTERO (máximo {int.MaxValue}).",
                              $"Escríbelo como decimal: {l.Token.Lexema}.0");
                        return null;
                    }
                    return new Valor(l.Tipo, n);
                }

                case Variable v:
                {
                    if (!Tabla.TryGetValue(v.Nombre, out var s))
                    {
                        string? parecido = Tabla.Keys.FirstOrDefault(k => string.Equals(k, v.Nombre, StringComparison.OrdinalIgnoreCase));
                        Error(v.Token.Linea, v.Token.Columna, $"La variable \"{v.Nombre}\" no ha sido declarada.",
                              parecido != null ? $"¿Quisiste decir \"{parecido}\"? Las mayúsculas y minúsculas cuentan."
                                               : $"Declárala antes de usarla: DECIMAL {v.Nombre} = 0");
                        v.Tipo = TipoDato.Decimal;
                        return null;
                    }
                    s.Usos++;
                    v.Tipo = s.Tipo;
                    return s.Valor;
                }

                case Grupo g:
                {
                    var r = Evaluar(g.Interior);
                    g.Tipo = g.Interior.Tipo;
                    return r;
                }

                case Negacion n:
                {
                    var r = Evaluar(n.Operando);
                    n.Tipo = n.Operando.Tipo;
                    return r == null ? null : Aritmetica.Negar(r.Value);
                }

                case Llamada f:
                {
                    var x = Evaluar(f.Argumento);
                    f.Tipo = f.Funcion == "RAIZ" ? TipoDato.Decimal : f.Argumento.Tipo;
                    if (x == null) return null;
                    var (res, err, pista) = Aritmetica.Funcion(f.Funcion, x.Value);
                    if (err != null) { Error(f.Token.Linea, f.Token.Columna, err, pista); return null; }
                    return res;
                }

                case Binaria b:
                {
                    var a = Evaluar(b.Izquierda);
                    var c = Evaluar(b.Derecha);
                    bool enteros = b.Izquierda.Tipo == TipoDato.Entero && b.Derecha.Tipo == TipoDato.Entero;
                    b.Tipo = b.Operador is "/" or "^" || !enteros ? TipoDato.Decimal : TipoDato.Entero;
                    if (a == null || c == null) return null;

                    var (res, err, pista) = Aritmetica.Binaria(b.Operador, a.Value, c.Value, Fuente(b.Derecha));
                    if (err != null) { Error(b.Token.Linea, b.Token.Columna, err, pista); return null; }
                    if (b.Operador is "/" or "%")
                        Verificaciones.Add($"Línea {b.Token.Linea}: el divisor de \"{Fuente(b)}\" es {c} (distinto de cero).");
                    return res;
                }
            }
            throw new InvalidOperationException("Expresión desconocida");
        }

        private void Error(int linea, int columna, string mensaje, string sugerencia) =>
            Errores.Add(new ErrorCompilacion(Fase.Semantico, linea, columna, mensaje, sugerencia));

        private static string Nombre(TipoDato t) => t == TipoDato.Entero ? "ENTERO" : "DECIMAL";

        /// <summary>Primer token de una expresión (para señalar dónde empieza).</summary>
        public static Lexico.Token Inicio(Expresion e) => e switch
        {
            Binaria b => Inicio(b.Izquierda),
            _ => e.Token
        };

        /// <summary>Vuelve a escribir una expresión en el lenguaje fuente (para mensajes y el árbol).</summary>
        public static string Fuente(Expresion e) => e switch
        {
            Literal l => l.Token.Lexema,
            Variable v => v.Nombre,
            Grupo g => "(" + Fuente(g.Interior) + ")",
            Negacion n => "-" + Fuente(n.Operando),
            Llamada f => $"{f.Funcion}({Fuente(f.Argumento)})",
            Binaria b => $"{Fuente(b.Izquierda)} {b.Operador} {Fuente(b.Derecha)}",
            _ => "?"
        };
    }
}
