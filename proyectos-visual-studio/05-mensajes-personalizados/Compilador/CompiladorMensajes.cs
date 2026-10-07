using System.Text;

namespace MensajesPersonalizados.Compilador;

public sealed class ResultadoCompilacion
{
    public List<Token> Tokens { get; init; } = new();
    public ProgramaFuente? Arbol { get; init; }
    public IReadOnlyCollection<Simbolo> Simbolos { get; init; } = Array.Empty<Simbolo>();
    public List<Diagnostico> Errores { get; init; } = new();
    public IReadOnlyList<string> Avisos { get; init; } = Array.Empty<string>();
    public string? CodigoCSharp { get; init; }

    public bool Exitoso => Errores.Count == 0 && CodigoCSharp is not null;
}

/// <summary>Une las cuatro fases: léxico → sintáctico → semántico → generación de C#.</summary>
public static class CompiladorMensajes
{
    public static ResultadoCompilacion Compilar(string fuente)
    {
        List<Token> tokens;
        try
        {
            tokens = new AnalizadorLexico(fuente).Analizar();
        }
        catch (ErrorCompilacion ex)
        {
            return new ResultadoCompilacion { Errores = { ex.Diagnostico } };
        }

        ProgramaFuente arbol;
        try
        {
            arbol = new AnalizadorSintactico(tokens).Analizar();
        }
        catch (ErrorCompilacion ex)
        {
            return new ResultadoCompilacion { Tokens = tokens, Errores = { ex.Diagnostico } };
        }

        var semantico = new AnalizadorSemantico();
        semantico.Analizar(arbol);
        if (semantico.Errores.Count > 0)
        {
            return new ResultadoCompilacion
            {
                Tokens = tokens,
                Arbol = arbol,
                Simbolos = semantico.Simbolos,
                Errores = semantico.Errores.ToList(),
                Avisos = semantico.Avisos
            };
        }

        string codigo = new GeneradorCSharp(semantico.TiposExpresion).Generar(arbol);
        return new ResultadoCompilacion
        {
            Tokens = tokens,
            Arbol = arbol,
            Simbolos = semantico.Simbolos,
            Avisos = semantico.Avisos,
            CodigoCSharp = codigo
        };
    }

    public static string TablaTokens(IEnumerable<Token> tokens)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{"LÍN:COL",-7} {"TIPO",-18} LEXEMA");
        foreach (Token t in tokens)
        {
            sb.AppendLine(t.ToString());
        }
        return sb.ToString();
    }

    public static string TablaSimbolos(IEnumerable<Simbolo> simbolos)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{"NOMBRE",-16} {"TIPO",-8} {"LÍNEA",-6} USADA");
        foreach (Simbolo s in simbolos)
        {
            sb.AppendLine($"{s.Nombre,-16} {s.Tipo,-8} {s.Linea,-6} {(s.Usado ? "sí" : "no")}");
        }
        return sb.ToString();
    }

    /// <summary>Representación en texto del árbol de sintaxis abstracta.</summary>
    public static string DibujarArbol(ProgramaFuente programa)
    {
        var sb = new StringBuilder("Programa").AppendLine();
        for (int i = 0; i < programa.Instrucciones.Count; i++)
        {
            Dibujar(sb, programa.Instrucciones[i], "", i == programa.Instrucciones.Count - 1);
        }
        return sb.ToString();
    }

    private static void Dibujar(StringBuilder sb, object nodo, string sangria, bool ultimo)
    {
        sb.Append(sangria).Append(ultimo ? "└── " : "├── ").AppendLine(Etiqueta(nodo));
        string hija = sangria + (ultimo ? "    " : "│   ");
        List<object> hijos = Hijos(nodo);
        for (int i = 0; i < hijos.Count; i++)
        {
            Dibujar(sb, hijos[i], hija, i == hijos.Count - 1);
        }
    }

    private static string Etiqueta(object nodo) => nodo switch
    {
        Declaracion d => $"Declaración {d.Tipo} {d.Nombre}",
        Asignacion a => $"Asignación {a.Nombre}",
        Mostrar => "Mostrar",
        Pedir p => $"Pedir {p.Nombre}" + (p.Pregunta is null ? "" : $" \"{p.Pregunta}\""),
        LiteralNumero n => $"Número {n.Lexema}",
        LiteralCadena c => c.EsPlantilla ? "Plantilla" : $"Texto \"{((SegmentoTexto)c.Segmentos[0]).Texto}\"",
        SegmentoTexto t => $"Texto \"{t.Texto}\"",
        SegmentoVariable v => $"Marcador {{{v.Nombre}}}",
        Variable v => $"Variable {v.Nombre}",
        Binaria b => b.Operador == "+" ? "Operación + (unir / sumar)" : $"Operación {b.Operador}",
        LlamadaFuncion f => $"Función {f.Funcion}",
        Agrupacion => "Paréntesis",
        _ => nodo.ToString() ?? ""
    };

    private static List<object> Hijos(object nodo) => nodo switch
    {
        Declaracion { Valor: not null } d => new() { d.Valor },
        Asignacion a => new() { a.Valor },
        Mostrar m => new() { m.Valor },
        LiteralCadena { EsPlantilla: true } c => c.Segmentos.Cast<object>().ToList(),
        Binaria b => new() { b.Izquierda, b.Derecha },
        LlamadaFuncion f => new() { f.Argumento },
        Agrupacion g => new() { g.Interna },
        _ => new()
    };
}
