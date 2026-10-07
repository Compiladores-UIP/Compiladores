# Tema 08 · Compilador de Pseudocódigo a C#

Taller colaborativo de Compiladores, módulo 4 · Universidad Interamericana de Panamá.

**Grupo 5:** Miguel Mes, Alonso Pinzón y Carlos Contreras.  
**Docente:** Ricardo Wong.

Este proyecto traduce un subconjunto de pseudocódigo en español a código fuente C#. La idea es mostrar el recorrido de un programa: primero se reconocen sus tokens, después se revisa su estructura, se validan los tipos y finalmente se genera el código.

Es un **compilador fuente a fuente**, también llamado transpilador. No genera código objeto ni un ejecutable por su cuenta. Esa parte corresponde al compilador de C# del SDK de .NET. La página ofrece una simulación del AST para probar ejemplos rápidamente, pero la simulación no ejecuta C#.

Esta carpeta contiene el núcleo y la terminal del tema 08. El tema 05 está en la carpeta vecina `05-mensajes-personalizados/`; ambos se presentan en una [página conjunta](../index.html).

## 1. Probar la página conjunta

Abre `../index.html` y dirígete a la sección **Tema 08**. Desde esta carpeta, en PowerShell:

```powershell
Start-Process ..\index.html
```

La página funciona sin servidor ni conexión y tiene pestañas de descripción, lenguaje, tokens, análisis, código, resultado y pruebas. Carga un ejemplo, compila a C#, revisa la simulación y descarga `Program.cs` o el reporte JSON. Cada edición invalida los resultados anteriores; una traducción fallida bloquea las descargas.

## 2. Usar la terminal

La CLI y las pruebas usan **Node.js 20 o posterior**. No hay dependencias npm; no es necesario ejecutar `npm install`.

```powershell
node --version
node .\src\cli.cjs --help
node .\src\cli.cjs .\examples\positivo.pseudo --out .\generated\Program.cs --report .\generated\reporte.json
node .\src\cli.cjs .\examples\positivo.pseudo --simulate
```

La primera traducción guarda el C# y un reporte con tokens, AST y símbolos. El segundo comando interpreta el AST y debe mostrar `Positivo`, junto con un aviso que identifica la salida como simulada.

| Opción | Resultado |
| --- | --- |
| `--out ruta.cs` | Guarda el código C# generado. Sin esta opción, lo escribe en la consola, salvo si se pide simulación. |
| `--report ruta.json` | Guarda el análisis completo cuando la traducción es válida. |
| `--simulate` | Simula el AST con límites; no llama a .NET. |
| `--help` | Muestra el uso de la CLI. |

Una entrada válida devuelve código de salida `0`. Un error de entrada, traducción o simulación devuelve `1`. Un error de traducción no crea ni reemplaza los archivos de salida. Una ruta de salida ya existente sí se reemplaza cuando la traducción es válida: usa una ruta dedicada a este ejemplo.

## 3. Compilar y ejecutar el C# de verdad

Instala el **SDK de .NET 8 o posterior**, no solamente el runtime. Se puede descargar desde [el sitio oficial de .NET](https://dotnet.microsoft.com/download). Node.js tiene su [descarga oficial](https://nodejs.org/en/download).

Desde esta carpeta, ejecuta los comandos uno por uno en PowerShell:

```powershell
dotnet --list-sdks
dotnet new console -n DemoPseudocodigo -o .\generated\DemoPseudocodigo
node .\src\cli.cjs .\examples\positivo.pseudo --out .\generated\DemoPseudocodigo\Program.cs
dotnet run --project .\generated\DemoPseudocodigo
```

El resultado del programa es:

```text
Positivo
```

`dotnet new` crea un proyecto de consola. Nuestra CLI reemplaza su `Program.cs` con la traducción. Después, `dotnet run` compila el proyecto y lo ejecuta. Si el proyecto ya existe, omite `dotnet new`; no es necesario forzar su recreación.

Este flujo funciona en Windows sin GCC, MSYS2 ni Bash. En macOS/Linux se pueden usar las mismas opciones cambiando `\` por `/` en las rutas.

## 4. Lenguaje admitido

Cada programa empieza con `INICIO` y termina con `FIN`, en líneas separadas. Se usa una instrucción por línea; la sangría ayuda a leer, pero no determina los bloques.

```text
INICIO
    ENTERO numero = 5
    SI numero > 0 ENTONCES
        IMPRIMIR "Positivo"
    SINO
        IMPRIMIR "No positivo"
    FIN_SI
FIN
```

| Pseudocódigo | Traducción / regla |
| --- | --- |
| `ENTERO n = 5` | `int v_n = 5;` · entero de 32 bits con signo. |
| `DECIMAL x = 2.5` | `double v_x = 2.5d;` · no es el tipo `decimal` de C#. |
| `TEXTO s = "Hola"` | `string v_s = "Hola";` |
| `LOGICO b = VERDADERO` | `bool v_b = true;` |
| `n <- n + 1` o `n = n + 1` | Asignación; la variable debe estar declarada. |
| `IMPRIMIR s` o `ESCRIBIR s` | `Console.WriteLine(...)`; añade salto de línea. |
| `IMPRIMIR "Valor: ", n` | Concatena los valores en orden, sin espacios adicionales. |
| `SI condición [ENTONCES]` / `SINO` / `FIN_SI` | `if` / `else`; `SINO` es opcional. |
| `MIENTRAS condición [HACER]` / `FIN_MIENTRAS` | `while`; `HACER` es opcional. |
| `PARA i = 1 HASTA 5 [PASO 1]` / `FIN_PARA` | Ciclo con ambos límites incluidos y paso entero literal distinto de cero. |

Los corchetes de la tabla indican elementos opcionales; **no se escriben** en el programa.

Reglas importantes:

- Palabras reservadas sin distinción de mayúsculas: `inicio` y `INICIO` son equivalentes. Los identificadores sí distinguen mayúsculas: `n` y `N` son distintos.
- Nombres de variables: letras ASCII, dígitos y `_`; no pueden empezar con un dígito ni ser palabras reservadas. El texto entre comillas sí admite Unicode.
- Todas las variables necesitan valor inicial. Se puede asignar un entero a `DECIMAL`, pero no un `DECIMAL` a `ENTERO`.
- Los nombres deben ser únicos en todo el programa. Una variable de un bloque solo es visible allí y en sus bloques internos; no se permite redeclarar el mismo nombre en otro bloque.
- Los operadores numéricos son `+ - * / %`. La división entre dos enteros descarta la fracción hacia cero: `-7 / 2` da `-3`. Usa un operando real, como `2.0`, para obtener división real.
- Comparaciones: `==`, `!=`, `<>`, `<`, `>`, `<=`, `>=`. Lógica: `Y`, `O`, `NO`, o `&&`, `||`, `!`. `Y` y `O` usan cortocircuito. Las condiciones deben ser `LOGICO`.
- El operador `+` también admite dos textos, pero no convierte automáticamente un número a texto. Para mostrar ambos, usa `IMPRIMIR "n=", n`.
- Los límites de `PARA` se calculan una vez al entrar. Su contador se declara en el ciclo y no se puede modificar manualmente. El paso por defecto es `1`; admite pasos negativos.
- Cadenas con comillas dobles; escapes admitidos: `\"`, `\\`, `\n`, `\t`, `\r`. Comentarios: `#` o `//` hasta el fin de línea. El `;` al final de una línea es opcional, pero no separa varias instrucciones en una misma línea.
- Los números reales usan punto decimal y no admiten notación exponencial ni sufijos. Este es un lenguaje didáctico propio, no una implementación completa de PSeInt.

La [gramática completa](docs/gramatica.md) incluye la precedencia y los cierres de bloque.

## 5. Fases implementadas

| Fase | Qué hace el proyecto | Evidencia |
| --- | --- | --- |
| Análisis léxico | Recorre caracteres y produce tokens con lexema, línea y columna. Reconoce cadenas sin sustituir su contenido. | Pestaña Tokens y reporte JSON. |
| Análisis sintáctico | Parser descendente con precedencia de operadores; construye un AST y comprueba bloques y expresiones. | Pestaña Árbol sintáctico. |
| Análisis semántico | Revisa declaraciones, ámbitos, tipos, condiciones y contadores; detecta algunos errores constantes. | Tabla de símbolos y diagnósticos semánticos. |
| Representación intermedia | Usa el AST anotado con tipos. No genera un IR de tres direcciones. | Reporte JSON. |
| Generación de código | Recorre el AST y emite un programa C# con `Main`, declaraciones, impresiones y control de flujo. | Editor C# y descarga `.cs`. |
| Optimización | **No implementada.** La evaluación de constantes se usa para validar, no para reemplazar expresiones en la salida. | Limitación declarada. |
| Compilación del destino y ejecución | **Externas:** Roslyn/.NET reciben el C# generado. El navegador solamente simula el AST. | Pruebas de integración y comandos `dotnet`. |

El proyecto no afirma ser de una sola pasada. La tokenización, el análisis sintáctico, la validación y la generación realizan recorridos separados. Tampoco consiste en reemplazar palabras con expresiones regulares: las instrucciones se generan a partir de una estructura validada.

## 6. Pruebas y evidencias

```powershell
npm test
npm run test:ui
npm run test:dotnet
```

- `npm test`: 99 pruebas del núcleo y la CLI, sin necesitar .NET.
- `npm run test:ui`: pruebas del controlador conjunto y la estructura HTML, con un DOM mínimo simulado. No verifican el diseño visual ni un navegador real.
- `npm run test:dotnet`: 17 casos que compilan con Roslyn y ejecutan con .NET. Si no se encuentra un SDK adecuado, se marcan como **omitidos**, no como aprobados.
- Las pruebas reales usan el compilador y las referencias incluidos en el SDK, sin paquetes NuGet. Si `dotnet` no está en el PATH, se puede indicar su ruta con `$env:PC_DOTNET = "C:\ruta\dotnet.exe"`.
- Los resultados incluidos en [evidencias del grupo](../evidencias/README.md) fueron obtenidos en un entorno de prueba local. No representan commits, ejecución en el equipo del estudiante ni integración ya realizada en el portal.

| Caso | Resultado esperado |
| --- | --- |
| `examples/hola.pseudo` | `Hola Mundo UIP` |
| `examples/positivo.pseudo` | `Positivo` |
| `examples/operaciones.pseudo` | Resultado `18`, promedio `7`, división entera `2`. |
| `examples/ciclos.pseudo` | `Suma: 15`, luego `3`, `2`, `1`. |
| `examples/logicos.pseudo` | `Aprobado` y `True`. |
| `examples/error-lexico.pseudo` | Rechaza el carácter `@`. |
| `examples/error-sintactico.pseudo` | Rechaza `IMPRIMIR` sin expresión. |
| `examples/error-semantico.pseudo` | Rechaza asignar `"veinte"` a `ENTERO`. |

Las pruebas adicionales cubren prioridad de operadores, escapes, nombres reservados de C#, ámbitos, ciclos anidados, división, límites enteros, errores y preservación de archivos ante una traducción fallida.

## 7. Organización

| Ruta | Contenido |
| --- | --- |
| `../index.html` | Página conjunta y laboratorios de los temas 05 y 08. |
| `../assets/` | Estilos y controlador compartidos. |
| `src/compiler.js` | Lexer, parser, análisis semántico, generador y simulador. |
| `src/cli.cjs` | Interfaz de terminal. |
| `examples/` | Cinco programas válidos y tres incorrectos. |
| `tests/` | Pruebas del compilador, CLI y .NET. |
| `docs/` | Gramática y guía para demostrar el trabajo. |
| `../evidencias/` | Registro de verificaciones de la entrega conjunta. |
| `generated/` | Salidas locales; está excluida por `.gitignore`. |

El núcleo también puede reutilizarse desde Node.js:

```javascript
const { compile, simulate } = require("./src/compiler.js");
const result = compile('INICIO\nIMPRIMIR "Hola"\nFIN');
if (result.ok) {
  console.log(result.code);
  console.log(simulate(result).output); // simulación, no ejecución de C#
} else {
  console.error(result.diagnostics);
}
```

En el navegador, cargar `src/compiler.js` expone la API como `window.PseudoCSharp`. `compile()` devuelve un resultado con `ok`, `code`, `tokens`, `ast`, `symbols` y `diagnostics`. No utiliza `eval` ni ejecuta texto ingresado como JavaScript.

## 8. Límites del proyecto

No incluye funciones, arreglos, lectura por teclado, archivos, objetos, `SEGUN`, `REPETIR`, conversiones explícitas ni recuperación para listar todos los errores a la vez. Reporta el primer error de traducción.

`ENTERO` corresponde a `int` de C#, de −2 147 483 648 a 2 147 483 647. El programa generado usa un contexto `checked` para detectar desbordamientos enteros. Una división entera por cero o un desbordamiento que depende de variables puede ocurrir en ejecución: .NET lanza su excepción, mientras que el simulador presenta su propio diagnóstico.

`DECIMAL` usa `double` (punto flotante binario). La cultura de la salida C# se fija a invariante. La simulación usa números de JavaScript; puede diferir de .NET en el formato de valores reales extremos, el cero negativo o detalles de redondeo. Las pruebas incluidas verifican casos concretos, no una equivalencia total con toda la plataforma C#.

Límites del traductor: 50 000 caracteres, 20 000 tokens y profundidad interna limitada a 64. La tabla web muestra como máximo 400 tokens; el reporte conserva todos. La simulación limita pasos (10 000), líneas de salida (500) y caracteres de salida (50 000). **Estos límites de ejecución no se insertan en el C# generado:** un `MIENTRAS` infinito seguirá siendo infinito en .NET. Ejecuta ejemplos revisados y usa `Ctrl + C` para detenerlos si hace falta.

## 9. Integración y fuentes

Consulta el [README del grupo](../README.md) y la [guía de integración](../docs/INTEGRACION.md). La ruta propuesta es `compiladores/grupo-05/`, con una sola página para los dos temas. El núcleo de este tema conserva su implementación anterior; la interfaz está centralizada.

Fuentes técnicas: [Referencia oficial de C#](https://learn.microsoft.com/dotnet/csharp/language-reference/), [herramientas de .NET](https://learn.microsoft.com/dotnet/core/tools/) y [dotnet run](https://learn.microsoft.com/dotnet/core/tools/dotnet-run). La guía del docente proporcionada por el usuario define las fases y los entregables académicos.
