"use strict";
const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const { compile, tokenize, simulate, LIMITS } = require("../src/compiler.js");

const program = body => `INICIO\n${body}\nFIN`;
function valid(source, output) {
  const result = compile(source);
  assert.equal(result.ok, true, JSON.stringify(result.diagnostics));
  assert.match(result.code, /class Program/);
  if (output !== undefined) {
    const preview = simulate(result);
    assert.equal(preview.ok, true, JSON.stringify(preview.diagnostics));
    assert.equal(preview.output, output);
  }
  return result;
}
function invalid(source, phase, fragment) {
  const result = compile(source);
  assert.equal(result.ok, false, source);
  assert.equal(result.code, "");
  assert.equal(result.diagnostics[0].phase, phase);
  if (fragment) assert.match(result.diagnostics[0].message, fragment);
  assert.ok(result.diagnostics[0].line >= 1);
  assert.ok(result.diagnostics[0].column >= 1);
  return result.diagnostics[0];
}

const examples = [
  ["hola", "Hola Mundo UIP\n"], ["positivo", "Positivo\n"],
  ["operaciones", "Resultado: 18\nPromedio: 7\nDivision entera: 2\n"],
  ["ciclos", "Suma: 15\n3\n2\n1\n"], ["logicos", "Aprobado\nTrue\n"]
];
for (const [name, output] of examples) test(`Ejemplo válido: ${name}`, () => {
  valid(fs.readFileSync(path.join(__dirname, `../examples/${name}.pseudo`), "utf8"), output);
});
for (const [name, phase] of [["error-lexico", "Léxico"], ["error-sintactico", "Sintáctico"], ["error-semantico", "Semántico"]]) {
  test(`Ejemplo inválido: ${name}`, () => invalid(fs.readFileSync(path.join(__dirname, `../examples/${name}.pseudo`), "utf8"), phase));
}
test("Programa vacío válido", () => valid(program(""), ""));
test("CRLF, BOM, líneas vacías y comentarios", () => valid('\uFEFF\r\n# portada\r\ninicio\r\n// comentario\r\nIMPRIMIR "ok"; // nota\r\nfin\r\n', "ok\n"));
test("Palabras reservadas no distinguen mayúsculas", () => valid(program('entero numero = 3\nescribir numero'), "3\n"));
test("Identificadores distinguen mayúsculas", () => valid(program('ENTERO a = 1\nENTERO A = 2\nIMPRIMIR a, A'), "12\n"));
test("Ubicación precisa de tokens", () => {
  const token = tokenize('INICIO\n  ENTERO numero = 5\nFIN').find(item => item.type === "IDENTIFIER");
  assert.equal(token.line, 2); assert.equal(token.column, 10); assert.equal(token.raw, "numero");
});
test("Cadena y escapes", () => valid(program('IMPRIMIR "Hola \\\"UIP\\\" \\\\ ruta\\tfin"'), 'Hola "UIP" \\ ruta\tfin\n'));
test("No se traducen palabras dentro de cadenas", () => {
  const result = valid(program('IMPRIMIR "SI Y SINO FIN"'), "SI Y SINO FIN\n");
  assert.match(result.code, /"SI Y SINO FIN"/);
});
test("Escape de caracteres de separación de Unicode en C#", () => {
  const result = valid(program('IMPRIMIR "a\u2028b"'), 'a\u2028b\n');
  assert.ok(result.code.includes('"a\\u2028b"'));
});
test("Identificadores reservados de C# se renombran", () => {
  const result = valid(program('ENTERO class = 5\nENTERO Console = 6\nIMPRIMIR class + Console'), "11\n");
  assert.match(result.code, /int v_class/); assert.match(result.code, /int v_Console/);
});
test("Prioridad aritmética", () => valid(program('IMPRIMIR 2 + 3 * 4\nIMPRIMIR (2 + 3) * 4'), "14\n20\n"));
test("Asociatividad izquierda", () => valid(program('IMPRIMIR 20 - 5 - 3\nIMPRIMIR 20 / 2 / 2'), "12\n5\n"));
test("Asignación <- y =", () => valid(program('ENTERO n <- 2\nn = n + 3\nIMPRIMIR n'), "5\n"));
test("División entera y real", () => valid(program('IMPRIMIR -7 / 2\nIMPRIMIR 7 / 2.0'), "-3\n3.5\n"));
test("Residuo", () => valid(program('IMPRIMIR -7 % 2'), "-1\n"));
test("Promoción ENTERO a DECIMAL", () => valid(program('DECIMAL n = 4\nn = n / 2\nIMPRIMIR n'), "2\n"));
test("Concatenación explícita de texto", () => valid(program('TEXTO mensaje = "Hola " + "UIP"\nIMPRIMIR mensaje'), "Hola UIP\n"));
test("Lógica, prioridades y cortocircuito", () => valid(program('ENTERO cero = 0\nLOGICO a = VERDADERO O (1 / cero > 0)\nIMPRIMIR a\nIMPRIMIR FALSO Y VERDADERO O NO FALSO'), "True\nTrue\n"));
test("Operadores alternativos != y <>", () => valid(program('IMPRIMIR 1 <> 2\nIMPRIMIR 1 != 1'), "True\nFalse\n"));
test("SI, SINO y anidación", () => valid(program('ENTERO n = -1\nSI n > 0\nIMPRIMIR "positivo"\nSINO\nSI n == 0\nIMPRIMIR "cero"\nSINO\nIMPRIMIR "negativo"\nFIN_SI\nFIN_SI'), "negativo\n"));
test("SI sin SINO", () => valid(program('SI FALSO\nIMPRIMIR "no"\nFIN_SI\nIMPRIMIR "fin"'), "fin\n"));
test("PARA descendente y paso", () => valid(program('PARA i = 5 HASTA 1 PASO -2\nIMPRIMIR i\nFIN_PARA'), "5\n3\n1\n"));
test("PARA sin iteraciones", () => valid(program('PARA i = 3 HASTA 1\nIMPRIMIR i\nFIN_PARA'), ""));
test("Límites de PARA se calculan una vez", () => valid(program('ENTERO limite = 3\nPARA i = 1 HASTA limite\nIMPRIMIR i\nlimite = 1\nFIN_PARA'), "1\n2\n3\n"));
test("Ciclos anidados y temporales distintos", () => {
  const result = valid(program('PARA i = 1 HASTA 2\nPARA j = 1 HASTA 2\nIMPRIMIR i, j\nFIN_PARA\nFIN_PARA'), "11\n12\n21\n22\n");
  assert.match(result.code, /__pc_i1/); assert.match(result.code, /__pc_i2/);
});
test("PARA en los extremos int no se desborda", () => valid(program('PARA i = 2147483647 HASTA 2147483647\nIMPRIMIR i\nFIN_PARA\nPARA j = -2147483648 HASTA -2147483648 PASO -1\nIMPRIMIR j\nFIN_PARA'), "2147483647\n-2147483648\n"));
test("MIENTRAS falso no ejecuta el cuerpo", () => valid(program('MIENTRAS FALSO\nIMPRIMIR "no"\nFIN_MIENTRAS'), ""));
test("Alcance de variables en bloques", () => valid(program('ENTERO n = 2\nSI VERDADERO\nENTERO local = n + 1\nIMPRIMIR local\nFIN_SI\nIMPRIMIR n'), "3\n2\n"));
test("Tabla de símbolos informa tipo y nombre destino", () => {
  const result = valid(program('ENTERO numero = 5\nIMPRIMIR numero'));
  assert.equal(result.symbols[0].name, "numero"); assert.equal(result.symbols[0].type, "ENTERO");
  assert.equal(result.symbols[0].csharpName, "v_numero");
});

const failures = [
  ["Sin INICIO", 'IMPRIMIR "x"\nFIN', "Sintáctico", /INICIO/],
  ["Sin FIN", 'INICIO\nIMPRIMIR "x"', "Sintáctico", /FIN/],
  ["Después de FIN", program('IMPRIMIR "x"') + '\nIMPRIMIR "y"', "Sintáctico", /después de FIN/],
  ["IMPRIMIR sin valor", program('IMPRIMIR'), "Sintáctico", /expresión/],
  ["Coma sin valor", program('IMPRIMIR "x",'), "Sintáctico", /expresión/],
  ["Cadena sin cierre", program('IMPRIMIR "x'), "Léxico", /cadena/],
  ["Escape inválido", program('IMPRIMIR "\\q"'), "Léxico", /escape/],
  ["Carácter desconocido", program('IMPRIMIR @'), "Léxico", /Carácter/],
  ["Número mal formado", program('DECIMAL x = 2.'), "Léxico", /dígito/],
  ["Paréntesis incompleto", program('IMPRIMIR (2 + 3'), "Sintáctico", /Falta \)/],
  ["Falta FIN_SI", program('SI VERDADERO\nIMPRIMIR 1'), "Sintáctico", /FIN_SI/],
  ["Cierre equivocado", program('MIENTRAS VERDADERO\nFIN_PARA'), "Sintáctico", /FIN_MIENTRAS/],
  ["SINO aislado", program('SINO'), "Sintáctico", /FIN/],
  ["Dos instrucciones en una línea", program('ENTERO x = 1 IMPRIMIR x'), "Sintáctico", /fin de la línea/],
  ["Sin inicialización", program('ENTERO x'), "Sintáctico", /valor inicial/],
  ["Declaración reservada", program('ENTERO SI = 1'), "Sintáctico", /nombre/],
  ["Variable desconocida", program('IMPRIMIR x'), "Semántico", /no está declarada/],
  ["Autorreferencia", program('ENTERO x = x + 1'), "Semántico", /no está declarada/],
  ["Variable duplicada", program('ENTERO x = 1\nENTERO x = 2'), "Semántico", /ya fue declarado/],
  ["Sombra prohibida", program('ENTERO x = 1\nSI VERDADERO\nENTERO x = 2\nFIN_SI'), "Semántico", /nombres únicos/],
  ["Variable fuera del bloque", program('SI VERDADERO\nENTERO x = 1\nFIN_SI\nIMPRIMIR x'), "Semántico", /visible/],
  ["Tipo incompatible", program('ENTERO n = "texto"'), "Semántico", /asignar TEXTO a ENTERO/],
  ["Real a entero", program('ENTERO n = 2.5'), "Semántico", /asignar DECIMAL a ENTERO/],
  ["Asignación inválida", program('ENTERO n = 1\nn = "hola"'), "Semántico", /asignar/],
  ["Condición no booleana", program('SI 2\nIMPRIMIR 1\nFIN_SI'), "Semántico", /LOGICO/],
  ["Lógica con números", program('IMPRIMIR 1 Y 2'), "Semántico", /LOGICO/],
  ["NO con número", program('IMPRIMIR NO 2'), "Semántico", /LOGICO/],
  ["Texto y número con +", program('IMPRIMIR "n=" + 2'), "Semántico", /aritmético/],
  ["Comparación incompatible", program('IMPRIMIR "a" == 2'), "Semántico", /incompatibles/],
  ["Orden de texto", program('IMPRIMIR "a" > "b"'), "Semántico", /números/],
  ["Paso cero", program('PARA i = 1 HASTA 3 PASO 0\nFIN_PARA'), "Semántico", /distinto de cero/],
  ["Paso decimal", program('PARA i = 1 HASTA 3 PASO 0.5\nFIN_PARA'), "Sintáctico", /entero/],
  ["Paso variable", program('PARA i = 1 HASTA 3 PASO n\nFIN_PARA'), "Sintáctico", /literal/],
  ["Límite real", program('PARA i = 1 HASTA 3.5\nFIN_PARA'), "Semántico", /ENTERO/],
  ["Modificar contador", program('PARA i = 1 HASTA 3\ni = i + 1\nFIN_PARA'), "Semántico", /contador/],
  ["Contador fuera del ciclo", program('PARA i = 1 HASTA 3\nFIN_PARA\nIMPRIMIR i'), "Semántico", /visible/],
  ["Literal entero fuera de rango", program('ENTERO n = 2147483648'), "Semántico", /rango/],
  ["Desbordamiento constante", program('ENTERO n = 2147483647 + 1'), "Semántico", /Desbordamiento/],
  ["División constante entre cero", program('IMPRIMIR 1 / 0'), "Semántico", /cero/],
  ["Doble negación del mínimo int", program('IMPRIMIR -(-2147483648)'), "Semántico", /Desbordamiento/]
];
for (const [name, source, phase, fragment] of failures) test(name, () => invalid(source, phase, fragment));
test("Error con posición exacta", () => {
  const d = invalid('INICIO\n  IMPRIMIR\nFIN', "Sintáctico");
  assert.equal(d.line, 2); assert.equal(d.column, 11);
});
test("Entrada demasiado extensa devuelve diagnóstico", () => invalid("x".repeat(LIMITS.sourceChars + 1), "Léxico", /caracteres/));
test("Tipo de entrada inválido", () => invalid(null, "Léxico", /texto/));
test("Bloques excesivos", () => invalid(program('SI VERDADERO\n'.repeat(70) + 'FIN_SI\n'.repeat(70)), "Sintáctico", /anidados/));
test("Paréntesis excesivos", () => invalid(program('IMPRIMIR ' + '('.repeat(70) + '1' + ')'.repeat(70)), "Sintáctico", /anidada/));
test("Expresión binaria excesiva", () => invalid(program('IMPRIMIR ' + Array(200).fill('1').join(' + ')), "Sintáctico", /compleja/));
test("Simulación limita ciclos infinitos", () => {
  const result = valid(program('MIENTRAS VERDADERO\nFIN_MIENTRAS'));
  const preview = simulate(result, { maxSteps: 30 });
  assert.equal(preview.ok, false); assert.equal(preview.diagnostics[0].code, "RUN002");
});
test("Simulación controla volumen de salida", () => {
  const result = valid(program('PARA i = 1 HASTA 600\nIMPRIMIR i\nFIN_PARA'));
  const preview = simulate(result);
  assert.equal(preview.ok, false); assert.equal(preview.diagnostics[0].code, "RUN005");
});
test("Error dinámico de división entera", () => {
  const preview = simulate(valid(program('ENTERO n = 0\nIMPRIMIR 1 / n')));
  assert.equal(preview.ok, false); assert.equal(preview.diagnostics[0].code, "RUN004");
});
test("Desbordamiento dinámico", () => {
  const preview = simulate(valid(program('ENTERO n = 2147483647\nn = n + 1')));
  assert.equal(preview.ok, false); assert.equal(preview.diagnostics[0].code, "RUN003");
});
test("No se simula código inválido", () => assert.equal(simulate(program('IMPRIMIR')).ok, false));
test("Módulo disponible sin Node en el navegador", () => {
  const context = vm.createContext({});
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/compiler.js'), 'utf8'), context);
  assert.equal(context.PseudoCSharp.compile(program('IMPRIMIR "web"')).ok, true);
});
test("Entradas inyectadas no ejecutan JavaScript ni C#", () => {
  const source = program('IMPRIMIR "<script>alert(1)</script>"');
  valid(source, "<script>alert(1)</script>\n");
  invalid(program('System.Console.WriteLine("intruso")'), "Léxico");
});
