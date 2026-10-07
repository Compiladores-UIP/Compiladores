"use strict";
const test = require("node:test"), assert = require("node:assert/strict");
const fs = require("node:fs"), path = require("node:path");
const { compile, simulate, tokenize, LIMITS } = require("../src/compiler.js");
const program = body => `INICIO\n${body}\nFIN`;
function output(body, expected) {
  const r = compile(program(body)); assert.equal(r.ok, true, JSON.stringify(r.diagnostics));
  const s = simulate(r); assert.equal(s.ok, true, JSON.stringify(s.diagnostics)); assert.equal(s.output, expected); return r;
}
function error(source, phase, message) {
  const r = compile(source); assert.equal(r.ok, false); assert.equal(r.code, "");
  assert.equal(r.diagnostics[0].phase, phase); assert.match(r.diagnostics[0].message, message); return r.diagnostics[0];
}
const samples = { bienvenida: "Hola, Alonso. Bienvenido a Compiladores.\nSomos el grupo 5 de la UIP.\n",
  equipo: "Hola, Miguel.\nHola, Alonso.\nHola, Carlos.\n",
  escapes: 'Desde Panamá: "Hola UIP"\nUna llave literal: {nombre}\nPrimera línea\nSegunda línea\n' };
for (const [name, expected] of Object.entries(samples)) test(`Mensajes: ejemplo ${name}`, () => {
  const r = compile(fs.readFileSync(path.join(__dirname, `../examples/${name}.msg`), "utf8"));
  assert.equal(r.ok, true); assert.equal(simulate(r).output, expected);
});
test("Mensajes: IMPRIMIR y mensaje sin marcadores", () => output('IMPRIMIR "Hola Mundo"', "Hola Mundo\n"));
test("Mensajes: actualiza los valores en el orden del programa", () => output('TEXTO n = "Uno"\nMENSAJE "{n}"\nn = "Dos"\nMENSAJE "{n}"', "Uno\nDos\n"));
test("Mensajes: dos marcadores y un marcador repetido", () => output('TEXTO a = "A"\nTEXTO b = "B"\nMENSAJE "{a}{b}{a}"', "ABA\n"));
test("Mensajes: el valor de una variable es texto literal", () => output('TEXTO dato = "{otra}"\nMENSAJE "{dato}"', "{otra}\n"));
test("Mensajes: llaves literales y marcador adyacente", () => output('TEXTO n = "UIP"\nMENSAJE "{{{n}}}"', "{UIP}\n"));
test("Mensajes: cadena vacía y programa vacío", () => {
  output('MENSAJE ""', "\n"); output("", "");
});
test("Mensajes: Unicode y nombres reservados de C#", () => {
  const r = output('TEXTO class = "Panamá 🐇"\nTEXTO Console = "UIP"\nMENSAJE "{class} — {Console}"', "Panamá 🐇 — UIP\n");
  assert.match(r.code, /string m_class/); assert.match(r.code, /m_Console/);
});
test("Mensajes: comillas y barra inversa se escapan en C#", () => {
  const r = output('MENSAJE "Hola \\"UIP\\" \\\\ ruta"', 'Hola "UIP" \\ ruta\n');
  assert.match(r.code, /\\"UIP\\"/); assert.match(r.code, /\\\\ ruta/);
});
test("Mensajes: conserva caracteres de control mediante escapes C#", () => {
  const r = output('MENSAJE "A\u0000B\u2028C"', "A\u0000B\u2028C\n");
  assert.ok(r.code.includes("\\u0000")); assert.ok(r.code.includes("\\u2028"));
});
test("Mensajes: acepta BOM, CRLF, comentarios y punto y coma", () => {
  const r = compile('\uFEFFinicio;\r\n# comentario\r\ntexto n = "Hola"; // fin\r\nmensaje "{n}";\r\nfin;');
  assert.equal(r.ok, true); assert.equal(simulate(r).output, "Hola\n");
});
test("Mensajes: los identificadores distinguen mayúsculas", () => error(program('TEXTO n = "Hola"\nMENSAJE "{N}"'), "Semántico", /N no está declarada/));
test("Mensajes: variable desconocida y ubicación exacta del marcador", () => {
  const d = error(program('MENSAJE "Hola, {nombre}."'), "Semántico", /nombre/);
  assert.equal(d.line, 2); assert.equal(d.column, 16);
});
test("Mensajes: asignación a una variable no declarada", () => error(program('n = "Nuevo"'), "Semántico", /no está declarada/));
test("Mensajes: rechaza declaraciones duplicadas", () => error(program('TEXTO n = "A"\nTEXTO n = "B"'), "Semántico", /ya fue declarada/));
test("Mensajes: rechaza usar un marcador antes de declarar", () => error(program('MENSAJE "{n}"\nTEXTO n = "Después"'), "Semántico", /antes del mensaje/));
test("Mensajes: los nombres reservados del lenguaje no son identificadores", () => error(program('TEXTO FIN = "A"'), "Sintáctico", /nombre de la variable/));
test("Mensajes: MENSAJE requiere una cadena", () => error(program("MENSAJE"), "Sintáctico", /cadena después/));
test("Mensajes: rechaza un marcador vacío o con espacios", () => {
  error(program('MENSAJE "{}"'), "Sintáctico", /identificador/);
  error(program('MENSAJE "{no valido}"'), "Sintáctico", /identificador/);
});
test("Mensajes: rechaza llaves sin cierre o apertura", () => {
  error(program('MENSAJE "{n"'), "Sintáctico", /Falta }/);
  error(program('MENSAJE "n}"'), "Sintáctico", /Llave de cierre/);
});
test("Mensajes: rechaza comilla sin cierre", () => error(program('MENSAJE "Hola'), "Léxico", /cadena|comilla/));
test("Mensajes: rechaza escapes desconocidos y barra final", () => {
  error(program('MENSAJE "\\q"'), "Léxico", /escape/);
  error('INICIO\nMENSAJE "Hola\\', "Léxico", /comilla/);
});
test("Mensajes: rechaza caracteres ajenos al lenguaje", () => error(program('MENSAJE "Hola" @'), "Léxico", /Carácter/));
test("Mensajes: no admite instrucciones sin sus delimitadores", () => {
  error('MENSAJE "Hola"', "Sintáctico", /INICIO/);
  error('INICIO\nMENSAJE "Hola"', "Sintáctico", /Falta FIN/);
  error('INICIO\nFIN\nMENSAJE "Hola"', "Sintáctico", /después de FIN/);
});
test("Mensajes: requiere una instrucción por línea", () => error(program('MENSAJE "A"; MENSAJE "B"'), "Sintáctico", /por línea/));
test("Mensajes: límites de fuente y tokens", () => {
  error(" ".repeat(LIMITS.sourceChars + 1), "Léxico", /50000/);
  error("=;\n".repeat(7000), "Léxico", /tokens/);
  error(null, "Léxico", /texto/);
});
test("Mensajes: límite de salida cuenta los saltos dentro de cadenas", () => {
  const r = compile(program('MENSAJE "' + '\\n'.repeat(500) + '"'));
  assert.equal(r.ok, true); const s = simulate(r);
  assert.equal(s.ok, false); assert.equal(s.diagnostics[0].phase, "Ejecución");
});
test("Mensajes: límite de salida con un marcador repetido", () => {
  const r = compile(program('TEXTO x = "' + 'a'.repeat(1000) + '"\nMENSAJE "' + '{x}'.repeat(51) + '"'));
  assert.equal(r.ok, true); assert.equal(simulate(r).ok, false);
});
test("Mensajes: tokens y símbolos conservan la información académica", () => {
  const r = compile(program('TEXTO nombre = "Alonso"\nMENSAJE "{nombre}"'));
  assert.equal(r.symbols[0].name, "nombre"); assert.equal(r.symbols[0].type, "TEXTO");
  assert.equal(r.ast.body[1].parts[0].kind, "Placeholder");
  assert.equal(tokenize('MENSAJE "Hola"')[1].raw, '"Hola"');
});
test("Mensajes: no simula una traducción inválida", () => { assert.equal(simulate("incorrecto").ok, false); assert.equal(simulate(null).ok, false); });
