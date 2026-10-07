using System.Text;

namespace MensajesPersonalizados.Compilador;

/// <summary>
/// Fase 4. Recorre el AST validado y escribe un programa de consola C# equivalente.
/// Las plantillas "Hola {nombre}" se traducen a cadenas interpoladas $"Hola {nombre}".
/// </summary>
public sealed class GeneradorCSharp
{
    private static readonly HashSet<string> ClavesCSharp = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    };

    private readonly IReadOnlyDictionary<Expresion, TipoDato> _tipos;
    private readonly Dictionary<string, TipoDato> _variables = new(StringComparer.Ordinal);
    private readonly StringBuilder _sb = new();
    private bool _usaLeerEntero;
    private bool _usaLeerDecimal;

    public GeneradorCSharp(IReadOnlyDictionary<Expresion, TipoDato> tipos)
    {
        _tipos = tipos;
    }

    public string Generar(ProgramaFuente programa)
    {
        var cuerpo = new StringBuilder();
        foreach (Instruccion instruccion in programa.Instrucciones)
        {
            cuerpo.Append("        ").AppendLine(Traducir(instruccion));
        }

        _sb.AppendLine("// Código generado por el Mini-compilador de Mensajes Personalizados (Tema 05, Grupo 5).");
        _sb.AppendLine("using System;");
        _sb.AppendLine("using System.Globalization;");
        _sb.AppendLine("using System.Text;");
        _sb.AppendLine();
        _sb.AppendLine("class Program");
        _sb.AppendLine("{");
        _sb.AppendLine("    static void Main()");
        _sb.AppendLine("    {");
        _sb.AppendLine("        Console.OutputEncoding = Encoding.UTF8;");
        _sb.AppendLine("        Console.InputEncoding = Encoding.UTF8;");
        _sb.AppendLine("        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;");
        _sb.AppendLine();
        _sb.Append(cuerpo);
        _sb.AppendLine("    }");

        if (_usaLeerEntero)
        {
            _sb.AppendLine();
            _sb.AppendLine("    static int LeerEntero(string pregunta)");
            _sb.AppendLine("    {");
            _sb.AppendLine("        while (true)");
            _sb.AppendLine("        {");
            _sb.AppendLine("            Console.Write(pregunta);");
            _sb.AppendLine("            string? dato = Console.ReadLine();");
            _sb.AppendLine("            if (dato is null) return 0;");
            _sb.AppendLine("            if (int.TryParse(dato.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor)) return valor;");
            _sb.AppendLine("            Console.WriteLine(\"Escriba un número entero.\");");
            _sb.AppendLine("        }");
            _sb.AppendLine("    }");
        }

        if (_usaLeerDecimal)
        {
            _sb.AppendLine();
            _sb.AppendLine("    static double LeerDecimal(string pregunta)");
            _sb.AppendLine("    {");
            _sb.AppendLine("        while (true)");
            _sb.AppendLine("        {");
            _sb.AppendLine("            Console.Write(pregunta);");
            _sb.AppendLine("            string? dato = Console.ReadLine();");
            _sb.AppendLine("            if (dato is null) return 0;");
            _sb.AppendLine("            if (double.TryParse(dato.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double valor)) return valor;");
            _sb.AppendLine("            Console.WriteLine(\"Escriba un número, por ejemplo 12.5\");");
            _sb.AppendLine("        }");
            _sb.AppendLine("    }");
        }

        _sb.AppendLine("}");
        return _sb.ToString();
    }

    private string Traducir(Instruccion instruccion) => instruccion switch
    {
        Declaracion d => TraducirDeclaracion(d),
        Asignacion a => $"{Nombre(a.Nombre)} = {Traducir(a.Valor)};",
        Mostrar m => $"Console.WriteLine({Traducir(m.Valor)});",
        Pedir p => TraducirPedir(p),
        _ => throw new InvalidOperationException("Instrucción desconocida.")
    };

    private string TraducirDeclaracion(Declaracion d)
    {
        _variables[d.Nombre] = d.Tipo;
        string valor = d.Valor is null
            ? d.Tipo switch { TipoDato.TEXTO => "\"\"", TipoDato.ENTERO => "0", _ => "0.0" }
            : Traducir(d.Valor);
        return $"{TipoCSharp(d.Tipo)} {Nombre(d.Nombre)} = {valor};";
    }

    private string TraducirPedir(Pedir p)
    {
        string pregunta = Literal((p.Pregunta ?? $"{p.Nombre}:").TrimEnd() + " ");
        switch (_variables[p.Nombre])
        {
            case TipoDato.ENTERO:
                _usaLeerEntero = true;
                return $"{Nombre(p.Nombre)} = LeerEntero({pregunta});";
            case TipoDato.DECIMAL:
                _usaLeerDecimal = true;
                return $"{Nombre(p.Nombre)} = LeerDecimal({pregunta});";
            default:
                return $"Console.Write({pregunta}); {Nombre(p.Nombre)} = Console.ReadLine() ?? \"\";";
        }
    }

    private string Traducir(Expresion e) => e switch
    {
        LiteralNumero n => n.Lexema,
        LiteralCadena c => TraducirCadena(c),
        Variable v => Nombre(v.Nombre),
        Agrupacion g => $"({Traducir(g.Interna)})",
        LlamadaFuncion f => f.Funcion switch
        {
            "MAYUSCULAS" => $"{Receptor(f.Argumento)}.ToUpperInvariant()",
            "MINUSCULAS" => $"{Receptor(f.Argumento)}.ToLowerInvariant()",
            _ => $"{Receptor(f.Argumento)}.Length"
        },
        Binaria b => $"{Traducir(b.Izquierda)} {b.Operador} {Traducir(b.Derecha)}",
        _ => throw new InvalidOperationException("Expresión desconocida.")
    };

    /// <summary>Envuelve en paréntesis lo que no sea una variable o literal, para llamar métodos sobre el resultado.</summary>
    private string Receptor(Expresion e) =>
        e is Variable or LiteralCadena or Agrupacion or LlamadaFuncion ? Traducir(e) : $"({Traducir(e)})";

    private string TraducirCadena(LiteralCadena c)
    {
        if (!c.EsPlantilla)
        {
            return Literal(string.Concat(c.Segmentos.OfType<SegmentoTexto>().Select(s => s.Texto)));
        }

        var sb = new StringBuilder("$\"");
        foreach (SegmentoCadena segmento in c.Segmentos)
        {
            if (segmento is SegmentoTexto t)
            {
                sb.Append(Escapar(t.Texto).Replace("{", "{{").Replace("}", "}}"));
            }
            else if (segmento is SegmentoVariable v)
            {
                sb.Append('{').Append(Nombre(v.Nombre)).Append('}');
            }
        }
        return sb.Append('"').ToString();
    }

    private static string Literal(string texto) => $"\"{Escapar(texto)}\"";

    private static string Escapar(string texto)
    {
        var sb = new StringBuilder();
        foreach (char c in texto)
        {
            sb.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\t' => "\\t",
                '\r' => "\\r",
                _ => c.ToString()
            });
        }
        return sb.ToString();
    }

    private static string Nombre(string nombre) => ClavesCSharp.Contains(nombre) ? "@" + nombre : nombre;

    private static string TipoCSharp(TipoDato tipo) => tipo switch
    {
        TipoDato.TEXTO => "string",
        TipoDato.ENTERO => "int",
        _ => "double"
    };
}
