"use strict";

// Pruebas de integración reales, sin paquetes NuGet ni conexión a Internet.
// Se invoca Roslyn incluido en el SDK y luego el host de .NET.
const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawnSync } = require("node:child_process");
const { compile, simulate } = require("../src/compiler.js");

const host = process.env.PC_DOTNET || "dotnet";
function compareVersions(a, b) {
  const aa = a.split(/[.-]/).map(Number), bb = b.split(/[.-]/).map(Number);
  for (let i = 0; i < 3; i++) {
    const delta = (aa[i] || 0) - (bb[i] || 0);
    if (delta) return delta;
  }
  return 0;
}
function findToolchain() {
  const sdkList = spawnSync(host, ["--list-sdks"], { encoding: "utf8", timeout: 15000 });
  if (sdkList.error && sdkList.error.code === "ENOENT") return { skip: "No se encontró dotnet. Instale el SDK para ejecutar estas pruebas reales." };
  assert.equal(sdkList.status, 0, sdkList.stderr || String(sdkList.error));
  const sdks = [...sdkList.stdout.matchAll(/^(\S+) \[(.+)\]\s*$/gm)]
    .map(match => ({ version: match[1], root: match[2] }))
    .filter(sdk => parseInt(sdk.version, 10) >= 8)
    .sort((a, b) => compareVersions(a.version, b.version));
  if (!sdks.length) return { skip: "Se necesita un SDK de .NET 8 o posterior; el runtime por sí solo no contiene Roslyn." };
  const sdk = sdks.at(-1), root = path.dirname(sdk.root);
  const packRoot = path.join(root, "packs", "Microsoft.NETCore.App.Ref");
  assert.ok(fs.existsSync(packRoot), "Falta el paquete de referencias incluido en el SDK.");
  const packs = fs.readdirSync(packRoot).filter(version => /^\d+\.\d+\.\d+$/.test(version))
    .filter(version => fs.existsSync(path.join(root, "shared", "Microsoft.NETCore.App", version)))
    .sort(compareVersions);
  assert.ok(packs.length, "Se necesita un runtime y un paquete de referencias de la misma versión.");
  const version = packs.at(-1), tfm = `net${parseInt(version, 10)}.0`;
  const references = path.join(packRoot, version, "ref", tfm);
  const csc = path.join(sdk.root, sdk.version, "Roslyn", "bincore", "csc.dll");
  assert.ok(fs.existsSync(csc), "No se encontró csc.dll en el SDK.");
  assert.ok(fs.existsSync(references), "No se encontraron las referencias del framework.");
  return { sdk: sdk.version, version, tfm, csc, references };
}
const toolchain = findToolchain();
const program = body => `INICIO\n${body}\nFIN`;
const cases = [
  ["hola", null, "Hola Mundo UIP\n"],
  ["positivo", null, "Positivo\n"],
  ["operaciones", null, "Resultado: 18\nPromedio: 7\nDivision entera: 2\n"],
  ["ciclos", null, "Suma: 15\n3\n2\n1\n"],
  ["logicos", null, "Aprobado\nTrue\n"],
  ["division-entera", program('IMPRIMIR -7 / 2\nIMPRIMIR -7 % 2\nIMPRIMIR 7 / 2.0'), "-3\n-1\n3.5\n"],
  ["cadenas-escapadas", program('IMPRIMIR "Hola \\"UIP\\" \\\\ ruta"'), 'Hola "UIP" \\ ruta\n'],
  ["nombres-reservados", program('ENTERO class = 5\nENTERO Console = 6\nIMPRIMIR class + Console'), "11\n"],
  ["ambito-y-decision", program('ENTERO n = -1\nSI n > 0\nIMPRIMIR "positivo"\nSINO\nENTERO local = n + 1\nSI local == 0\nIMPRIMIR "cero"\nFIN_SI\nFIN_SI'), "cero\n"],
  ["paso-descendente", program('PARA i = 5 HASTA 1 PASO -2\nIMPRIMIR i\nFIN_PARA'), "5\n3\n1\n"],
  ["limites-int32", program('PARA i = 2147483647 HASTA 2147483647\nIMPRIMIR i\nFIN_PARA\nPARA j = -2147483648 HASTA -2147483648 PASO -1\nIMPRIMIR j\nFIN_PARA'), "2147483647\n-2147483648\n"],
  ["ciclos-anidados", program('PARA i = 1 HASTA 2\nPARA j = 1 HASTA 2\nIMPRIMIR i, j\nFIN_PARA\nFIN_PARA'), "11\n12\n21\n22\n"],
  ["limite-evaluado-una-vez", program('ENTERO limite = 3\nPARA i = 1 HASTA limite\nIMPRIMIR i\nlimite = 1\nFIN_PARA'), "1\n2\n3\n"],
  ["cortocircuito", program('ENTERO cero = 0\nIMPRIMIR VERDADERO O (1 / cero > 0)\nIMPRIMIR FALSO Y (1 / cero > 0)'), "True\nFalse\n"],
  ["promocion-numerica", program('DECIMAL valor = 5\nvalor = valor / 2\nIMPRIMIR valor\nTEXTO saludo = "Hola " + "UIP"\nIMPRIMIR saludo'), "2.5\nHola UIP\n"],
  ["unicode-y-comentarios", program('# ejemplo\nIMPRIMIR "Panamá — compilación" // fin'), "Panamá — compilación\n"],
  ["programa-vacio", program(''), ""]
];

for (const [name, source, expected] of cases) {
  test(`C# real con .NET: ${name}`, { skip: toolchain.skip || false }, t => {
    t.diagnostic(`SDK ${toolchain.sdk}; runtime ${toolchain.version}; Roslyn del SDK.`);
    const input = source === null ? fs.readFileSync(path.join(__dirname, `../examples/${name}.pseudo`), "utf8") : source;
    const result = compile(input);
    assert.equal(result.ok, true, JSON.stringify(result.diagnostics));
    const temporary = fs.mkdtempSync(path.join(os.tmpdir(), "pseudo-csharp-dotnet-"));
    const cs = path.join(temporary, "Program.cs"), dll = path.join(temporary, "Program.dll");
    fs.writeFileSync(cs, result.code);
    fs.writeFileSync(path.join(temporary, "Program.runtimeconfig.json"), JSON.stringify({
      runtimeOptions: { tfm: toolchain.tfm, framework: { name: "Microsoft.NETCore.App", version: toolchain.version } }
    }));
    const references = fs.readdirSync(toolchain.references).filter(file => file.endsWith(".dll"))
      .map(file => `-r:${path.join(toolchain.references, file)}`);
    const build = spawnSync(host, [toolchain.csc, "-nologo", "-noconfig", "-nostdlib+", "-target:exe", `-out:${dll}`, ...references, cs], {
      encoding: "utf8", timeout: 20000, maxBuffer: 2 * 1024 * 1024
    });
    assert.equal(build.status, 0, `${build.stdout}\n${build.stderr}\n${build.error || ""}`);
    const execution = spawnSync(host, [dll], { encoding: "utf8", timeout: 5000, maxBuffer: 1024 * 1024 });
    assert.equal(execution.status, 0, execution.stderr || String(execution.error));
    const output = execution.stdout.replace(/\r\n/g, "\n");
    assert.equal(output, expected);
    assert.equal(simulate(result).output, output, "La simulación y la ejecución deben coincidir en estos casos controlados.");
    t.diagnostic(`Salida real de .NET: ${JSON.stringify(output)}`);
  });
}
