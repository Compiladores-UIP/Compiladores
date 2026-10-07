/* Mini-compilador académico de pseudocódigo a C#.
 * Módulo independiente: navegador (window.PseudoCSharp) y Node.js (require).
 * No utiliza eval, Function ni ejecución dinámica del código generado.
 */
(function (root, factory) {
  "use strict";
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.PseudoCSharp = factory();
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  "use strict";

  const TYPES = new Set(["ENTERO", "DECIMAL", "TEXTO", "LOGICO"]);
  const KEYWORDS = new Set([
    "INICIO", "FIN", ...TYPES, "IMPRIMIR", "ESCRIBIR", "SI", "ENTONCES",
    "SINO", "FIN_SI", "MIENTRAS", "HACER", "FIN_MIENTRAS", "PARA",
    "HASTA", "PASO", "FIN_PARA", "VERDADERO", "FALSO", "Y", "O", "NO"
  ]);
  const CLOSERS = new Set(["FIN", "SINO", "FIN_SI", "FIN_PARA", "FIN_MIENTRAS"]);
  const PRECEDENCE = { "||": 1, "&&": 2, "==": 3, "!=": 3, "<": 4, ">": 4,
    "<=": 4, ">=": 4, "+": 5, "-": 5, "*": 6, "/": 6, "%": 6 };
  const CSHARP_TYPES = { ENTERO: "int", DECIMAL: "double", TEXTO: "string", LOGICO: "bool" };
  const MIN_INT = -2147483648;
  const MAX_INT = 2147483647;
  const LIMITS = Object.freeze({ sourceChars: 50000, tokens: 20000, nesting: 64,
    steps: 10000, outputLines: 500, outputChars: 50000 });

  class CompilerError extends Error {
    constructor(phase, message, token = { line: 1, column: 1 }, code = "PC001") {
      super(message);
      this.name = "CompilerError";
      this.diagnostic = { phase, code, message, line: token.line, column: token.column };
    }
  }

  function fail(phase, message, token, code) {
    throw new CompilerError(phase, message, token, code);
  }

  function tokenize(source) {
    if (typeof source !== "string") fail("Léxico", "La entrada debe ser texto.", undefined, "LEX001");
    if (source.length > LIMITS.sourceChars) fail("Léxico", "La entrada supera los 50000 caracteres.", undefined, "LEX002");
    const tokens = [];
    let offset = 0, line = 1, column = 1;
    const advance = () => { column++; return source[offset++]; };
    function emit(type, value, raw, at) {
      tokens.push({ type, value, raw, line: at.line, column: at.column });
      if (tokens.length > LIMITS.tokens) fail("Léxico", "Demasiados tokens para este mini-compilador.", at, "LEX003");
    }
    while (offset < source.length) {
      const ch = source[offset];
      const at = { line, column };
      if (ch === "\uFEFF" && offset === 0) { offset++; continue; }
      if (ch === " " || ch === "\t") { advance(); continue; }
      if (ch === "\r" || ch === "\n") {
        const start = offset;
        offset += ch === "\r" && source[offset + 1] === "\n" ? 2 : 1;
        emit("NEWLINE", "\n", source.slice(start, offset), at);
        line++; column = 1; continue;
      }
      if (ch === "#" || (ch === "/" && source[offset + 1] === "/")) {
        while (offset < source.length && !/[\r\n]/.test(source[offset])) advance();
        continue;
      }
      if (ch === '"') {
        const start = offset;
        advance();
        let value = "", closed = false;
        while (offset < source.length) {
          if (/[\r\n]/.test(source[offset])) fail("Léxico", "La cadena debe cerrarse en la misma línea.", at, "LEX004");
          const current = advance();
          if (current === '"') { closed = true; break; }
          if (current === "\\") {
            const escape = advance();
            const escapes = { n: "\n", t: "\t", r: "\r", '"': '"', "\\": "\\" };
            if (!Object.hasOwn(escapes, escape)) fail("Léxico", "Secuencia de escape no admitida en la cadena.", at, "LEX005");
            value += escapes[escape];
          } else value += current;
        }
        if (!closed) fail("Léxico", "Falta la comilla de cierre de la cadena.", at, "LEX004");
        emit("STRING", value, source.slice(start, offset), at); continue;
      }
      if (/[0-9]/.test(ch)) {
        const start = offset;
        while (offset < source.length && /[0-9]/.test(source[offset])) advance();
        if (source[offset] === ".") {
          advance();
          if (!/[0-9]/.test(source[offset] || "")) fail("Léxico", "Se esperaba un dígito después del punto decimal.", at, "LEX006");
          while (offset < source.length && /[0-9]/.test(source[offset])) advance();
        }
        const raw = source.slice(start, offset);
        emit("NUMBER", raw, raw, at); continue;
      }
      if (/[A-Za-z_]/.test(ch)) {
        const start = offset;
        while (offset < source.length && /[A-Za-z0-9_]/.test(source[offset])) advance();
        const raw = source.slice(start, offset);
        const upper = raw.toUpperCase();
        emit(KEYWORDS.has(upper) ? upper : "IDENTIFIER", KEYWORDS.has(upper) ? upper : raw, raw, at);
        continue;
      }
      const pair = source.slice(offset, offset + 2);
      const pairs = { "<-": "ASSIGN", "==": "OPERATOR", "!=": "OPERATOR", "<>": "OPERATOR",
        "<=": "OPERATOR", ">=": "OPERATOR", "&&": "OPERATOR", "||": "OPERATOR" };
      if (Object.hasOwn(pairs, pair)) {
        advance(); advance(); emit(pairs[pair], pair === "<>" ? "!=" : pair, pair, at); continue;
      }
      const singles = { "=": "ASSIGN", "+": "OPERATOR", "-": "OPERATOR", "*": "OPERATOR",
        "/": "OPERATOR", "%": "OPERATOR", "<": "OPERATOR", ">": "OPERATOR", "!": "OPERATOR",
        "(": "LPAREN", ")": "RPAREN", ",": "COMMA", ";": "SEMICOLON" };
      if (Object.hasOwn(singles, ch)) { advance(); emit(singles[ch], ch, ch, at); continue; }
      fail("Léxico", `Carácter no permitido: ${JSON.stringify(ch)}.`, at, "LEX007");
    }
    tokens.push({ type: "EOF", value: "", raw: "", line, column });
    return tokens;
  }

  class Parser {
    constructor(tokens) { this.tokens = tokens; this.position = 0; this.depth = 0; }
    current() { return this.tokens[this.position]; }
    take() { return this.tokens[this.position++]; }
    is(type) { return this.current().type === type; }
    accept(type) { return this.is(type) ? this.take() : null; }
    expect(type, message) {
      if (!this.is(type)) fail("Sintáctico", message || `Se esperaba ${type}.`, this.current(), "SYN001");
      return this.take();
    }
    skipLines() { while (this.accept("NEWLINE")) { /* líneas vacías */ } }
    lineEnd() {
      this.accept("SEMICOLON");
      if (!this.is("NEWLINE") && !this.is("EOF")) {
        fail("Sintáctico", "Se esperaba el fin de la línea; use una instrucción por línea.", this.current(), "SYN002");
      }
      this.skipLines();
    }
    parse() {
      this.skipLines();
      const token = this.expect("INICIO", "El programa debe comenzar con INICIO.");
      this.lineEnd();
      const body = this.block(new Set(["FIN"]), "FIN", token);
      this.expect("FIN", "Falta FIN para cerrar el programa.");
      this.lineEnd();
      this.expect("EOF", "No se permiten instrucciones después de FIN.");
      return { kind: "Program", body, token };
    }
    block(stops, expected, opener) {
      if (++this.depth > LIMITS.nesting) fail("Sintáctico", "Demasiados bloques anidados.", opener, "SYN003");
      const body = [];
      this.skipLines();
      while (!stops.has(this.current().type)) {
        if (this.is("EOF") || CLOSERS.has(this.current().type)) {
          fail("Sintáctico", `Falta ${expected} para cerrar el bloque abierto en la línea ${opener.line}.`, this.current(), "SYN004");
        }
        body.push(this.statement());
      }
      this.depth--;
      return body;
    }
    statement() {
      const token = this.take();
      if (TYPES.has(token.type)) {
        const name = this.expect("IDENTIFIER", "Se esperaba el nombre de la variable.");
        this.expect("ASSIGN", "La declaración necesita = o <- y un valor inicial.");
        const initializer = this.expression();
        this.lineEnd();
        return { kind: "Declaration", dataType: token.type, name: name.value, nameToken: name, initializer, token };
      }
      if (token.type === "IDENTIFIER") {
        this.expect("ASSIGN", "Se esperaba = o <- después del identificador.");
        const value = this.expression();
        this.lineEnd();
        return { kind: "Assignment", name: token.value, value, token };
      }
      if (token.type === "IMPRIMIR" || token.type === "ESCRIBIR") {
        const expressions = [this.expression()];
        while (this.accept("COMMA")) expressions.push(this.expression());
        this.lineEnd();
        return { kind: "Print", expressions, token };
      }
      if (token.type === "SI") {
        const condition = this.expression();
        this.accept("ENTONCES"); this.lineEnd();
        const thenBody = this.block(new Set(["SINO", "FIN_SI"]), "FIN_SI", token);
        let elseBody = [];
        if (this.accept("SINO")) {
          this.lineEnd();
          elseBody = this.block(new Set(["FIN_SI"]), "FIN_SI", token);
        }
        this.expect("FIN_SI"); this.lineEnd();
        return { kind: "If", condition, thenBody, elseBody, token };
      }
      if (token.type === "MIENTRAS") {
        const condition = this.expression();
        this.accept("HACER"); this.lineEnd();
        const body = this.block(new Set(["FIN_MIENTRAS"]), "FIN_MIENTRAS", token);
        this.expect("FIN_MIENTRAS"); this.lineEnd();
        return { kind: "While", condition, body, token };
      }
      if (token.type === "PARA") {
        const name = this.expect("IDENTIFIER", "Se esperaba el contador después de PARA.");
        this.expect("ASSIGN", "Se esperaba = o <- después del contador.");
        const from = this.expression();
        this.expect("HASTA", "El ciclo PARA necesita HASTA y un límite.");
        const to = this.expression();
        let step = 1;
        if (this.accept("PASO")) {
          let sign = 1;
          if (this.is("OPERATOR") && ["+", "-"].includes(this.current().value)) sign = this.take().value === "-" ? -1 : 1;
          const literal = this.expect("NUMBER", "PASO debe ser un entero literal distinto de cero.");
          if (literal.raw.includes(".")) fail("Sintáctico", "PASO debe ser un entero literal.", literal, "SYN005");
          step = sign * Number(literal.value);
        }
        this.lineEnd();
        const body = this.block(new Set(["FIN_PARA"]), "FIN_PARA", token);
        this.expect("FIN_PARA"); this.lineEnd();
        return { kind: "For", name: name.value, nameToken: name, from, to, step, body, token };
      }
      fail("Sintáctico", `Instrucción no reconocida: ${token.raw || token.type}.`, token, "SYN006");
    }
    operator(token) {
      if (token.type === "Y") return "&&";
      if (token.type === "O") return "||";
      return token.type === "OPERATOR" ? token.value : null;
    }
    expression(minimum = 0, depth = 0) {
      if (depth > LIMITS.nesting) fail("Sintáctico", "Expresión demasiado anidada.", this.current(), "SYN003");
      const token = this.take();
      let left;
      if (token.type === "NUMBER") {
        left = { kind: "Literal", value: Number(token.value), raw: token.raw,
          valueType: token.raw.includes(".") ? "DECIMAL" : "ENTERO", token };
      } else if (token.type === "STRING") {
        left = { kind: "Literal", value: token.value, valueType: "TEXTO", token };
      } else if (["VERDADERO", "FALSO"].includes(token.type)) {
        left = { kind: "Literal", value: token.type === "VERDADERO", valueType: "LOGICO", token };
      } else if (token.type === "IDENTIFIER") {
        left = { kind: "Identifier", name: token.value, token };
      } else if (token.type === "LPAREN") {
        left = this.expression(0, depth + 1);
        this.expect("RPAREN", "Falta ) para cerrar la expresión.");
      } else if (token.type === "NO" || (token.type === "OPERATOR" && ["+", "-", "!"].includes(token.value))) {
        left = { kind: "Unary", operator: token.type === "NO" ? "!" : token.value,
          operand: this.expression(7, depth + 1), token };
      } else fail("Sintáctico", "Se esperaba una expresión, una variable o un valor.", token, "SYN007");
      left.height = left.kind === "Unary" ? left.operand.height + 1 : (left.height || 1);
      while (true) {
        const operator = this.operator(this.current());
        const precedence = PRECEDENCE[operator];
        if (precedence === undefined || precedence < minimum) break;
        const opToken = this.take();
        const right = this.expression(precedence + 1, depth + 1);
        const height = 1 + Math.max(left.height, right.height);
        if (height > LIMITS.nesting) fail("Sintáctico", "Expresión demasiado compleja para este mini-compilador.", opToken, "SYN003");
        left = { kind: "Binary", operator, left, right, height, token: opToken };
      }
      return left;
    }
  }

  class Analyzer {
    constructor() { this.scopes = [new Map()]; this.names = new Set(); this.symbols = []; this.scopeNumber = 0; }
    resolve(name, token) {
      for (let i = this.scopes.length - 1; i >= 0; i--) if (this.scopes[i].has(name)) return this.scopes[i].get(name);
      fail("Semántico", `La variable ${name} no está declarada o no es visible en este bloque.`, token, "SEM001");
    }
    declare(name, type, token, readonly = false) {
      if (this.names.has(name)) fail("Semántico", `El identificador ${name} ya fue declarado; use nombres únicos.`, token, "SEM002");
      this.names.add(name);
      const symbol = { name, type, csharpName: `v_${name}`, line: token.line, column: token.column,
        scope: this.scopes.length === 1 ? "principal" : `bloque ${this.scopeNumber}`, readonly };
      this.scopes[this.scopes.length - 1].set(name, symbol);
      this.symbols.push(symbol);
      return symbol;
    }
    numeric(type) { return type === "ENTERO" || type === "DECIMAL"; }
    assignable(target, source) { return target === source || (target === "DECIMAL" && source === "ENTERO"); }
    ensureAssignment(target, source, token) {
      if (!this.assignable(target, source)) fail("Semántico", `No se puede asignar ${source} a ${target}.`, token, "SEM003");
    }
    expression(node) {
      if (node.kind === "Literal") {
        if (this.numeric(node.valueType) && !Number.isFinite(node.value)) fail("Semántico", "Literal numérico fuera de rango.", node.token, "SEM004");
        if (node.valueType === "ENTERO" && (node.value < MIN_INT || node.value > MAX_INT)) fail("Semántico", "El literal ENTERO está fuera del rango de int de C#.", node.token, "SEM004");
        node.type = node.valueType;
      } else if (node.kind === "Identifier") {
        const symbol = this.resolve(node.name, node.token);
        node.type = symbol.type; node.csharpName = symbol.csharpName;
      } else if (node.kind === "Unary") {
        // C# admite el mínimo int como un literal negativo, no como positivo.
        if (node.operator === "-" && node.operand.kind === "Literal" && node.operand.valueType === "ENTERO" && node.operand.value === 2147483648) {
          node.minInt = true; node.type = "ENTERO"; node.operand.type = "ENTERO";
          node.constantKnown = true; node.constantValue = MIN_INT; return node.type;
        }
        const operand = this.expression(node.operand);
        if (node.operator === "!") {
          if (operand !== "LOGICO") fail("Semántico", "NO o ! necesita un valor LOGICO.", node.token, "SEM005");
          node.type = "LOGICO";
        } else {
          if (!this.numeric(operand)) fail("Semántico", "El signo unario necesita un valor numérico.", node.token, "SEM005");
          node.type = operand;
        }
      } else if (node.kind === "Binary") {
        const left = this.expression(node.left), right = this.expression(node.right);
        if (["&&", "||"].includes(node.operator)) {
          if (left !== "LOGICO" || right !== "LOGICO") fail("Semántico", "Y/O necesita dos valores LOGICO.", node.token, "SEM005");
          node.type = "LOGICO";
        } else if (["==", "!="].includes(node.operator)) {
          if (left !== right && !(this.numeric(left) && this.numeric(right))) fail("Semántico", "La comparación usa tipos incompatibles.", node.token, "SEM005");
          node.type = "LOGICO";
        } else if (["<", ">", "<=", ">="].includes(node.operator)) {
          if (!this.numeric(left) || !this.numeric(right)) fail("Semántico", "La comparación de orden necesita números.", node.token, "SEM005");
          node.type = "LOGICO";
        } else if (node.operator === "+" && left === "TEXTO" && right === "TEXTO") node.type = "TEXTO";
        else {
          if (!this.numeric(left) || !this.numeric(right)) fail("Semántico", "El operador aritmético necesita números; + también admite TEXTO + TEXTO.", node.token, "SEM005");
          node.type = left === "DECIMAL" || right === "DECIMAL" ? "DECIMAL" : "ENTERO";
        }
      }
      this.checkConstant(node);
      return node.type;
    }
    checkConstant(node) {
      if (node.kind === "Literal") { node.constantKnown = true; node.constantValue = node.value; }
      else if (node.kind === "Unary" && node.operand.constantKnown) {
        node.constantKnown = true;
        node.constantValue = node.operator === "!" ? !node.operand.constantValue :
          node.operator === "-" ? -node.operand.constantValue : node.operand.constantValue;
      } else if (node.kind === "Binary" && node.left.constantKnown && node.right.constantKnown) {
        const a = node.left.constantValue, b = node.right.constantValue;
        if (node.type === "ENTERO" && ["/", "%"].includes(node.operator) && b === 0) {
          fail("Semántico", "División o residuo entero entre cero en una expresión constante.", node.token, "SEM010");
        }
        if (node.type === "ENTERO" && node.operator === "%" && a === MIN_INT && b === -1) {
          fail("Semántico", "Desbordamiento en una expresión constante ENTERO.", node.token, "SEM010");
        }
        const operations = { "+": () => a + b, "-": () => a - b, "*": () => a * b,
          "/": () => node.type === "ENTERO" ? Math.trunc(a / b) : a / b, "%": () => a % b,
          "==": () => a === b, "!=": () => a !== b, "<": () => a < b, ">": () => a > b,
          "<=": () => a <= b, ">=": () => a >= b, "&&": () => a && b, "||": () => a || b };
        node.constantKnown = true; node.constantValue = operations[node.operator]();
      }
      if (node.constantKnown && node.type === "ENTERO" &&
          (!Number.isInteger(node.constantValue) || node.constantValue < MIN_INT || node.constantValue > MAX_INT)) {
        fail("Semántico", "Desbordamiento en una expresión constante ENTERO.", node.token, "SEM010");
      }
    }
    condition(node) {
      if (this.expression(node) !== "LOGICO") fail("Semántico", "La condición de SI o MIENTRAS debe ser LOGICO.", node.token, "SEM006");
    }
    block(body, child = true) {
      if (child) { this.scopes.push(new Map()); this.scopeNumber++; }
      for (const node of body) {
        switch (node.kind) {
          case "Declaration": {
            this.ensureAssignment(node.dataType, this.expression(node.initializer), node.token);
            node.csharpName = this.declare(node.name, node.dataType, node.nameToken).csharpName;
            break;
          }
          case "Assignment": {
            const symbol = this.resolve(node.name, node.token);
            if (symbol.readonly) fail("Semántico", "No se permite modificar el contador de PARA dentro del ciclo.", node.token, "SEM007");
            this.ensureAssignment(symbol.type, this.expression(node.value), node.token);
            node.csharpName = symbol.csharpName; node.type = symbol.type;
            break;
          }
          case "Print": node.expressions.forEach(expr => this.expression(expr)); break;
          case "If": this.condition(node.condition); this.block(node.thenBody); this.block(node.elseBody); break;
          case "While": this.condition(node.condition); this.block(node.body); break;
          case "For": {
            if (this.expression(node.from) !== "ENTERO" || this.expression(node.to) !== "ENTERO") fail("Semántico", "Los límites de PARA deben ser ENTERO.", node.token, "SEM008");
            if (!Number.isInteger(node.step) || node.step === 0 || node.step < MIN_INT || node.step > MAX_INT) fail("Semántico", "PASO debe ser un entero int distinto de cero.", node.token, "SEM009");
            this.scopes.push(new Map()); this.scopeNumber++;
            node.csharpName = this.declare(node.name, "ENTERO", node.nameToken, true).csharpName;
            this.block(node.body, false); this.scopes.pop();
            break;
          }
        }
      }
      if (child) this.scopes.pop();
    }
    analyze(ast) { this.block(ast.body, false); return this.symbols; }
  }

  function quoteCSharp(value) {
    const replacements = { '"': '\\"', "\\": "\\\\", "\n": "\\n", "\r": "\\r", "\t": "\\t", "\b": "\\b", "\f": "\\f" };
    return '"' + value.replace(/["\\\u0000-\u001f\u007f\u2028\u2029]/g, ch =>
      replacements[ch] || "\\u" + ch.charCodeAt(0).toString(16).padStart(4, "0")) + '"';
  }
  function emitExpression(node) {
    if (node.kind === "Literal") {
      if (node.type === "TEXTO") return quoteCSharp(node.value);
      if (node.type === "LOGICO") return node.value ? "true" : "false";
      return node.type === "DECIMAL" ? `${node.raw}d` : String(node.value);
    }
    if (node.kind === "Identifier") return node.csharpName;
    if (node.kind === "Unary") return node.minInt ? "(-2147483648)" : `(${node.operator}${emitExpression(node.operand)})`;
    return `(${emitExpression(node.left)} ${node.operator} ${emitExpression(node.right)})`;
  }
  function generateCSharp(ast) {
    const lines = ["// Generado por el mini-compilador académico Pseudocódigo a C#.",
      "using System;", "using System.Globalization;", "", "class Program", "{", "    static void Main()", "    {",
      "        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;", "        checked", "        {"];
    let loopIndex = 0;
    const line = (level, text) => lines.push("    ".repeat(level) + text);
    function body(nodes, level) {
      for (const node of nodes) {
        if (node.kind === "Declaration") line(level, `${CSHARP_TYPES[node.dataType]} ${node.csharpName} = ${emitExpression(node.initializer)};`);
        else if (node.kind === "Assignment") line(level, `${node.csharpName} = ${emitExpression(node.value)};`);
        else if (node.kind === "Print") {
          const values = node.expressions.map(emitExpression);
          line(level, values.length === 1 ? `Console.WriteLine(${values[0]});` : `Console.WriteLine(string.Concat(new object[] { ${values.join(", ")} }));`);
        } else if (node.kind === "If") {
          line(level, `if (${emitExpression(node.condition)})`); line(level, "{"); body(node.thenBody, level + 1); line(level, "}");
          if (node.elseBody.length) { line(level, "else"); line(level, "{"); body(node.elseBody, level + 1); line(level, "}"); }
        } else if (node.kind === "While") {
          line(level, `while (${emitExpression(node.condition)})`); line(level, "{"); body(node.body, level + 1); line(level, "}");
        } else if (node.kind === "For") {
          const id = ++loopIndex, counter = `__pc_i${id}`, end = `__pc_fin${id}`;
          // long evita desbordar int al incrementar el último valor del rango.
          line(level, `for (long ${counter} = ${emitExpression(node.from)}, ${end} = ${emitExpression(node.to)}; ${counter} ${node.step > 0 ? "<=" : ">="} ${end}; ${counter} += ${node.step}L)`);
          line(level, "{"); line(level + 1, `int ${node.csharpName} = (int)${counter};`);
          body(node.body, level + 1); line(level, "}");
        }
      }
    }
    body(ast.body, 3);
    lines.push("        }", "    }", "}", "");
    return lines.join("\n");
  }

  function compile(source) {
    let tokens = [], ast = null, symbols = [];
    try {
      tokens = tokenize(source);
      ast = new Parser(tokens).parse();
      const analyzer = new Analyzer();
      symbols = analyzer.analyze(ast);
      return { ok: true, code: generateCSharp(ast), tokens, ast, symbols, diagnostics: [] };
    } catch (error) {
      if (!(error instanceof CompilerError)) throw error;
      return { ok: false, code: "", tokens, ast: null, symbols: [], diagnostics: [error.diagnostic] };
    }
  }

  // La simulación interpreta el AST: NO compila ni ejecuta C# en el navegador.
  function simulate(compilation, options = {}) {
    const result = typeof compilation === "string" ? compile(compilation) : compilation;
    if (!result || !result.ok) return { ok: false, output: "", lines: [], steps: 0,
      diagnostics: result ? result.diagnostics : [{ phase: "Ejecución", message: "Compile una entrada válida.", line: 1, column: 1, code: "RUN001" }] };
    const maxSteps = Math.min(Math.max(Number(options.maxSteps) || LIMITS.steps, 1), LIMITS.steps);
    const scopes = [new Map()], lines = [];
    let steps = 0, outputChars = 0;
    const tick = token => { if (++steps > maxSteps) fail("Ejecución", `Se alcanzó el límite de ${maxSteps} pasos de la simulación. Revise el ciclo.`, token, "RUN002"); };
    function resolve(name) {
      for (let i = scopes.length - 1; i >= 0; i--) if (scopes[i].has(name)) return scopes[i];
      throw new Error("AST inválido: variable no resuelta.");
    }
    function checkedInt(value, token) {
      if (value < MIN_INT || value > MAX_INT || !Number.isInteger(value)) fail("Ejecución", "Desbordamiento de ENTERO (int de 32 bits).", token, "RUN003");
      return value;
    }
    function evaluate(node) {
      tick(node.token);
      if (node.kind === "Literal") return node.value;
      if (node.kind === "Identifier") return resolve(node.name).get(node.name);
      if (node.kind === "Unary") {
        if (node.minInt) return MIN_INT;
        const value = evaluate(node.operand);
        const answer = node.operator === "!" ? !value : node.operator === "-" ? -value : value;
        return node.type === "ENTERO" ? checkedInt(answer, node.token) : answer;
      }
      const left = evaluate(node.left);
      if (node.operator === "&&") return left && evaluate(node.right);
      if (node.operator === "||") return left || evaluate(node.right);
      const right = evaluate(node.right);
      let answer;
      switch (node.operator) {
        case "+": answer = left + right; break;
        case "-": answer = left - right; break;
        case "*": answer = left * right; break;
        case "/":
          if (node.type === "ENTERO" && right === 0) fail("Ejecución", "División entera entre cero.", node.token, "RUN004");
          answer = node.type === "ENTERO" ? Math.trunc(left / right) : left / right; break;
        case "%":
          if (node.type === "ENTERO" && right === 0) fail("Ejecución", "Residuo entero entre cero.", node.token, "RUN004");
          // int.MinValue % -1 también lanza OverflowException en .NET.
          if (node.type === "ENTERO" && left === MIN_INT && right === -1) fail("Ejecución", "Desbordamiento de ENTERO.", node.token, "RUN003");
          answer = left % right; break;
        case "==": answer = left === right; break;
        case "!=": answer = left !== right; break;
        case "<": answer = left < right; break;
        case ">": answer = left > right; break;
        case "<=": answer = left <= right; break;
        case ">=": answer = left >= right; break;
      }
      return node.type === "ENTERO" ? checkedInt(answer, node.token) : answer;
    }
    function format(value) { return typeof value === "boolean" ? (value ? "True" : "False") : String(Object.is(value, -0) ? 0 : value); }
    function body(nodes, child = true) {
      if (child) scopes.push(new Map());
      for (const node of nodes) {
        tick(node.token);
        switch (node.kind) {
          case "Declaration": scopes[scopes.length - 1].set(node.name, evaluate(node.initializer)); break;
          case "Assignment": resolve(node.name).set(node.name, evaluate(node.value)); break;
          case "Print": {
            const text = node.expressions.map(expr => format(evaluate(expr))).join("");
            outputChars += text.length + 1;
            if (lines.length >= LIMITS.outputLines || outputChars > LIMITS.outputChars) fail("Ejecución", "La salida supera el límite seguro de la vista previa.", node.token, "RUN005");
            lines.push(text); break;
          }
          case "If": body(evaluate(node.condition) ? node.thenBody : node.elseBody); break;
          case "While": while (evaluate(node.condition)) { tick(node.token); body(node.body); } break;
          case "For": {
            const from = evaluate(node.from), to = evaluate(node.to);
            for (let i = from; node.step > 0 ? i <= to : i >= to; i += node.step) {
              tick(node.token); scopes.push(new Map([[node.name, i]])); body(node.body, false); scopes.pop();
            }
            break;
          }
        }
      }
      if (child) scopes.pop();
    }
    try {
      body(result.ast.body, false);
      return { ok: true, output: lines.join("\n") + (lines.length ? "\n" : ""), lines, steps, diagnostics: [] };
    } catch (error) {
      if (!(error instanceof CompilerError)) throw error;
      return { ok: false, output: lines.join("\n") + (lines.length ? "\n" : ""), lines, steps, diagnostics: [error.diagnostic] };
    }
  }

  return Object.freeze({ compile, tokenize, simulate, LIMITS, version: "1.0.0" });
});
