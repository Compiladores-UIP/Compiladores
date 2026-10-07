using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("Mini-compilador de estructuras IF");
Console.WriteLine();
Console.WriteLine("Escriba el código fuente línea por línea.");
Console.WriteLine("Las líneas vacías se permiten.");
Console.WriteLine("Cuando termine, escriba EJECUTAR en una línea independiente.");
Console.WriteLine();
Console.WriteLine("Ejemplo:");
Console.WriteLine("ENTERO edad = 20");
Console.WriteLine("SI edad >= 18 ENTONCES");
Console.WriteLine("IMPRIMIR \"Mayor de edad\"");
Console.WriteLine("SINO");
Console.WriteLine("IMPRIMIR \"Menor de edad\"");
Console.WriteLine("FIN_SI");
Console.WriteLine("EJECUTAR");
Console.WriteLine();
Console.WriteLine("Código >");

var lineas = new List<string>();
while (true)
{
    string? linea = Console.ReadLine();

    if (linea is null || linea.Trim().Equals("EJECUTAR", StringComparison.Ordinal))
    {
        break;
    }

    lineas.Add(linea);
}

string fuente = string.Join(Environment.NewLine, lineas);

try
{
    List<string> lineasUtiles = lineas
        .Where(linea => !string.IsNullOrWhiteSpace(linea))
        .Select(linea => linea.Trim())
        .ToList();

    if (lineasUtiles.Count == 0)
    {
        throw new Exception("Error sintáctico: no se recibió código fuente.");
    }

    int posicion = 0;

    Match declaracion = Regex.Match(
        lineasUtiles[posicion++],
        "^(ENTERO|DECIMAL|TEXTO)\\s+([A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(.+)$");

    if (!declaracion.Success)
    {
        throw new Exception("Error sintáctico: la declaración inicial debe tener el formato TIPO nombre = valor.");
    }

    string tipoFuente = declaracion.Groups[1].Value;
    string nombreVariable = declaracion.Groups[2].Value;
    string valorInicial = declaracion.Groups[3].Value.Trim();
    ValidarValor(tipoFuente, valorInicial, "declaración");

    if (posicion >= lineasUtiles.Count)
    {
        throw new Exception("Error sintáctico: se esperaba una estructura SI después de la declaración.");
    }

    string lineaCondicion = lineasUtiles[posicion++];
    if (lineaCondicion.StartsWith("SI ", StringComparison.Ordinal) &&
        !Regex.IsMatch(lineaCondicion, "\\sENTONCES$") )
    {
        throw new Exception("Error sintáctico: se esperaba ENTONCES después de la condición.");
    }

    Match condicion = Regex.Match(
        lineaCondicion,
        "^SI\\s+([A-Za-z_][A-Za-z0-9_]*)\\s*(>=|<=|==|!=|>|<)\\s*(.+?)\\s+ENTONCES$");

    if (!condicion.Success)
    {
        throw new Exception("Error sintáctico: la condición SI no tiene un formato válido.");
    }

    string variableCondicion = condicion.Groups[1].Value;
    string operador = condicion.Groups[2].Value;
    string valorComparacion = condicion.Groups[3].Value.Trim();

    if (variableCondicion != nombreVariable)
    {
        throw new Exception($"Error semántico: la variable '{variableCondicion}' no coincide con la variable declarada '{nombreVariable}'.");
    }

    ValidarValor(tipoFuente, valorComparacion, "condición");

    if (tipoFuente == "TEXTO" && operador is not ("==" or "!="))
    {
        throw new Exception("Error semántico: las variables TEXTO solo admiten == y != en este mini-compilador.");
    }

    if (posicion >= lineasUtiles.Count)
    {
        throw new Exception("Error sintáctico: se esperaba IMPRIMIR después de ENTONCES.");
    }

    string mensajeVerdadero = LeerImpresion(lineasUtiles[posicion++], "después de ENTONCES");
    string? mensajeFalso = null;

    if (posicion < lineasUtiles.Count && lineasUtiles[posicion] == "SINO")
    {
        posicion++;

        if (posicion >= lineasUtiles.Count)
        {
            throw new Exception("Error sintáctico: se esperaba IMPRIMIR después de SINO.");
        }

        mensajeFalso = LeerImpresion(lineasUtiles[posicion++], "después de SINO");
    }

    if (posicion >= lineasUtiles.Count || lineasUtiles[posicion] != "FIN_SI")
    {
        throw new Exception("Error sintáctico: falta FIN_SI.");
    }

    posicion++;

    if (posicion != lineasUtiles.Count)
    {
        throw new Exception("Error sintáctico: hay instrucciones adicionales después de FIN_SI.");
    }

    List<(string Texto, string Tipo)> tokens = Tokenizar(fuente);
    string codigo = GenerarCodigo(
        tipoFuente,
        nombreVariable,
        valorInicial,
        operador,
        valorComparacion,
        mensajeVerdadero,
        mensajeFalso);

    bool condicionVerdadera = EvaluarCondicion(
        tipoFuente,
        valorInicial,
        operador,
        valorComparacion);

    Console.WriteLine();
    Console.WriteLine("Código fuente:");
    Console.WriteLine(fuente);

    Console.WriteLine();
    Console.WriteLine("Tokens:");
    foreach (var token in tokens)
    {
        Console.WriteLine($"{token.Texto,-24}{token.Tipo}");
    }

    Console.WriteLine();
    Console.WriteLine("Análisis sintáctico: correcto.");

    Console.WriteLine();
    Console.WriteLine("Código C# generado:");
    Console.WriteLine(codigo);

    Console.WriteLine();
    Console.WriteLine("Resultado:");
    if (condicionVerdadera)
    {
        Console.WriteLine(mensajeVerdadero);
    }
    else if (mensajeFalso is not null)
    {
        Console.WriteLine(mensajeFalso);
    }
    else
    {
        Console.WriteLine("(sin salida)");
    }
}
catch (Exception error)
{
    Console.WriteLine();
    Console.WriteLine(error.Message);
    Environment.ExitCode = 1;
}

static string LeerImpresion(string linea, string contexto)
{
    Match impresion = Regex.Match(linea, "^IMPRIMIR\\s+\"([^\"]*)\"$");

    if (!impresion.Success)
    {
        throw new Exception($"Error sintáctico: se esperaba IMPRIMIR \"texto\" {contexto}.");
    }

    return impresion.Groups[1].Value;
}

static void ValidarValor(string tipo, string valor, string contexto)
{
    bool valido = tipo switch
    {
        "ENTERO" => Regex.IsMatch(valor, "^-?\\d+$"),
        "DECIMAL" => Regex.IsMatch(valor, "^-?\\d+(?:\\.\\d+)?$"),
        "TEXTO" => Regex.IsMatch(valor, "^\"[^\"]*\"$"),
        _ => false
    };

    if (!valido)
    {
        throw new Exception($"Error semántico: el valor usado en la {contexto} no corresponde al tipo {tipo}.");
    }
}

static List<(string Texto, string Tipo)> Tokenizar(string fuente)
{
    var resultado = new List<(string Texto, string Tipo)>();
    string patron = "\"[^\"]*\"|>=|<=|==|!=|=|>|<|-?\\d+(?:\\.\\d+)?|[A-Za-z_][A-Za-z0-9_]*";

    foreach (Match coincidencia in Regex.Matches(fuente, patron))
    {
        string texto = coincidencia.Value;
        string tipo = texto switch
        {
            "ENTERO" or "DECIMAL" or "TEXTO" or "SI" or "ENTONCES" or "IMPRIMIR" or "SINO" or "FIN_SI" => "PALABRA_RESERVADA",
            ">=" or "<=" or "==" or "!=" or ">" or "<" => "OPERADOR_RELACIONAL",
            "=" => "ASIGNACION",
            _ when Regex.IsMatch(texto, "^\"[^\"]*\"$") => "CADENA",
            _ when Regex.IsMatch(texto, "^-?\\d+(?:\\.\\d+)?$") => "NUMERO",
            _ => "IDENTIFICADOR"
        };

        resultado.Add((texto, tipo));
    }

    return resultado;
}

static string GenerarCodigo(
    string tipoFuente,
    string nombre,
    string valor,
    string operador,
    string comparacion,
    string verdadero,
    string? falso)
{
    string tipoCSharp = tipoFuente switch
    {
        "ENTERO" => "int",
        "DECIMAL" => "decimal",
        "TEXTO" => "string",
        _ => throw new InvalidOperationException("Tipo no soportado.")
    };

    string valorCSharp = ConvertirLiteralCSharp(tipoFuente, valor);
    string comparacionCSharp = ConvertirLiteralCSharp(tipoFuente, comparacion);
    string mensajeVerdadero = EscaparCadena(verdadero);

    var codigo = new StringBuilder();
    codigo.AppendLine("using System;");
    codigo.AppendLine();
    codigo.AppendLine("class Program");
    codigo.AppendLine("{");
    codigo.AppendLine("    static void Main()");
    codigo.AppendLine("    {");
    codigo.AppendLine($"        {tipoCSharp} {nombre} = {valorCSharp};");
    codigo.AppendLine();
    codigo.AppendLine($"        if ({nombre} {operador} {comparacionCSharp})");
    codigo.AppendLine("        {");
    codigo.AppendLine($"            Console.WriteLine(\"{mensajeVerdadero}\");");
    codigo.AppendLine("        }");

    if (falso is not null)
    {
        codigo.AppendLine("        else");
        codigo.AppendLine("        {");
        codigo.AppendLine($"            Console.WriteLine(\"{EscaparCadena(falso)}\");");
        codigo.AppendLine("        }");
    }

    codigo.AppendLine("    }");
    codigo.AppendLine("}");

    return codigo.ToString();
}

static string ConvertirLiteralCSharp(string tipo, string valor)
{
    return tipo switch
    {
        "DECIMAL" => valor + "m",
        _ => valor
    };
}

static string EscaparCadena(string texto)
{
    return texto.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

static bool EvaluarCondicion(string tipo, string izquierdaTexto, string operador, string derechaTexto)
{
    return tipo switch
    {
        "ENTERO" => CompararNumeros(
            long.Parse(izquierdaTexto, CultureInfo.InvariantCulture),
            operador,
            long.Parse(derechaTexto, CultureInfo.InvariantCulture)),
        "DECIMAL" => CompararNumeros(
            decimal.Parse(izquierdaTexto, CultureInfo.InvariantCulture),
            operador,
            decimal.Parse(derechaTexto, CultureInfo.InvariantCulture)),
        "TEXTO" => CompararTexto(QuitarComillas(izquierdaTexto), operador, QuitarComillas(derechaTexto)),
        _ => false
    };
}

static bool CompararNumeros<T>(T izquierda, string operador, T derecha) where T : IComparable<T>
{
    int comparacion = izquierda.CompareTo(derecha);

    return operador switch
    {
        ">" => comparacion > 0,
        "<" => comparacion < 0,
        ">=" => comparacion >= 0,
        "<=" => comparacion <= 0,
        "==" => comparacion == 0,
        "!=" => comparacion != 0,
        _ => false
    };
}

static bool CompararTexto(string izquierda, string operador, string derecha)
{
    return operador switch
    {
        "==" => izquierda == derecha,
        "!=" => izquierda != derecha,
        _ => throw new Exception("Error semántico: operador no válido para TEXTO.")
    };
}

static string QuitarComillas(string texto)
{
    return texto.Length >= 2 && texto[0] == '"' && texto[^1] == '"'
        ? texto[1..^1]
        : texto;
}
