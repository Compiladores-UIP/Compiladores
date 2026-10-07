# 06 - Compilador de estructuras IF

## Descripción

Este mini-compilador interpreta una declaración de variable seguida de una estructura condicional escrita con palabras reservadas en español. La entrada se valida y se traduce a una estructura `if` de C#.

Ejemplo completo:

```text
ENTERO edad = 20
SI edad >= 18 ENTONCES
IMPRIMIR "Mayor de edad"
SINO
IMPRIMIR "Menor de edad"
FIN_SI
```

En la aplicación de consola, cuando se termina de escribir el código se debe introducir:

```text
EJECUTAR
```

`EJECUTAR` es solamente una orden de la consola para terminar la captura. No pertenece al lenguaje fuente, no se analiza y no aparece en la lista de tokens.

## Objetivo

Demostrar el reconocimiento y traducción de una estructura condicional sencilla:

```text
Código fuente
↓
Análisis léxico
↓
Análisis sintáctico
↓
Validación semántica básica
↓
Generación de código C# con if/else
↓
Evaluación del resultado
```

## Nivel

Intermedio.

## Producto esperado

Código C# equivalente que utilice una estructura `if` y, cuando corresponda, `else`.

## Tecnologías

- C#.
- .NET 8.
- Expresiones regulares para reconocer declaraciones, condiciones e instrucciones de impresión.
- `CultureInfo.InvariantCulture` para interpretar valores numéricos de forma consistente.

## Requisitos

- .NET 8 SDK.
- Windows, Linux o macOS para la aplicación de consola.
- Visual Studio 2022 es opcional para abrir la solución completa.

## Cómo ejecutar

### Desde Visual Studio 2022

1. Abrir `CompiladoresGrupo1.sln`.
2. Seleccionar `MiniCompiladorEstructurasIf`.
3. Establecerlo como proyecto de inicio.
4. Ejecutar con `F5` o `Ctrl + F5`.
5. Escribir el código línea por línea.
6. Escribir `EJECUTAR` en una línea independiente para iniciar el análisis.

### Desde PowerShell

Desde la raíz del repositorio:

```powershell
dotnet run --project ".\compiladores\06-compilador-estructuras-if\MiniCompiladorEstructurasIf.csproj"
```

## Lenguaje fuente

La estructura general es:

```text
TIPO variable = valor
SI variable OPERADOR valor ENTONCES
IMPRIMIR "texto"
SINO
IMPRIMIR "texto"
FIN_SI
```

La sección `SINO` es opcional.

Las líneas vacías están permitidas y se ignoran durante el análisis.

## Tipos admitidos

| Tipo del lenguaje | Tipo generado en C# | Ejemplo |
|---|---|---|
| `ENTERO` | `int` | `ENTERO edad = 20` |
| `DECIMAL` | `decimal` | `DECIMAL promedio = 91.5` |
| `TEXTO` | `string` | `TEXTO estado = "activo"` |

Para `TEXTO`, este mini-compilador admite únicamente los operadores `==` y `!=`.

## Palabras reservadas

| Palabra | Función |
|---|---|
| `ENTERO` | Declara una variable entera. |
| `DECIMAL` | Declara una variable decimal. |
| `TEXTO` | Declara una variable de texto. |
| `SI` | Inicia la condición. |
| `ENTONCES` | Separa la condición del bloque verdadero. |
| `IMPRIMIR` | Indica el mensaje que debe mostrarse. |
| `SINO` | Inicia el bloque alternativo. Es opcional. |
| `FIN_SI` | Cierra la estructura condicional. |

`EJECUTAR` no aparece en esta tabla porque no es una palabra reservada del lenguaje. Es una instrucción de la interfaz de consola.

## Operadores admitidos

| Operador | Significado |
|---|---|
| `>` | Mayor que. |
| `<` | Menor que. |
| `>=` | Mayor o igual que. |
| `<=` | Menor o igual que. |
| `==` | Igual a. |
| `!=` | Diferente de. |

## Tokens reconocidos

Para esta entrada:

```text
ENTERO edad = 20
SI edad >= 18 ENTONCES
IMPRIMIR "Mayor de edad"
SINO
IMPRIMIR "Menor de edad"
FIN_SI
```

se reconocen, entre otros:

| Entrada | Tipo de token |
|---|---|
| `ENTERO` | `PALABRA_RESERVADA` |
| `edad` | `IDENTIFICADOR` |
| `=` | `ASIGNACION` |
| `20` | `NUMERO` |
| `SI` | `PALABRA_RESERVADA` |
| `>=` | `OPERADOR_RELACIONAL` |
| `18` | `NUMERO` |
| `ENTONCES` | `PALABRA_RESERVADA` |
| `IMPRIMIR` | `PALABRA_RESERVADA` |
| `"Mayor de edad"` | `CADENA` |
| `SINO` | `PALABRA_RESERVADA` |
| `FIN_SI` | `PALABRA_RESERVADA` |

## Análisis sintáctico

El mini-compilador comprueba el código en este orden:

1. Debe existir una declaración inicial con el formato `TIPO nombre = valor`.
2. El valor declarado debe corresponder al tipo indicado.
3. Después debe existir una instrucción `SI`.
4. La condición debe usar la misma variable declarada.
5. La condición debe contener un operador admitido y terminar con `ENTONCES`.
6. Después de `ENTONCES` debe existir `IMPRIMIR "texto"`.
7. Puede existir un bloque `SINO` seguido de otro `IMPRIMIR "texto"`.
8. La estructura debe terminar con `FIN_SI`.
9. No se permite código adicional después de `FIN_SI`.

## Validación semántica básica

Además de la forma del código, se comprueba:

- que la variable utilizada en la condición sea la misma variable declarada;
- que un `ENTERO` reciba valores enteros;
- que un `DECIMAL` reciba valores numéricos;
- que un `TEXTO` utilice cadenas entre comillas;
- que las comparaciones de `TEXTO` utilicen `==` o `!=`.

## Caso válido

Entrada escrita en la consola:

```text
ENTERO edad = 20
SI edad >= 18 ENTONCES
IMPRIMIR "Mayor de edad"
SINO
IMPRIMIR "Menor de edad"
FIN_SI
EJECUTAR
```

Código C# generado:

```csharp
using System;

class Program
{
    static void Main()
    {
        int edad = 20;

        if (edad >= 18)
        {
            Console.WriteLine("Mayor de edad");
        }
        else
        {
            Console.WriteLine("Menor de edad");
        }
    }
}
```

Resultado esperado:

```text
Mayor de edad
```

## Caso válido con líneas vacías

También es válido:

```text
ENTERO edad = 20

SI edad >= 18 ENTONCES
IMPRIMIR "Mayor de edad"
SINO
IMPRIMIR "Menor de edad"
FIN_SI
EJECUTAR
```

La línea vacía se ignora.

## Caso inválido: falta ENTONCES

Entrada:

```text
ENTERO edad = 20
SI edad >= 18
IMPRIMIR "Mayor de edad"
FIN_SI
EJECUTAR
```

Mensaje esperado:

```text
Error sintáctico: se esperaba ENTONCES después de la condición.
```

## Caso inválido: falta FIN_SI

Entrada:

```text
ENTERO edad = 20
SI edad >= 18 ENTONCES
IMPRIMIR "Mayor de edad"
EJECUTAR
```

Mensaje esperado:

```text
Error sintáctico: falta FIN_SI.
```

## Archivos del proyecto

| Archivo | Función |
|---|---|
| `Program.cs` | Contiene la captura de líneas, análisis, validaciones, generación del código C# y evaluación del resultado. |
| `MiniCompiladorEstructurasIf.csproj` | Define el proyecto de consola en .NET 8. |
| `README.md` | Describe el lenguaje, tokens, reglas, pruebas y ejecución. |


## Integrantes

- Daniela Insturaín
- Aaron Fechrenback
- Euris J. Rodríguez V.
