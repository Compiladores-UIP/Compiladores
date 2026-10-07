#!/usr/bin/env node
"use strict";

const fs = require("node:fs");
const path = require("node:path");
const compiler = require("./compiler.js");

const HELP = `Compilador de mensajes personalizados a C# (grupo 5, tema 05)

Uso:
  node src/cli.cjs archivo.msg [--out Program.cs] [--report reporte.json]
  node src/cli.cjs archivo.msg --simulate
  node src/cli.cjs --help

Sin --out, escribe el C# generado en la salida estándar.
--report guarda tokens, AST, tabla de símbolos y diagnósticos.
--simulate interpreta el AST de forma limitada; NO ejecuta C#.
Los errores de compilación no crean ni reemplazan archivos de salida.
`;

function main(args) {
  if (args.includes("--help") || args.includes("-h")) { process.stdout.write(HELP); return 0; }
  let input, output, report, preview = false;
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    if (arg === "--out" || arg === "--report") {
      if (!args[i + 1] || args[i + 1].startsWith("--")) throw new Error(`${arg} necesita una ruta.`);
      if (arg === "--out") output = args[++i]; else report = args[++i];
    } else if (arg === "--simulate") preview = true;
    else if (arg.startsWith("-")) throw new Error(`Opción desconocida: ${arg}.`);
    else if (!input) input = arg;
    else throw new Error("Solo se admite un archivo de entrada.");
  }
  if (!input) throw new Error("Falta el archivo de entrada. Use --help para ver el formato.");
  const inputPath = path.resolve(input);
  if ([output, report].some(file => file && path.resolve(file) === inputPath)) throw new Error("La salida no puede reemplazar el archivo fuente.");
  if (output && report && path.resolve(output) === path.resolve(report)) throw new Error("El C# y el reporte necesitan rutas diferentes.");
  const result = compiler.compile(fs.readFileSync(inputPath, "utf8"));
  if (!result.ok) {
    for (const d of result.diagnostics) process.stderr.write(`Error ${d.phase.toLowerCase()} [${d.code}] en línea ${d.line}, columna ${d.column}: ${d.message}\n`);
    return 1;
  }
  function save(filename, content) {
    fs.mkdirSync(path.dirname(path.resolve(filename)), { recursive: true });
    fs.writeFileSync(filename, content, "utf8");
  }
  if (output) { save(output, result.code); process.stderr.write(`Código C# generado: ${output}\n`); }
  else if (!preview) process.stdout.write(result.code);
  if (report) { save(report, JSON.stringify(result, null, 2) + "\n"); process.stderr.write(`Reporte generado: ${report}\n`); }
  if (preview) {
    process.stderr.write("Vista previa simulada del AST; no es ejecución de C#.\n");
    const simulation = compiler.simulate(result);
    process.stdout.write(simulation.output);
    if (!simulation.ok) {
      const d = simulation.diagnostics[0];
      process.stderr.write(`Error de simulación [${d.code}] en línea ${d.line}, columna ${d.column}: ${d.message}\n`);
      return 1;
    }
  }
  return 0;
}

try { process.exitCode = main(process.argv.slice(2)); }
catch (error) { process.stderr.write(`Error: ${error.message}\n`); process.exitCode = 1; }
