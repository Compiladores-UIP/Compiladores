# 03 · Mini-Compilador de Variables

**Universidad Interamericana de Panamá · Compiladores · Portal Nexo**
**Grupo 3:** Ana Vallarino · Josimar Osorio

Mini-compilador que reconoce declaraciones de variables escritas en español y las traduce a C#.
Identifica el tipo, el identificador, el operador de asignación y el valor, y construye la tabla de símbolos.
Está basado en la versión inicial de Josimar Osorio y se amplió para leer varias líneas y explicar cada error.

## Lenguaje fuente

```
ENTERO edad = 25
TEXTO nombre = "Carlos"
DECIMAL promedio = 91.5
BOOLEANO activo = VERDADERO
IMPRIMIR nombre
MOSTRAR promedio
```

Reglas: una instrucción por línea, palabras clave en mayúsculas, textos entre comillas dobles,
punto como separador decimal, `;` al final es opcional y `//` inicia un comentario.

| Lenguaje fuente | C#       | Ejemplo                      | C# generado                  |
|-----------------|----------|------------------------------|------------------------------|
| `ENTERO`        | `int`    | `ENTERO edad = 25`           | `int edad = 25;`             |
| `DECIMAL`       | `double` | `DECIMAL promedio = 91.5`    | `double promedio = 91.5;`    |
| `TEXTO`         | `string` | `TEXTO nombre = "Carlos"`    | `string nombre = "Carlos";`  |
| `BOOLEANO`      | `bool`   | `BOOLEANO activo = VERDADERO`| `bool activo = true;`        |
| `IMPRIMIR` / `MOSTRAR` | `Console.WriteLine` | `IMPRIMIR edad` | `Console.WriteLine(edad);` |

## Tokens

| Token                 | Ejemplos                              |
|-----------------------|---------------------------------------|
| `TOKEN_TIPO`          | ENTERO, DECIMAL, TEXTO, BOOLEANO      |
| `TOKEN_IMPRIMIR`      | IMPRIMIR, MOSTRAR                     |
| `TOKEN_IDENTIFICADOR` | edad, nombre, promedio                |
| `TOKEN_ASIGNACION`    | =                                     |
| `TOKEN_ENTERO`        | 25                                    |
| `TOKEN_DECIMAL`       | 91.5                                  |
| `TOKEN_TEXTO`         | "Carlos"                              |
| `TOKEN_BOOLEANO`      | VERDADERO, FALSO                      |
| `TOKEN_FIN`           | ;                                     |

## Gramática (BNF)

```
<programa>    ::= { <sentencia> }
<sentencia>   ::= <declaracion> | <impresion>
<declaracion> ::= <tipo> IDENTIFICADOR "=" <valor> [ ";" ]
<impresion>   ::= ( IMPRIMIR | MOSTRAR ) <valor> [ ";" ]
<tipo>        ::= ENTERO | DECIMAL | TEXTO | BOOLEANO
<valor>       ::= NUMERO_ENTERO | NUMERO_DECIMAL | CADENA | VERDADERO | FALSO | IDENTIFICADOR
```

## Fases

| Fase | ¿Implementada? | Archivo |
|------|----------------|---------|
| 1. Análisis léxico | Sí | `Lexico/AnalizadorLexico.cs` |
| 2. Análisis sintáctico (árbol) | Sí | `Sintactico/AnalizadorSintactico.cs` |
| 3. Análisis semántico + tabla de símbolos | Sí | `Semantico/AnalizadorSemantico.cs` |
| 4. Generación de código C# | Sí | `Generacion/GeneradorCSharp.cs` |
| 5. Compilación .NET y ejecución | Sí (opción 5 del menú: crea `ProgramaGenerado` y ejecuta `dotnet run`) | `Generacion/Ejecutor.cs` |
| Optimización de código | Conceptual (no aplica a este lenguaje) | — |

Validaciones semánticas: variable no declarada, variable declarada dos veces, tipo incompatible
(`DECIMAL` acepta `ENTERO`, al revés no) y nombres que son palabras reservadas de C#.

## Cómo ejecutarlo

Requisitos: .NET 8 SDK y Visual Studio 2022 (o VS Code con la extensión C#).

- **Visual Studio:** abrir `CompiladorVariables.sln` y presionar F5.
- **Consola:**
  ```
  cd CompiladorVariables
  dotnet run                              # menú interactivo
  dotnet run -- ejemplos/correcto.txt     # compilar un archivo directo
  ```

Menú: 1) ejemplo correcto · 2) ejemplo con errores · 3) escribir código · 4) abrir .txt ·
5) compilar y ejecutar el C# generado con .NET.

## Pruebas

**Caso correcto** (`ejemplos/correcto.txt`). Resultado esperado:
```
Carlos
25
91.5
True
```

**Casos incorrectos** (`ejemplos/con_errores.txt`):

| Entrada | Mensaje |
|---------|---------|
| `ENTERO edad = 20.5` | Error semántico: No se puede guardar un valor DECIMAL en una variable ENTERO. |
| `TEXTO nombre = Carlos` | Error semántico: La variable "Carlos" no ha sido declarada. → escríbelo entre comillas |
| `entero nota = 90` | Error sintáctico: "entero" no se reconoce como palabra clave. |
| `ENTERO = 5` | Error sintáctico: Se esperaba el nombre de la variable y se encontró "=". |
| `DECIMAL 2promedio = 8.5` | Error léxico: "2promedio" no es un identificador válido. |
| `BOOLEANO activo = "si"` | Error semántico: No se puede guardar un valor TEXTO en una variable BOOLEANO. |
| `IMPRIMIR apellido` | Error semántico: La variable "apellido" no ha sido declarada. |
| `TEXTO saludo = "Hola` | Error léxico: Texto sin cerrar. |

## Estructura

```
03-variables/
├── CompiladorVariables.sln
├── README.md
└── CompiladorVariables/
    ├── CompiladorVariables.csproj
    ├── Program.cs              # menú y salida por fases
    ├── Compilador.cs           # une las fases
    ├── ErrorCompilacion.cs
    ├── Pantalla.cs             # tablas y colores en consola
    ├── Lexico/                 # Token.cs, AnalizadorLexico.cs
    ├── Sintactico/             # Arbol.cs, AnalizadorSintactico.cs
    ├── Semantico/              # AnalizadorSemantico.cs (tabla de símbolos)
    ├── Generacion/             # GeneradorCSharp.cs, Ejecutor.cs
    └── ejemplos/               # correcto.txt, con_errores.txt
```
