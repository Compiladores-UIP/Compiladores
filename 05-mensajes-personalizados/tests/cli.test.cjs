"use strict";
const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const os = require("node:os");
const { spawnSync } = require("node:child_process");
const cli = path.join(__dirname, "../src/cli.cjs");
const example = name => path.join(__dirname, `../examples/${name}.msg`);
function run(args) { return spawnSync(process.execPath, [cli, ...args], { encoding: "utf8" }); }
test("CLI ofrece ayuda", () => {
  const result = run(["--help"]); assert.equal(result.status, 0); assert.match(result.stdout, /Uso:/);
});
test("CLI genera C# en stdout", () => {
  const result = run([example("bienvenida")]); assert.equal(result.status, 0); assert.match(result.stdout, /m_nombre/);
});
test("CLI guarda código y reporte", () => {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "pc-cli-"));
  const output = path.join(dir, "Program.cs"), report = path.join(dir, "reporte.json");
  const result = run([example("bienvenida"), "--out", output, "--report", report]);
  assert.equal(result.status, 0); assert.match(fs.readFileSync(output, "utf8"), /class Program/);
  assert.equal(JSON.parse(fs.readFileSync(report, "utf8")).ok, true);
});
test("CLI no reemplaza salidas existentes al fallar", () => {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "pc-cli-"));
  const output = path.join(dir, "Program.cs"); fs.writeFileSync(output, "original");
  const result = run([example("error-sintactico"), "--out", output]);
  assert.equal(result.status, 1); assert.equal(fs.readFileSync(output, "utf8"), "original");
  assert.match(result.stderr, /línea 2/); assert.match(result.stderr, /sintáctico/);
});
test("CLI rechaza sobrescribir la entrada", () => {
  const result = run([example("bienvenida"), "--out", example("bienvenida")]);
  assert.equal(result.status, 1); assert.match(result.stderr, /archivo fuente/);
});
test("CLI necesita una ruta para --out", () => {
  const result = run([example("bienvenida"), "--out"]); assert.equal(result.status, 1); assert.match(result.stderr, /ruta/);
});
test("CLI reporta archivo inexistente", () => {
  const result = run(["archivo-inexistente.msg"]); assert.equal(result.status, 1); assert.match(result.stderr, /ENOENT/);
});
test("CLI identifica la simulación", () => {
  const result = run([example("bienvenida"), "--simulate"]);
  assert.equal(result.status, 0); assert.equal(result.stdout, "Hola, Alonso. Bienvenido a Compiladores.\nSomos el grupo 5 de la UIP.\n"); assert.match(result.stderr, /no es ejecución de C#/);
});
test("CLI comunica error semántico", () => {
  const result = run([example("error-semantico")]); assert.equal(result.status, 1); assert.match(result.stderr, /semántico/);
});
test("CLI rechaza compartir ruta entre reporte y código", () => {
  const result = run([example("bienvenida"), "--out", "same", "--report", "same"]);
  assert.equal(result.status, 1); assert.match(result.stderr, /rutas diferentes/);
});
