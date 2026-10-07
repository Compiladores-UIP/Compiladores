/* Tema 05 · Compilador de mensajes personalizados.
 * Lenguaje didáctico propio -> C#; el AST también sirve para una vista simulada.
 * API independiente para navegador y Node.js. No ejecuta texto con eval.
 */
(function (root, factory) {
  "use strict";
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.MessageCompiler = factory();
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  "use strict";
  const KEYWORDS = new Set(["INICIO", "FIN", "TEXTO", "MENSAJE", "IMPRIMIR"]);
  const LIMITS = Object.freeze({ sourceChars: 50000, tokens: 20000, outputChars: 50000, outputLines: 500 });

  class CompilerError extends Error {
    constructor(phase, message, token = { line: 1, column: 1 }, code = "MSG001") {
      super(message);
      this.diagnostic = { phase, message, line: token.line, column: token.column, code };
    }
  }
  function fail(phase, message, token, code) { throw new CompilerError(phase, message, token, code); }

  function tokenize(source) {
    if (typeof source !== "string") fail("Léxico", "La entrada debe ser texto.", undefined, "MSG_LEX001");
    if (source.length > LIMITS.sourceChars) fail("Léxico", "La entrada supera los 50000 caracteres.", undefined, "MSG_LEX002");
    const tokens = [];
    let offset = 0, line = 1, column = 1;
    const advance = () => { column++; return source[offset++]; };
    function emit(type, value, raw, at, locations) {
      tokens.push({ type, value, raw, ...at, ...(locations ? { locations } : {}) });
      if (tokens.length > LIMITS.tokens) fail("Léxico", "Demasiados tokens.", at, "MSG_LEX003");
    }
    while (offset < source.length) {
      const ch = source[offset], at = { line, column };
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
        const start = offset, locations = [];
        let value = "", closed = false;
        advance();
        while (offset < source.length) {
          const currentAt = { line, column };
          if (/[\r\n]/.test(source[offset])) fail("Léxico", "La cadena debe cerrarse en la misma línea.", at, "MSG_LEX004");
          const current = advance();
          if (current === '"') { closed = true; break; }
          if (current === "\\") {
            if (offset >= source.length) fail("Léxico", "Falta la comilla de cierre.", at, "MSG_LEX004");
            const escape = advance(), escapes = { n: "\n", r: "\r", t: "\t", '"': '"', "\\": "\\" };
            if (!Object.hasOwn(escapes, escape)) fail("Léxico", "Secuencia de escape no admitida.", currentAt, "MSG_LEX005");
            value += escapes[escape];
          } else value += current;
          locations.push(currentAt);
        }
        if (!closed) fail("Léxico", "Falta la comilla de cierre.", at, "MSG_LEX004");
        emit("STRING", value, source.slice(start, offset), at, locations); continue;
      }
      if (/[A-Za-z_]/.test(ch)) {
        const start = offset;
        while (offset < source.length && /[A-Za-z0-9_]/.test(source[offset])) advance();
        const raw = source.slice(start, offset), upper = raw.toUpperCase();
        emit(KEYWORDS.has(upper) ? upper : "IDENTIFIER", KEYWORDS.has(upper) ? upper : raw, raw, at); continue;
      }
      if (ch === "=" || ch === ";") {
        advance(); emit(ch === "=" ? "ASSIGN" : "SEMICOLON", ch, ch, at); continue;
      }
      fail("Léxico", `Carácter no permitido: ${JSON.stringify(ch)}.`, at, "MSG_LEX006");
    }
    tokens.push({ type: "EOF", value: "", raw: "", line, column });
    return tokens;
  }

  // Las llaves se analizan dentro de cada cadena de MENSAJE, no como tokens externos.
  function templateParts(token) {
    const parts = [], value = token.value;
    let text = "";
    const flush = () => { if (text) { parts.push({ kind: "Text", value: text }); text = ""; } };
    for (let i = 0; i < value.length;) {
      const ch = value[i], at = token.locations[i] || token;
      if ((ch === "{" || ch === "}") && value[i + 1] === ch) { text += ch; i += 2; continue; }
      if (ch === "}") fail("Sintáctico", "Llave de cierre sin apertura; escriba }} para una llave literal.", at, "MSG_SYN010");
      if (ch !== "{") { text += ch; i++; continue; }
      const end = value.indexOf("}", i + 1);
      if (end < 0) fail("Sintáctico", "Falta } para cerrar el marcador del mensaje.", at, "MSG_SYN010");
      const name = value.slice(i + 1, end);
      if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(name)) fail("Sintáctico", "El marcador debe contener un identificador: {nombre}.", at, "MSG_SYN011");
      flush(); parts.push({ kind: "Placeholder", name, token: at }); i = end + 1;
    }
    flush();
    return parts;
  }

  class Parser {
    constructor(tokens) { this.tokens = tokens; this.index = 0; }
    current() { return this.tokens[this.index]; }
    take() { return this.tokens[this.index++]; }
    accept(type) { return this.current().type === type ? this.take() : null; }
    expect(type, message) {
      if (this.current().type !== type) fail("Sintáctico", message, this.current(), "MSG_SYN001");
      return this.take();
    }
    skipLines() { while (this.accept("NEWLINE")) { /* líneas vacías */ } }
    lineEnd() {
      this.accept("SEMICOLON");
      if (!["NEWLINE", "EOF"].includes(this.current().type)) fail("Sintáctico", "Use una instrucción por línea.", this.current(), "MSG_SYN002");
      this.skipLines();
    }
    parse() {
      this.skipLines();
      const token = this.expect("INICIO", "El programa debe comenzar con INICIO.");
      this.lineEnd();
      const body = [];
      while (this.current().type !== "FIN") {
        if (this.current().type === "EOF") fail("Sintáctico", "Falta FIN para cerrar el programa.", this.current(), "MSG_SYN003");
        body.push(this.statement());
      }
      this.take(); this.lineEnd();
      this.expect("EOF", "No se permiten instrucciones después de FIN.");
      return { kind: "Program", body, token };
    }
    statement() {
      const token = this.take();
      if (token.type === "TEXTO" || token.type === "IDENTIFIER") {
        const declaration = token.type === "TEXTO";
        const nameToken = declaration ? this.expect("IDENTIFIER", "Se esperaba el nombre de la variable.") : token;
        this.expect("ASSIGN", "Se esperaba = después del nombre.");
        const value = this.expect("STRING", "Se esperaba un texto entre comillas dobles.").value;
        this.lineEnd();
        return { kind: declaration ? "Declaration" : "Assignment", name: nameToken.value, value, token: nameToken };
      }
      if (["MENSAJE", "IMPRIMIR"].includes(token.type)) {
        const template = this.expect("STRING", `Se esperaba una cadena después de ${token.type}.`);
        const parts = templateParts(template);
        this.lineEnd();
        return { kind: "Message", template: template.value, parts, token };
      }
      fail("Sintáctico", "Instrucción no admitida. Use TEXTO, MENSAJE o una asignación.", token, "MSG_SYN004");
    }
  }

  function analyze(ast) {
    const symbols = new Map();
    for (const node of ast.body) {
      if (node.kind === "Declaration") {
        if (symbols.has(node.name)) fail("Semántico", `La variable ${node.name} ya fue declarada.`, node.token, "MSG_SEM001");
        node.csharpName = `m_${node.name}`;
        symbols.set(node.name, { name: node.name, type: "TEXTO", csharpName: node.csharpName,
          initialValue: node.value, line: node.token.line, column: node.token.column });
      } else if (node.kind === "Assignment") {
        if (!symbols.has(node.name)) fail("Semántico", `La variable ${node.name} no está declarada.`, node.token, "MSG_SEM002");
        node.csharpName = symbols.get(node.name).csharpName;
      } else {
        for (const part of node.parts) if (part.kind === "Placeholder") {
          if (!symbols.has(part.name)) fail("Semántico", `La variable ${part.name} no está declarada antes del mensaje.`, part.token, "MSG_SEM002");
          part.csharpName = symbols.get(part.name).csharpName;
        }
      }
    }
    return [...symbols.values()];
  }

  function quoteCSharp(value) {
    const replacements = { '"': '\\"', "\\": "\\\\", "\n": "\\n", "\r": "\\r", "\t": "\\t", "\b": "\\b", "\f": "\\f" };
    return '"' + value.replace(/["\\\u0000-\u001f\u007f\u2028\u2029]/g, ch =>
      replacements[ch] || "\\u" + ch.charCodeAt(0).toString(16).padStart(4, "0")) + '"';
  }
  function generateCSharp(ast) {
    const lines = ["// Generado por el compilador de mensajes personalizados · Grupo 5, tema 05.",
      "using System;", "", "class Program", "{", "    static void Main()", "    {"];
    for (const node of ast.body) {
      if (node.kind === "Declaration") lines.push(`        string ${node.csharpName} = ${quoteCSharp(node.value)};`);
      else if (node.kind === "Assignment") lines.push(`        ${node.csharpName} = ${quoteCSharp(node.value)};`);
      else {
        const values = node.parts.map(part => part.kind === "Text" ? quoteCSharp(part.value) : part.csharpName);
        const expression = values.length === 0 ? '""' : values.length === 1 ? values[0]
          : `string.Concat(new string[] { ${values.join(", ")} })`;
        lines.push(`        Console.WriteLine(${expression});`);
      }
    }
    lines.push("    }", "}", "");
    return lines.join("\n");
  }

  function compile(source) {
    let tokens = [];
    try {
      tokens = tokenize(source);
      const ast = new Parser(tokens).parse(), symbols = analyze(ast);
      return { ok: true, tokens, ast, symbols, code: generateCSharp(ast), diagnostics: [] };
    } catch (error) {
      if (!(error instanceof CompilerError)) throw error;
      return { ok: false, tokens, ast: null, symbols: [], code: "", diagnostics: [error.diagnostic] };
    }
  }

  function simulate(compilation) {
    const result = typeof compilation === "string" ? compile(compilation) : compilation;
    if (!result || !result.ok) return { ok: false, output: "", lines: [], steps: 0,
      diagnostics: result ? result.diagnostics : [{ phase: "Ejecución", message: "Compile una entrada válida.", line: 1, column: 1, code: "MSG_RUN001" }] };
    const variables = new Map(), lines = [];
    let outputChars = 0, outputLines = 0, steps = 0;
    try {
      for (const node of result.ast.body) {
        steps++;
        if (node.kind === "Declaration" || node.kind === "Assignment") variables.set(node.name, node.value);
        else {
          const text = node.parts.map(part => part.kind === "Text" ? part.value : variables.get(part.name)).join("");
          const newLines = text.split(/\r\n|\r|\n/).length;
          if (outputChars + text.length + 1 > LIMITS.outputChars || outputLines + newLines > LIMITS.outputLines)
            fail("Ejecución", "La vista simulada excede el límite de salida (500 líneas o 50000 caracteres).", node.token, "MSG_RUN002");
          lines.push(text); outputChars += text.length + 1; outputLines += newLines;
        }
      }
      return { ok: true, output: lines.join("\n") + (lines.length ? "\n" : ""), lines, steps, diagnostics: [] };
    } catch (error) {
      if (!(error instanceof CompilerError)) throw error;
      return { ok: false, output: lines.join("\n") + (lines.length ? "\n" : ""), lines, steps, diagnostics: [error.diagnostic] };
    }
  }
  return Object.freeze({ compile, tokenize, simulate, LIMITS, version: "1.0.0" });
});
