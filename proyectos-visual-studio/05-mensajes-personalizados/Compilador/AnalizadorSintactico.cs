using System.Text;

namespace MensajesPersonalizados.Compilador;

/// <summary>
/// Fase 2. Parser descendente recursivo. Comprueba el orden de los tokens y construye el árbol (AST).
///
/// programa     → instruccion* FIN_ARCHIVO
/// instruccion  → (declaracion | asignacion | mostrar | pedir) FIN_LINEA
/// declaracion  → TIPO IDENTIFICADOR ( "=" expresion )?
/// asignacion   → IDENTIFICADOR "=" expresion
/// mostrar      → ( MOSTRAR | IMPRIMIR ) expresion
/// pedir        → ( PEDIR | LEER ) IDENTIFICADOR CADENA?
/// expresion    → termino ( ("+" | "-") termino )*
/// termino      → factor ( ("*" | "/") factor )*
/// factor       → CADENA | NUMERO | IDENTIFICADOR | FUNCION "(" expresion ")" | "(" expresion ")"
/// </summary>
public sealed class AnalizadorSintactico
{
    private readonly List<Token> _tokens;
    private int _pos;

    public AnalizadorSintactico(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public ProgramaFuente Analizar()
    {
        var instrucciones = new List<Instruccion>();

        while (Actual.Tipo != TipoToken.FIN_ARCHIVO)
        {
            instrucciones.Add(Instruccion());

            if (Actual.Tipo == TipoToken.FIN_LINEA)
            {
                _pos++;
            }
            else if (Actual.Tipo != TipoToken.FIN_ARCHIVO)
            {
                throw Error($"sobra '{Actual.Lexema}' al final de la instrucción. Escriba una instrucción por línea.");
            }
        }

        if (instrucciones.Count == 0)
        {
            throw new ErrorCompilacion(FaseError.Sintactico, "no se recibió ninguna instrucción.", 1, 1);
        }

        return new ProgramaFuente(instrucciones);
    }

    private Instruccion Instruccion()
    {
        Token t = Actual;

        if (t.Tipo == TipoToken.PALABRA_RESERVADA)
        {
            switch (t.Lexema)
            {
                case "TEXTO":
                case "ENTERO":
                case "DECIMAL":
                    return Declaracion();
                case "MOSTRAR":
                case "IMPRIMIR":
                    _pos++;
                    if (Actual.Tipo is TipoToken.FIN_LINEA or TipoToken.FIN_ARCHIVO)
                    {
                        throw Error($"se esperaba un mensaje después de {t.Lexema}.");
                    }
                    return new Mostrar(Expresion(), t.Linea, t.Columna);
                case "PEDIR":
                case "LEER":
                    return Pedir();
            }
        }

        if (t.Tipo == TipoToken.IDENTIFICADOR)
        {
            if (Mirar(1).Tipo == TipoToken.ASIGNACION)
            {
                _pos += 2;
                return new Asignacion(t.Lexema, Expresion(), t.Linea, t.Columna);
            }

            string sugerencia = AnalizadorLexico.PalabrasReservadas.Contains(t.Lexema.ToUpperInvariant())
                ? $" Las palabras reservadas van en mayúsculas: {t.Lexema.ToUpperInvariant()}."
                : " Se esperaba TEXTO, ENTERO, DECIMAL, MOSTRAR, IMPRIMIR, PEDIR o una asignación.";
            throw Error($"'{t.Lexema}' no inicia una instrucción válida.{sugerencia}");
        }

        throw Error($"'{t.Lexema}' no puede iniciar una instrucción.");
    }

    private Declaracion Declaracion()
    {
        Token tipo = Consumir(TipoToken.PALABRA_RESERVADA, "se esperaba un tipo");
        Token nombre = Consumir(TipoToken.IDENTIFICADOR, $"se esperaba el nombre de la variable después de {tipo.Lexema}");

        Expresion? valor = null;
        if (Actual.Tipo == TipoToken.ASIGNACION)
        {
            _pos++;
            if (Actual.Tipo is TipoToken.FIN_LINEA or TipoToken.FIN_ARCHIVO)
            {
                throw Error($"falta el valor de '{nombre.Lexema}' después de '='.");
            }
            valor = Expresion();
        }

        return new Declaracion(Enum.Parse<TipoDato>(tipo.Lexema), nombre.Lexema, valor, tipo.Linea, tipo.Columna);
    }

    private Pedir Pedir()
    {
        Token palabra = Consumir(TipoToken.PALABRA_RESERVADA, "se esperaba PEDIR");
        Token nombre = Consumir(TipoToken.IDENTIFICADOR, $"se esperaba la variable que recibirá el dato después de {palabra.Lexema}");

        string? pregunta = null;
        if (Actual.Tipo == TipoToken.CADENA)
        {
            pregunta = Actual.Valor;
            _pos++;
        }

        return new Pedir(nombre.Lexema, pregunta, palabra.Linea, palabra.Columna);
    }

    private Expresion Expresion()
    {
        Expresion izquierda = Termino();
        while (Actual.Tipo == TipoToken.OPERADOR && Actual.Lexema is "+" or "-")
        {
            Token op = Actual;
            _pos++;
            izquierda = new Binaria(izquierda, op.Lexema, Termino(), op.Linea, op.Columna);
        }
        return izquierda;
    }

    private Expresion Termino()
    {
        Expresion izquierda = Factor();
        while (Actual.Tipo == TipoToken.OPERADOR && Actual.Lexema is "*" or "/")
        {
            Token op = Actual;
            _pos++;
            izquierda = new Binaria(izquierda, op.Lexema, Factor(), op.Linea, op.Columna);
        }
        return izquierda;
    }

    private Expresion Factor()
    {
        Token t = Actual;
        switch (t.Tipo)
        {
            case TipoToken.CADENA:
                _pos++;
                return new LiteralCadena(Segmentar(t), t.Linea, t.Columna);
            case TipoToken.NUMERO_ENTERO:
            case TipoToken.NUMERO_DECIMAL:
                _pos++;
                return new LiteralNumero(t.Lexema, t.Tipo == TipoToken.NUMERO_DECIMAL, t.Linea, t.Columna);
            case TipoToken.IDENTIFICADOR:
                _pos++;
                return new Variable(t.Lexema, t.Linea, t.Columna);
            case TipoToken.FUNCION:
                _pos++;
                Consumir(TipoToken.PARENTESIS_IZQ, $"se esperaba '(' después de {t.Lexema}");
                Expresion argumento = Expresion();
                Consumir(TipoToken.PARENTESIS_DER, $"falta ')' para cerrar {t.Lexema}");
                return new LlamadaFuncion(t.Lexema, argumento, t.Linea, t.Columna);
            case TipoToken.PARENTESIS_IZQ:
                _pos++;
                Expresion interna = Expresion();
                Consumir(TipoToken.PARENTESIS_DER, "falta ')' para cerrar el paréntesis");
                return new Agrupacion(interna, t.Linea, t.Columna);
            case TipoToken.FIN_LINEA:
            case TipoToken.FIN_ARCHIVO:
                throw Error("la expresión está incompleta: se esperaba un texto, un número o una variable.");
            default:
                throw Error($"no se esperaba '{t.Lexema}' en esta posición; se esperaba un texto, un número o una variable.");
        }
    }

    /// <summary>
    /// Divide el contenido de una cadena en texto fijo y marcadores {variable}.
    /// "{{" y "}}" representan llaves literales.
    /// </summary>
    private static List<SegmentoCadena> Segmentar(Token cadena)
    {
        var segmentos = new List<SegmentoCadena>();
        var texto = new StringBuilder();
        string v = cadena.Valor;

        for (int i = 0; i < v.Length; i++)
        {
            char c = v[i];
            if (c == '{' && i + 1 < v.Length && v[i + 1] == '{') { texto.Append('{'); i++; continue; }
            if (c == '}' && i + 1 < v.Length && v[i + 1] == '}') { texto.Append('}'); i++; continue; }

            if (c == '}')
            {
                throw new ErrorCompilacion(FaseError.Sintactico,
                    "hay una '}' sin su '{'. Para mostrar una llave literal escriba '}}'.", cadena.Linea, cadena.Columna);
            }

            if (c == '{')
            {
                int cierre = v.IndexOf('}', i + 1);
                if (cierre < 0)
                {
                    throw new ErrorCompilacion(FaseError.Sintactico,
                        "el marcador de la plantilla no se cerró con '}'. Para mostrar una llave literal escriba '{{'.",
                        cadena.Linea, cadena.Columna);
                }

                string nombre = v[(i + 1)..cierre].Trim();
                if (!EsIdentificador(nombre))
                {
                    throw new ErrorCompilacion(FaseError.Sintactico,
                        $"'{{{nombre}}}' no es un marcador válido: dentro de las llaves solo va el nombre de una variable.",
                        cadena.Linea, cadena.Columna);
                }

                if (texto.Length > 0) { segmentos.Add(new SegmentoTexto(texto.ToString())); texto.Clear(); }
                segmentos.Add(new SegmentoVariable(nombre, cadena.Linea, cadena.Columna));
                i = cierre;
                continue;
            }

            texto.Append(c);
        }

        if (texto.Length > 0 || segmentos.Count == 0)
        {
            segmentos.Add(new SegmentoTexto(texto.ToString()));
        }
        return segmentos;
    }

    private static bool EsIdentificador(string s) =>
        s.Length > 0
        && (char.IsAsciiLetter(s[0]) || s[0] == '_')
        && s.All(ch => char.IsAsciiLetterOrDigit(ch) || ch == '_')
        && !AnalizadorLexico.PalabrasReservadas.Contains(s)
        && !AnalizadorLexico.Funciones.Contains(s);

    private Token Actual => _tokens[_pos];
    private Token Mirar(int desplazamiento) => _tokens[Math.Min(_pos + desplazamiento, _tokens.Count - 1)];

    private Token Consumir(TipoToken esperado, string mensaje)
    {
        if (Actual.Tipo != esperado)
        {
            string encontrado = Actual.Tipo is TipoToken.FIN_LINEA or TipoToken.FIN_ARCHIVO ? "el fin de la línea" : $"'{Actual.Lexema}'";
            throw Error($"{mensaje}, pero se encontró {encontrado}.");
        }
        return _tokens[_pos++];
    }

    private ErrorCompilacion Error(string mensaje) =>
        new(FaseError.Sintactico, mensaje, Actual.Linea, Actual.Columna);
}
