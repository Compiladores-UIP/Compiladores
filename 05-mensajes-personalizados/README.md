# Tema 05 · Compilador de Mensajes Personalizados

**Grupo 5:** Miguel Mes, Alonso Pinzón y Carlos Contreras.  
**Compiladores · Módulo 4 · UIP · Prof. Ricardo Wong.**

Este mini-compilador procesa un lenguaje propio para construir mensajes con datos de texto. Por ejemplo, permite escribir un saludo una sola vez y cambiar el nombre que aparece en él. La entrada se analiza, se valida y se transforma en un programa C#.

La interfaz está en la [página conjunta](../index.html#tema05), junto al tema 08. Abre `../index.html`; no necesitas instalar herramientas para probar la página.

## Un ejemplo completo

```text
INICIO
TEXTO nombre = "Alonso"
TEXTO curso = "Compiladores"
MENSAJE "Hola, {nombre}. Bienvenido a {curso}."
FIN
```

Salida esperada:

```text
Hola, Alonso. Bienvenido a Compiladores.
```

Fragmento del C# generado:

```csharp
string m_nombre = "Alonso";
string m_curso = "Compiladores";
Console.WriteLine(string.Concat(new string[] {
    "Hola, ", m_nombre, ". Bienvenido a ", m_curso, "."
}));
```

El archivo completo contiene `using System`, la clase `Program` y `Main`. El generador mantiene las variables y las concatenaciones; no sustituye todos los mensajes por textos calculados de antemano.

## Lenguaje admitido

| Forma | Función |
| --- | --- |
| `INICIO` y `FIN` | Inicio y cierre del programa, en líneas separadas. |
| `TEXTO nombre = "Alonso"` | Declara una variable de texto con valor inicial. |
| `nombre = "Miguel"` | Modifica una variable existente. |
| `MENSAJE "Hola, {nombre}"` | Imprime una plantilla con los valores actuales de sus marcadores. |
| `IMPRIMIR "Hola"` | Alias de `MENSAJE`. |
| `MENSAJE "{{nombre}}"` | Imprime `{nombre}` literalmente. |
| `// comentario` o `# comentario` | Comentario hasta el final de la línea. |

Las palabras reservadas no distinguen mayúsculas. Los nombres de variables sí: `nombre` y `Nombre` son distintos. Los nombres usan letras ASCII, números y `_`, sin empezar por un número ni coincidir con una palabra reservada. Las cadenas admiten Unicode y los escapes `\n`, `\r`, `\t`, `\"` y `\\`.

Se escribe una instrucción por línea, con `;` final opcional. Las variables deben declararse antes de utilizarse y no pueden repetirse. Solo se admiten valores de texto entre comillas dobles. Las llaves se interpretan exclusivamente en los mensajes: un valor declarado como `"{otra}"` se almacena literalmente.

## Fases implementadas

| Fase | Implementación |
| --- | --- |
| Léxico | Reconoce palabras reservadas, identificadores, cadenas, `=`, `;` y saltos de línea. Conserva lexemas y ubicación. |
| Sintáctico | Parser descendente que construye declaraciones, asignaciones y mensajes. Analiza las llaves dentro de cada plantilla. |
| Semántico | Comprueba que no haya declaraciones repetidas ni referencias a variables desconocidas. Construye una tabla de símbolos. |
| Representación intermedia | AST con fragmentos de texto y marcadores vinculados a variables. |
| Generación | Emite variables `string` y llamadas a `Console.WriteLine`, con concatenación cuando corresponde. Escapa las cadenas para C#. |
| Ejecución | La página simula el AST. Roslyn y .NET compilan y ejecutan el destino de manera externa. |

Los marcadores no se reconocen como tokens separados del lexer: forman parte del token `STRING` y luego se convierten en nodos `Placeholder` del AST. La simulación interpreta las instrucciones en orden para reflejar las reasignaciones.

## Terminal y ejecución real

Desde esta carpeta, con Node.js 20 o posterior:

```powershell
node .\src\cli.cjs .\examples\bienvenida.msg --out .\generated\Program.cs --report .\generated\reporte.json
node .\src\cli.cjs .\examples\equipo.msg --simulate
```

Para ejecutar el C# con el SDK de .NET 8 o posterior:

```powershell
dotnet new console -n DemoMensajes -o .\generated\DemoMensajes
node .\src\cli.cjs .\examples\bienvenida.msg --out .\generated\DemoMensajes\Program.cs
dotnet run --project .\generated\DemoMensajes
```

Omite el primer comando si ese proyecto ya existe. No se necesita `npm install`. La CLI devuelve `0` si la traducción es válida y `1` si falla. Un error de traducción no reemplaza los archivos de salida existentes.

## Pruebas y error controlado

```text
INICIO
MENSAJE "Hola, {nombre}."
FIN
```

Se rechaza porque `nombre` no fue declarado. El diagnóstico identifica la fase semántica y la ubicación del marcador: línea 2, columna 16. `MENSAJE` sin cadena produce un error sintáctico. Un carácter ajeno al lenguaje fuera de una cadena, como `@`, produce un error léxico.

```powershell
npm test
npm run test:dotnet
```

Las pruebas cubren personalización, reasignación, marcadores repetidos, llaves literales, escapes, Unicode, nombres de C#, errores, CLI y límites. Las pruebas con .NET comprueban compilación y salida real; se omiten si no hay SDK disponible.

## API y organización

```javascript
const { compile, simulate } = require("./src/compiler.js");
const result = compile('INICIO\nTEXTO n = "UIP"\nMENSAJE "Hola, {n}"\nFIN');
if (result.ok) console.log(simulate(result).output);
```

En el navegador el núcleo se expone como `window.MessageCompiler`. `compile()` devuelve `ok`, `code`, `tokens`, `ast`, `symbols` y `diagnostics`. Los directorios `src/`, `examples/` y `tests/` contienen el núcleo/CLI, las entradas de ejemplo y las verificaciones.

## Límites

No implementa números, operadores, decisiones, ciclos, funciones ni entrada por teclado. Reporta el primer error y no aplica optimizaciones. La fuente se limita a 50 000 caracteres y 20 000 tokens; la simulación a 500 líneas o 50 000 caracteres de salida, incluidos los saltos dentro de una cadena. Estos límites de salida pertenecen al simulador y no se insertan en el C#.

La simulación no ejecuta C# y no necesita `eval`. Consulta el [README del grupo](../README.md) para fuentes, evidencias e integración.
