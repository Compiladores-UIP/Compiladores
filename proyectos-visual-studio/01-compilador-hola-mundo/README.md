# 01 - Compilador “Hola Mundo”

## Descripción

Este mini-compilador reconoce una instrucción muy pequeña del lenguaje fuente del proyecto:

```text
IMPRIMIR "Hola Mundo"
```

La instrucción se analiza, se valida y se traduce a un programa equivalente en C#. Cuando la entrada es válida, el mini-compilador también genera físicamente un archivo `ProgramaGenerado.cs`, lo compila y produce `ProgramaGenerado.exe`.

## Objetivo

Demostrar de forma controlada las fases básicas de un compilador académico:

```text
Código fuente
↓
Análisis léxico
↓
Análisis sintáctico
↓
Generación de código C#
↓
Compilación
↓
Ejecución
↓
Resultado
```

## Nivel

Básico.

## Producto esperado

- Archivo C# generado: `ProgramaGenerado.cs`.
- Ejecutable generado: `ProgramaGenerado.exe`.

Ambos se crean localmente dentro de:

```text
salida/hola-mundo/
```

La carpeta `salida/` no forma parte del repositorio porque contiene artefactos generados durante la ejecución.

## Tecnologías

- C#.
- .NET 8.
- `System.Text.RegularExpressions` para reconocimiento básico de tokens.
- `System.Diagnostics.Process` para invocar `dotnet publish` y ejecutar el programa generado.

## Requisitos

- Windows 10 u 11.
- .NET 8 SDK instalado y disponible mediante el comando `dotnet`.
- Visual Studio 2022 es opcional, pero recomendado para abrir la solución completa.

Puede comprobarse la instalación de .NET con:

```powershell
dotnet --version
```

## Cómo ejecutar

### Desde Visual Studio 2022

1. Abrir `CompiladoresGrupo1.sln`.
2. En el Explorador de soluciones, seleccionar `MiniCompiladorHolaMundo`.
3. Establecerlo como proyecto de inicio.
4. Ejecutar con `F5` o `Ctrl + F5`.

### Desde PowerShell

Desde la raíz del repositorio:

```powershell
dotnet run --project ".\compiladores\01-compilador-hola-mundo\MiniCompiladorHolaMundo.csproj"
```

## Lenguaje fuente

El lenguaje admite una sola forma de instrucción:

```text
IMPRIMIR "texto"
```

Reglas:

- `IMPRIMIR` es obligatorio.
- Después de `IMPRIMIR` debe existir una cadena entre comillas dobles.
- No se admite texto adicional después de la cadena.
- La instrucción se procesa en una sola línea.

Ejemplos válidos:

```text
IMPRIMIR "Hola Mundo"
IMPRIMIR "Universidad Interamericana de Panamá"
IMPRIMIR "Compiladores"
```

## Palabras reservadas

| Palabra | Función |
|---|---|
| `IMPRIMIR` | Indica que el texto entre comillas debe mostrarse en la salida. |

## Tokens reconocidos

Para esta entrada:

```text
IMPRIMIR "Hola Mundo"
```

se identifican:

| Entrada | Tipo de token |
|---|---|
| `IMPRIMIR` | `PALABRA_RESERVADA` |
| `"Hola Mundo"` | `CADENA` |

Si aparece un elemento diferente, el analizador informa un error léxico.

## Análisis sintáctico

La estructura válida es:

```text
IMPRIMIR CADENA
```

El análisis verifica, en este orden:

1. Que la primera unidad sea `IMPRIMIR`.
2. Que exista una cadena después de `IMPRIMIR`.
3. Que no existan elementos adicionales después de la cadena.

## Código C# generado

Para:

```text
IMPRIMIR "Hola Mundo"
```

se genera un programa equivalente a:

```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hola Mundo");
    }
}
```

## Generación del `.cs` y `.exe`

Después de validar la entrada:

1. El código C# se guarda en `salida/hola-mundo/ProgramaGenerado.cs`.
2. Se crea un proyecto temporal de .NET fuera del repositorio.
3. El mini-compilador ejecuta `dotnet publish` para Windows x64.
4. El ejecutable resultante se copia a `salida/hola-mundo/ProgramaGenerado.exe`.
5. El proyecto temporal se elimina.
6. El `.exe` generado se ejecuta y su salida se muestra en la consola del mini-compilador.

El ejecutable generado es dependiente del runtime de .NET 8; por lo tanto, el equipo que lo ejecute debe tener instalado el runtime correspondiente.

## Caso válido

Entrada:

```text
IMPRIMIR "Hola Mundo"
```

Tokens:

```text
IMPRIMIR                PALABRA_RESERVADA
"Hola Mundo"            CADENA
```

Resultado del análisis:

```text
Análisis sintáctico: correcto.
```

Resultado final:

```text
Hola Mundo
```

Además deben existir:

```text
salida/hola-mundo/ProgramaGenerado.cs
salida/hola-mundo/ProgramaGenerado.exe
```

## Caso inválido

Entrada:

```text
IMPRIMIR
```

Resultado esperado:

```text
Error sintáctico: se esperaba una cadena después de IMPRIMIR.
```

Otro ejemplo inválido:

```text
MOSTRAR "Hola Mundo"
```

El analizador informa que `MOSTRAR` no pertenece al lenguaje definido para este ejercicio.

## Archivos del proyecto

| Archivo | Función |
|---|---|
| `Program.cs` | Contiene la lectura de la entrada, análisis léxico, análisis sintáctico, generación de C#, creación del `.cs`, compilación del `.exe` y ejecución final. |
| `MiniCompiladorHolaMundo.csproj` | Define el proyecto de consola en .NET 8. |
| `README.md` | Explica el funcionamiento, sintaxis, pruebas y forma de ejecución. |

## Qué no forma parte de la entrega

No se deben subir al repositorio:

```text
bin/
obj/
salida/
.vs/
```

Estas carpetas son locales o se generan automáticamente.

## Integrantes

- Daniela Insturaín
- Aaron Fechrenback
- Euris J. Rodríguez V.
