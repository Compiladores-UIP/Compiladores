using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Nexo;

public record Expr(string Kind, string Text, object? Value = null, Expr? Left = null, Expr? Right = null);
public record Stmt(string Kind, string? Name = null, string? Type = null, Expr? Expression = null,
    List<Stmt>? Body = null, List<Stmt>? Otherwise = null, Stmt? Initial = null, Stmt? Update = null);
public record Compilation(string Target, string Filename, string Generated, string Output,
    List<string> Tokens, object Tree, string? Project = null);

/// <summary>Compilador educativo con análisis léxico, análisis sintáctico, interpretación y generación de código.</summary>
public static class CompilerEngine
{
    private const string ConsoleWindowSource = """
internal static class ConsoleWindow
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint GetConsoleProcessList([System.Runtime.InteropServices.Out] uint[] processes, uint count);

    public static void WaitIfOpenedSeparately()
    {
        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected || Console.IsOutputRedirected) return;
        // Una consola con un solo proceso se cierra cuando termina este programa.
        if (GetConsoleProcessList(new uint[2], 2) != 1) return;
        Console.WriteLine("\nPulsa una tecla para cerrar...");
        Console.ReadKey(true);
    }
}
""";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Regex TokenPattern = new("\\G(?:\"(?:\\\\.|[^\"\\\\])*\"|\\d+(?:\\.\\d+)?|[A-Za-z_]\\w*|>=|<=|==|!=|&&|\\|\\||[{}();=+*/%<>!-])");

    public static List<string> Lex(string source)
    {
        var tokens = new List<string>();
        for (int position = 0; position < source.Length;)
        {
            if (char.IsWhiteSpace(source[position])) { position++; continue; }
            if (source.AsSpan(position).StartsWith("//"))
            { int end = source.IndexOf('\n', position); position = end < 0 ? source.Length : end; continue; }
            var match = TokenPattern.Match(source, position);
            if (!match.Success) throw new InvalidOperationException($"Carácter no admitido en posición {position + 1}: {source[position]}");
            tokens.Add(match.Value); position += match.Length;
            if (tokens.Count > 4000) throw new InvalidOperationException("Límite de 4000 tokens alcanzado.");
        }
        return tokens;
    }

    public static Compilation Compile(string source, int module)
    {
        if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException("El programa está vacío.");
        if (source.Length > 30000) throw new InvalidOperationException("Límite de 30 000 caracteres alcanzado.");
        if (module == 9) return Forms(source);
        if (module == 10) return Query(source);
        if (module == 11) return Configuration(source);
        var normalized = module == 2 && !Regex.IsMatch(source, @"^\s*(IMPRIMIR|MOSTRAR|ENTERO|DECIMAL)", RegexOptions.Multiline)
            ? $"PRINT({string.Join(' ', Lex(source))});" : Normalize(source);
        var tree = new Parser(Lex(normalized)).Parse();
        string output = new Interpreter().Run(tree);
        string generated = "using System;\n\nclass Program\n{\n    static void Main()\n    {\n        Console.OutputEncoding = new System.Text.UTF8Encoding(false);\n" + Generate(tree, 2) + "\n        ConsoleWindow.WaitIfOpenedSeparately();\n    }\n}\n" + ConsoleWindowSource;
        return new Compilation("C#", "Program.cs", generated, output, Lex(source), tree, Project(false));
    }

    private static string Normalize(string source)
    {
        var blocks = new Stack<string>(); var lines = new List<string>();
        string Expression(string text) => string.Join(' ', Lex(text).Select(t => t switch
        { "verdadero" => "true", "falso" => "false", "Y" => "&&", "O" => "||", "NO" => "!", _ => t }));
        void End(string kind)
        { if (blocks.Count == 0 || blocks.Pop() != kind) throw new InvalidOperationException($"Cierre incompatible: {kind}."); }
        foreach (string raw in source.Split('\n'))
        {
            string line = raw.Trim(); if (line.Length == 0 || line.StartsWith("//")) continue;
            Match m;
            if ((m = Regex.Match(line, @"^(IMPRIMIR|MOSTRAR)\s+(.+)$")).Success)
                lines.Add($"PRINT({Expression(m.Groups[2].Value.TrimEnd(';'))});");
            else if ((m = Regex.Match(line, @"^(ENTERO|DECIMAL|TEXTO|BOOLEANO)\s+([A-Za-z_]\w*)\s*=\s*(.+)$")).Success)
            {
                string type = m.Groups[1].Value switch { "ENTERO" => "int", "DECIMAL" => "double", "TEXTO" => "string", _ => "bool" };
                lines.Add($"{type} {m.Groups[2].Value} = {Expression(m.Groups[3].Value.TrimEnd(';'))};");
            }
            else if ((m = Regex.Match(line, @"^SI\s+(.+)\s+ENTONCES$")).Success)
            { blocks.Push("SI"); lines.Add($"if ({Expression(m.Groups[1].Value)}) {{"); }
            else if (line == "SINO")
            { End("SI"); blocks.Push("SINO"); lines.Add("} else {"); }
            else if (line == "FINSI")
            { if (blocks.Count == 0 || blocks.Peek() is not ("SI" or "SINO")) throw new InvalidOperationException("FINSI sin SI."); blocks.Pop(); lines.Add("}"); }
            else if ((m = Regex.Match(line, @"^MIENTRAS\s+(.+)\s+HACER$")).Success)
            { blocks.Push("MIENTRAS"); lines.Add($"while ({Expression(m.Groups[1].Value)}) {{"); }
            else if (line == "FINMIENTRAS") { End("MIENTRAS"); lines.Add("}"); }
            else if ((m = Regex.Match(line, @"^PARA\s+([A-Za-z_]\w*)\s*=\s*(.+)\s+HASTA\s+(.+)\s+HACER$")).Success)
            {
                blocks.Push("PARA"); string name = m.Groups[1].Value;
                lines.Add($"for (int {name} = {Expression(m.Groups[2].Value)}; {name} <= {Expression(m.Groups[3].Value)}; {name} = {name} + 1) {{");
            }
            else if (line == "FINPARA") { End("PARA"); lines.Add("}"); }
            else if (line == "REPETIR") { blocks.Push("REPETIR"); lines.Add("do {"); }
            else if ((m = Regex.Match(line, @"^HASTA\s+(.+)$")).Success)
            { End("REPETIR"); lines.Add($"}} while (!({Expression(m.Groups[1].Value)}));"); }
            else if (Regex.IsMatch(line, @"^[A-Za-z_]\w*\s*=")) lines.Add(Expression(line.TrimEnd(';')) + ";");
            else throw new InvalidOperationException($"Instrucción no reconocida: {line}");
        }
        if (blocks.Count > 0) throw new InvalidOperationException($"Falta cerrar {blocks.Peek()}.");
        return string.Join('\n', lines);
    }

    private sealed class Parser(List<string> tokens)
    {
        private int index, depth;
        private string Peek => index < tokens.Count ? tokens[index] : "";
        private string Next() => index < tokens.Count ? tokens[index++] : throw new InvalidOperationException("Fin inesperado del programa.");
        private void Take(string token)
        { if (Next() != token) throw new InvalidOperationException($"Se esperaba {token} cerca del token {index}."); }
        private string Name()
        {
            string name = Next();
            if (!Regex.IsMatch(name, @"^[A-Za-z_]\w*$") || new[] { "if", "else", "while", "for", "do", "int", "double", "string", "bool", "true", "false", "PRINT", "class", "return", "new", "using", "namespace", "static", "void", "public" }.Contains(name))
                throw new InvalidOperationException($"Nombre de variable inválido: {name}");
            return name;
        }
        public List<Stmt> Parse()
        { var statements = new List<Stmt>(); while (Peek.Length > 0) statements.Add(Statement()); return statements; }
        private Expr Atom()
        {
            if (++depth > 80) throw new InvalidOperationException("Demasiados niveles de anidación.");
            try
            {
                string token = Next();
                if (token == "(") { var expr = Expression(); Take(")"); return expr; }
                if (token is "!" or "-" or "+") return new Expr("Unary", token, Left: Atom());
                if (token.StartsWith('"')) return new Expr("Literal", token, JsonSerializer.Deserialize<string>(token));
                if (token is "true" or "false") return new Expr("Literal", token, token == "true");
                if (char.IsDigit(token[0])) return new Expr("Literal", token,
                    token.Contains('.') ? (object)double.Parse(token, CultureInfo.InvariantCulture) : long.Parse(token, CultureInfo.InvariantCulture));
                if (Regex.IsMatch(token, @"^[A-Za-z_]\w*$")) return new Expr("Variable", token);
                throw new InvalidOperationException($"Expresión inválida: {token}");
            }
            finally { depth--; }
        }
        private Expr Binary(Func<Expr> next, params string[] operators)
        { Expr expr = next(); while (operators.Contains(Peek)) expr = new Expr("Binary", Next(), Left: expr, Right: next()); return expr; }
        private Expr Product() => Binary(Atom, "*", "/", "%");
        private Expr Sum() => Binary(Product, "+", "-");
        private Expr Comparison() => Binary(Sum, ">", "<", ">=", "<=");
        private Expr Equality() => Binary(Comparison, "==", "!=");
        private Expr And() => Binary(Equality, "&&");
        private Expr Expression() => Binary(And, "||");
        private Stmt Declaration()
        { string type = Next(), name = Name(); Take("="); return new Stmt("Declaration", name, type, Expression()); }
        private Stmt Assignment()
        { string name = Name(); Take("="); return new Stmt("Assignment", name, Expression: Expression()); }
        private List<Stmt> Block()
        { Take("{"); var body = new List<Stmt>(); while (Peek is not ("}" or "")) body.Add(Statement()); Take("}"); return body; }
        private Stmt Statement()
        {
            if (++depth > 80) throw new InvalidOperationException("Demasiados bloques anidados.");
            try
            {
                if (Peek is "int" or "double" or "string" or "bool") { var declaration = Declaration(); Take(";"); return declaration; }
                if (Peek == "PRINT") { Next(); Take("("); var value = Expression(); Take(")"); Take(";"); return new Stmt("Print", Expression: value); }
                if (Peek == "if")
                { Next(); Take("("); var condition = Expression(); Take(")"); var body = Block(); var otherwise = new List<Stmt>(); if (Peek == "else") { Next(); otherwise = Block(); } return new Stmt("If", Expression: condition, Body: body, Otherwise: otherwise); }
                if (Peek == "while")
                { Next(); Take("("); var condition = Expression(); Take(")"); return new Stmt("While", Expression: condition, Body: Block()); }
                if (Peek == "do")
                { Next(); var body = Block(); Take("while"); Take("("); var condition = Expression(); Take(")"); Take(";"); return new Stmt("DoWhile", Expression: condition, Body: body); }
                if (Peek == "for")
                { Next(); Take("("); var initial = Declaration(); Take(";"); var condition = Expression(); Take(";"); var update = Assignment(); Take(")"); return new Stmt("For", Expression: condition, Body: Block(), Initial: initial, Update: update); }
                var assignment = Assignment(); Take(";"); return assignment;
            }
            finally { depth--; }
        }
    }

    private sealed class Interpreter
    {
        private record Variable(string Type, object Value);
        private readonly List<Dictionary<string, Variable>> scopes = [new()];
        private readonly StringBuilder output = new();
        private int steps;
        private void Tick() { if (++steps > 10000) throw new InvalidOperationException("Límite de 10 000 pasos; revisa si el ciclo termina."); }
        private Dictionary<string, Variable> Scope(string name) => scopes.AsEnumerable().Reverse().FirstOrDefault(s => s.ContainsKey(name))
            ?? throw new InvalidOperationException($"Variable no declarada: {name}");
        private static double Number(object value) => value is long or double ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : throw new InvalidOperationException("La operación requiere números.");
        private static bool Boolean(object value) => value is bool flag ? flag : throw new InvalidOperationException("La condición requiere un booleano.");
        private static string Text(object value) => value is bool b ? (b ? "True" : "False") : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        private static object Validate(string type, object value)
        {
            if (type == "string") return value is string ? value : throw new InvalidOperationException("TEXTO requiere una cadena.");
            if (type == "bool") return Boolean(value);
            double number = Number(value);
            if (!double.IsFinite(number)) throw new InvalidOperationException("Número fuera de rango.");
            if (type == "int")
            { if (number != Math.Truncate(number) || number < int.MinValue || number > int.MaxValue) throw new InvalidOperationException("ENTERO requiere un entero de 32 bits."); return (long)number; }
            return number;
        }
        private object Evaluate(Expr expr)
        {
            Tick();
            if (expr.Kind == "Literal") return expr.Value!;
            if (expr.Kind == "Variable") return Scope(expr.Text)[expr.Text].Value;
            object a = Evaluate(expr.Left!);
            if (expr.Kind == "Unary") return expr.Text switch { "!" => !Boolean(a), "-" => a is long integer ? (object)checked(-integer) : -Number(a), _ => a };
            if (expr.Text == "&&") return Boolean(a) && Boolean(Evaluate(expr.Right!));
            if (expr.Text == "||") return Boolean(a) || Boolean(Evaluate(expr.Right!));
            object b = Evaluate(expr.Right!);
            if (expr.Text == "+" && (a is string || b is string)) return Text(a) + Text(b);
            if (expr.Text is "==" or "!=")
            { bool equal = a is long or double && b is long or double ? Number(a) == Number(b) : Equals(a, b); return expr.Text == "==" ? equal : !equal; }
            double x = Number(a), y = Number(b);
            if (expr.Text is "/" or "%" && y == 0) throw new InvalidOperationException("No se puede dividir entre cero.");
            if (expr.Text is ">" or "<" or ">=" or "<=") return expr.Text switch { ">" => x > y, "<" => x < y, ">=" => x >= y, _ => x <= y };
            if (a is long left && b is long right) return expr.Text switch
            { "+" => checked(left + right), "-" => checked(left - right), "*" => checked(left * right), "/" => left / right, "%" => left % right, _ => throw new InvalidOperationException("Operador inválido.") };
            return expr.Text switch { "+" => x + y, "-" => x - y, "*" => x * y, "/" => x / y, "%" => x % y, _ => throw new InvalidOperationException("Operador inválido.") };
        }
        private void Block(List<Stmt> body)
        { scopes.Add(new()); try { foreach (var statement in body) Execute(statement); } finally { scopes.RemoveAt(scopes.Count - 1); } }
        private void Execute(Stmt statement)
        {
            Tick();
            switch (statement.Kind)
            {
                case "Declaration":
                    if (scopes[^1].ContainsKey(statement.Name!)) throw new InvalidOperationException($"Variable duplicada: {statement.Name}");
                    scopes[^1].Add(statement.Name!, new Variable(statement.Type!, Validate(statement.Type!, Evaluate(statement.Expression!)))); break;
                case "Assignment":
                    var scope = Scope(statement.Name!); var old = scope[statement.Name!];
                    scope[statement.Name!] = new Variable(old.Type, Validate(old.Type, Evaluate(statement.Expression!))); break;
                case "Print": output.AppendLine(Text(Evaluate(statement.Expression!))); if (output.Length > 30000) throw new InvalidOperationException("La salida supera 30 000 caracteres."); break;
                case "If": Block(Boolean(Evaluate(statement.Expression!)) ? statement.Body! : statement.Otherwise!); break;
                case "While": while (Boolean(Evaluate(statement.Expression!))) { Tick(); Block(statement.Body!); } break;
                case "DoWhile": do { Tick(); Block(statement.Body!); } while (Boolean(Evaluate(statement.Expression!))); break;
                case "For":
                    scopes.Add(new());
                    try { Execute(statement.Initial!); while (Boolean(Evaluate(statement.Expression!))) { Tick(); Block(statement.Body!); Execute(statement.Update!); } }
                    finally { scopes.RemoveAt(scopes.Count - 1); } break;
            }
        }
        public string Run(List<Stmt> statements) { foreach (var statement in statements) Execute(statement); return output.ToString().TrimEnd('\r', '\n'); }
    }

    private static string ExpressionCode(Expr expr) => expr.Kind switch
    {
        "Literal" => expr.Text, "Variable" => expr.Text,
        "Unary" => $"({expr.Text}{ExpressionCode(expr.Left!)})",
        _ => $"({ExpressionCode(expr.Left!)} {expr.Text} {ExpressionCode(expr.Right!)})"
    };
    private static string Generate(List<Stmt> statements, int level)
    {
        string pad = new(' ', level * 4);
        string Block(List<Stmt> body) => "{\n" + Generate(body, level + 1) + "\n" + pad + "}";
        return string.Join('\n', statements.Select(s => pad + (s.Kind switch
        {
            "Declaration" => $"{s.Type} {s.Name} = {ExpressionCode(s.Expression!)};",
            "Assignment" => $"{s.Name} = {ExpressionCode(s.Expression!)};",
            "Print" => $"Console.WriteLine({ExpressionCode(s.Expression!)});",
            "If" => $"if ({ExpressionCode(s.Expression!)}) {Block(s.Body!)}" + (s.Otherwise!.Count > 0 ? $" else {Block(s.Otherwise)}" : ""),
            "While" => $"while ({ExpressionCode(s.Expression!)}) {Block(s.Body!)}",
            "DoWhile" => $"do {Block(s.Body!)} while ({ExpressionCode(s.Expression!)});",
            "For" => $"for ({Generate([s.Initial!], 0).TrimEnd(';')}; {ExpressionCode(s.Expression!)}; {Generate([s.Update!], 0).TrimEnd(';')}) {Block(s.Body!)}",
            _ => throw new InvalidOperationException("Nodo no admitido.")
        })));
    }
    public static string Project(bool forms) => $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <OutputType>{(forms ? "WinExe" : "Exe")}</OutputType>\n    <TargetFramework>{(forms ? "net8.0-windows" : "net8.0")}</TargetFramework>\n    {(forms ? "<UseWindowsForms>true</UseWindowsForms>" : "")}\n  </PropertyGroup>\n</Project>\n";

    private static Compilation Query(string source)
    {
        var m = Regex.Match(source.Trim(), "^BUSCAR estudiante DONDE (edad|nombre)\\s*(>=|<=|!=|=|>|<)\\s*(\"(?:\\\\.|[^\"\\\\])*\"|\\d+)\\s*;?$");
        if (!m.Success) throw new InvalidOperationException("Usa BUSCAR estudiante DONDE edad > 18 o nombre = \"Ana\".");
        string field = m.Groups[1].Value, op = m.Groups[2].Value, raw = m.Groups[3].Value;
        if (field == "edad" && raw.StartsWith('"') || field == "nombre" && !raw.StartsWith('"')) throw new InvalidOperationException("El valor no corresponde al tipo del campo.");
        if (field == "nombre" && op is not ("=" or "!=")) throw new InvalidOperationException("Para nombre usa = o !=.");
        var students = new[] { new { nombre = "Ana", edad = 20 }, new { nombre = "Luis", edad = 17 }, new { nombre = "Marta", edad = 18 }, new { nombre = "Pedro", edad = 22 } };
        string text = field == "nombre" ? JsonSerializer.Deserialize<string>(raw)! : "";
        double number = field == "edad" ? double.Parse(raw, CultureInfo.InvariantCulture) : 0;
        var rows = students.Where(row => field == "nombre" ? (op == "=" ? row.nombre == text : row.nombre != text) : op switch
        { ">" => row.edad > number, "<" => row.edad < number, ">=" => row.edad >= number, "<=" => row.edad <= number, "=" => row.edad == number, _ => row.edad != number });
        string literal = field == "nombre" ? "'" + text.Replace("'", "''") + "'" : raw;
        string sql = $"SELECT *\nFROM estudiante\nWHERE {field} {(op == "!=" ? "<>" : op)} {literal};";
        return new Compilation("SQL", "consulta.sql", sql, string.Join('\n', rows.Select(row => $"{row.nombre} · {row.edad} años")), Lex(source), new { Table = "estudiante", Field = field, Operator = op, Value = raw });
    }
    private static Compilation Configuration(string source)
    {
        var lines = source.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith("//")).ToList();
        if (lines[0] is not ("FORMATO JSON" or "FORMATO XML")) throw new InvalidOperationException("Comienza con FORMATO JSON o FORMATO XML.");
        bool xml = lines[0].EndsWith("XML"); var values = new Dictionary<string, object>();
        foreach (string line in lines.Skip(1))
        {
            var m = Regex.Match(line, @"^CONFIG ([A-Za-z_]\w*)\s*=\s*(.+)$");
            if (!m.Success) throw new InvalidOperationException($"Configuración inválida: {line}");
            string name = m.Groups[1].Value, raw = m.Groups[2].Value;
            if (values.ContainsKey(name)) throw new InvalidOperationException($"Clave duplicada: {name}");
            using var document = JsonDocument.Parse(raw == "verdadero" ? "true" : raw == "falso" ? "false" : raw);
            var value = document.RootElement;
            values.Add(name, value.ValueKind switch { JsonValueKind.String => value.GetString()!, JsonValueKind.Number => value.GetDouble(), JsonValueKind.True => true, JsonValueKind.False => false, _ => throw new InvalidOperationException("Solo se admite texto, número y booleano.") });
        }
        string generated = xml ? new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("configuracion", values.Select(kv => new XElement(kv.Key, kv.Value)))).ToString() : JsonSerializer.Serialize(values, JsonOptions);
        return new Compilation(xml ? "XML" : "JSON", "configuracion." + (xml ? "xml" : "json"), generated, generated, Lex(source), values);
    }
    private static Compilation Forms(string source)
    {
        string? title = null; var controls = new List<object>(); var code = new List<string>(); int count = 0;
        foreach (string line in source.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith("//")))
        {
            var m = Regex.Match(line, "^(VENTANA|ETIQUETA|BOTON)\\s+(\"(?:\\\\.|[^\"\\\\])*\")(?: MENSAJE (\"(?:\\\\.|[^\"\\\\])*\"))?$");
            if (!m.Success) throw new InvalidOperationException($"Control inválido: {line}");
            string type = m.Groups[1].Value, text = JsonSerializer.Deserialize<string>(m.Groups[2].Value)!;
            string? message = m.Groups[3].Success ? JsonSerializer.Deserialize<string>(m.Groups[3].Value) : null;
            if (type != "BOTON" && message != null) throw new InvalidOperationException("Solo BOTON admite MENSAJE.");
            if (type == "VENTANA") { if (title != null) throw new InvalidOperationException("Solo se permite una VENTANA."); title = text; }
            else
            {
                code.Add($"        var c{count} = new {(type == "BOTON" ? "Button" : "Label")} {{ Text = {JsonSerializer.Serialize(text)}, AutoSize = true }};\n        panel.Controls.Add(c{count});");
                if (message != null) code.Add($"        c{count}.Click += (sender, e) => MessageBox.Show({JsonSerializer.Serialize(message)});");
                controls.Add(new { Type = type, Text = text, Message = message }); count++;
            }
        }
        if (title == null) throw new InvalidOperationException("Declara VENTANA \"Título\".");
        string generated = $"using System;\nusing System.Windows.Forms;\nclass Program\n{{\n    [STAThread]\n    static void Main()\n    {{\n        Application.EnableVisualStyles();\n        var form = new Form {{ Text = {JsonSerializer.Serialize(title)}, Width = 440, Height = 320 }};\n        var panel = new FlowLayoutPanel {{ Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(24) }};\n        form.Controls.Add(panel);\n{string.Join('\n', code)}\n        Application.Run(form);\n    }}\n}}\n";
        return new Compilation("WinForms", "Program.cs", generated, $"Ventana: {title}\nControles: {count}. Ejecuta el proyecto generado en Windows.", Lex(source), new { Title = title, Controls = controls }, Project(true));
    }
}
