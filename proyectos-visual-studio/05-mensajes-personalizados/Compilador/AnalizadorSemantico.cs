namespace MensajesPersonalizados.Compilador;

public sealed class Simbolo
{
    public required string Nombre { get; init; }
    public required TipoDato Tipo { get; init; }
    public required int Linea { get; init; }
    public bool Usado { get; set; }
}

/// <summary>
/// Fase 3. Revisa el significado del programa: variables declaradas, nombres únicos,
/// tipos compatibles y marcadores de plantilla que existan.
/// A diferencia de las fases anteriores, reúne todos los errores en lugar de detenerse en el primero.
/// </summary>
public sealed class AnalizadorSemantico
{
    private readonly Dictionary<string, Simbolo> _tabla = new(StringComparer.Ordinal);
    private readonly List<Diagnostico> _errores = new();
    private readonly List<string> _avisos = new();

    public IReadOnlyCollection<Simbolo> Simbolos => _tabla.Values;
    public IReadOnlyList<Diagnostico> Errores => _errores;
    public IReadOnlyList<string> Avisos => _avisos;

    /// <summary>Tipo final de cada expresión; el generador lo usa para decidir la traducción.</summary>
    public Dictionary<Expresion, TipoDato> TiposExpresion { get; } = new(ReferenceEqualityComparer.Instance);

    public void Analizar(ProgramaFuente programa)
    {
        foreach (Instruccion instruccion in programa.Instrucciones)
        {
            switch (instruccion)
            {
                case Declaracion d:
                    RevisarDeclaracion(d);
                    break;
                case Asignacion a:
                    RevisarAsignacion(a);
                    break;
                case Mostrar m:
                    Tipo(m.Valor);
                    break;
                case Pedir p:
                    if (Buscar(p.Nombre, p.Linea, p.Columna) is Simbolo s)
                    {
                        s.Usado = true;
                    }
                    break;
            }
        }

        foreach (Simbolo s in _tabla.Values.Where(s => !s.Usado))
        {
            _avisos.Add($"Aviso (línea {s.Linea}): la variable '{s.Nombre}' se declaró pero no se usa en ningún mensaje.");
        }
    }

    private void RevisarDeclaracion(Declaracion d)
    {
        // El valor se revisa antes de registrar el nombre: "TEXTO a = a" es un error.
        TipoDato? tipoValor = d.Valor is null ? null : Tipo(d.Valor);

        if (_tabla.TryGetValue(d.Nombre, out Simbolo? previo))
        {
            Error($"la variable '{d.Nombre}' ya fue declarada en la línea {previo.Linea}.", d.Linea, d.Columna);
            return;
        }

        if (tipoValor is TipoDato t && !EsCompatible(d.Tipo, t))
        {
            Error($"no se puede guardar un valor {t} en la variable {d.Tipo} '{d.Nombre}'.{Sugerencia(d.Tipo, t)}", d.Linea, d.Columna);
        }

        _tabla[d.Nombre] = new Simbolo { Nombre = d.Nombre, Tipo = d.Tipo, Linea = d.Linea };
    }

    private void RevisarAsignacion(Asignacion a)
    {
        TipoDato? tipoValor = Tipo(a.Valor);
        if (Buscar(a.Nombre, a.Linea, a.Columna) is not Simbolo s || tipoValor is not TipoDato t)
        {
            return;
        }

        if (!EsCompatible(s.Tipo, t))
        {
            Error($"no se puede asignar un valor {t} a la variable {s.Tipo} '{s.Nombre}'.{Sugerencia(s.Tipo, t)}", a.Linea, a.Columna);
        }
    }

    /// <summary>Calcula el tipo de una expresión. Devuelve null si contiene errores.</summary>
    private TipoDato? Tipo(Expresion e)
    {
        TipoDato? tipo = e switch
        {
            LiteralNumero n => n.EsDecimal ? TipoDato.DECIMAL : TipoDato.ENTERO,
            LiteralCadena c => TipoCadena(c),
            Variable v => Buscar(v.Nombre, v.Linea, v.Columna) is Simbolo s ? Usar(s) : null,
            Agrupacion g => Tipo(g.Interna),
            LlamadaFuncion f => TipoFuncion(f),
            Binaria b => TipoBinaria(b),
            _ => null
        };

        if (tipo is TipoDato t)
        {
            TiposExpresion[e] = t;
        }
        return tipo;
    }

    private TipoDato? TipoCadena(LiteralCadena c)
    {
        bool valido = true;
        foreach (SegmentoVariable marcador in c.Segmentos.OfType<SegmentoVariable>())
        {
            if (_tabla.TryGetValue(marcador.Nombre, out Simbolo? s))
            {
                s.Usado = true;
            }
            else
            {
                Error($"el marcador {{{marcador.Nombre}}} usa una variable que no ha sido declarada.", marcador.Linea, marcador.Columna);
                valido = false;
            }
        }
        return valido ? TipoDato.TEXTO : null;
    }

    private TipoDato? TipoFuncion(LlamadaFuncion f)
    {
        TipoDato? argumento = Tipo(f.Argumento);
        if (argumento is null)
        {
            return null;
        }

        if (argumento != TipoDato.TEXTO)
        {
            Error($"{f.Funcion} necesita un TEXTO y recibió un {argumento}.", f.Linea, f.Columna);
            return null;
        }

        return f.Funcion == "LONGITUD" ? TipoDato.ENTERO : TipoDato.TEXTO;
    }

    private TipoDato? TipoBinaria(Binaria b)
    {
        TipoDato? izq = Tipo(b.Izquierda);
        TipoDato? der = Tipo(b.Derecha);
        if (izq is not TipoDato i || der is not TipoDato d)
        {
            return null;
        }

        if (b.Operador == "+" && (i == TipoDato.TEXTO || d == TipoDato.TEXTO))
        {
            return TipoDato.TEXTO; // concatenación: el número se convierte en texto
        }

        if (i == TipoDato.TEXTO || d == TipoDato.TEXTO)
        {
            Error($"el operador '{b.Operador}' solo funciona con números; para unir textos use '+'.", b.Linea, b.Columna);
            return null;
        }

        if (b.Operador == "/" && b.Derecha is LiteralNumero { Lexema: var lexema } && double.Parse(lexema, System.Globalization.CultureInfo.InvariantCulture) == 0)
        {
            Error("división entre cero.", b.Linea, b.Columna);
            return null;
        }

        return i == TipoDato.DECIMAL || d == TipoDato.DECIMAL ? TipoDato.DECIMAL : TipoDato.ENTERO;
    }

    private Simbolo? Buscar(string nombre, int linea, int columna)
    {
        if (_tabla.TryGetValue(nombre, out Simbolo? s))
        {
            return s;
        }

        Error($"la variable '{nombre}' no ha sido declarada. Declárela antes con TEXTO, ENTERO o DECIMAL.", linea, columna);
        return null;
    }

    private static TipoDato Usar(Simbolo s)
    {
        s.Usado = true;
        return s.Tipo;
    }

    private static bool EsCompatible(TipoDato destino, TipoDato valor) =>
        destino == valor || (destino == TipoDato.DECIMAL && valor == TipoDato.ENTERO);

    private static string Sugerencia(TipoDato destino, TipoDato valor) => (destino, valor) switch
    {
        (TipoDato.TEXTO, _) => " Para convertir un número en texto puede escribir \"\" + numero.",
        (TipoDato.ENTERO, TipoDato.DECIMAL) => " Declare la variable como DECIMAL.",
        _ => ""
    };

    private void Error(string mensaje, int linea, int columna) =>
        _errores.Add(new Diagnostico(FaseError.Semantico, mensaje, linea, columna));
}
