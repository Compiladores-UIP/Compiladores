# 04 · Mini-Compilador Calculadora

**Universidad Interamericana de Panamá · Compiladores · Portal Nexo**
**Grupo 4:** Karen Arauz · Carlos Zachrison

Mini-compilador que reconoce operaciones aritméticas escritas en español y las traduce a un programa
de consola en C#. Respeta la precedencia de los operadores, construye el árbol de cada expresión,
calcula el tipo de cada resultado y detecta, antes de ejecutar, la división entre cero, la raíz de un
número negativo y los resultados que no caben en un `ENTERO`.

## Lenguaje fuente

```
DECIMAL a = 10
DECIMAL b = 5
IMPRIMIR "Suma:", a + b
IMPRIMIR "División:", a / b
ENTERO x = 7
IMPRIMIR x % 2
IMPRIMIR RAIZ(3 ^ 2 + 4 ^ 2)
x = x * 3 - 1
MOSTRAR "x ahora vale", x
```

Reglas: una instrucción por línea, palabras clave en mayúsculas, punto como separador decimal,
`;` al final es opcional y `//` inicia un comentario. Los textos entre comillas solo sirven como
etiqueta de `IMPRIMIR`.

| Lenguaje fuente | C# | Ejemplo | C# generado |
|---|---|---|---|
| `ENTERO` | `int` | `ENTERO x = 7` | `int x = 7;` |
| `DECIMAL` | `double` | `DECIMAL a = 10` | `double a = 10;` |
| asignación | `=` | `x = x * 3 - 1` | `x = x * 3 - 1;` |
| `+ - * %` | `+ - * %` | `a * b` | `a * b` |
| `/` | `/` | `x / y` (dos `ENTERO`) | `(double)x / y` |
| `^` | `Math.Pow` | `a ^ 2` | `Math.Pow(a, 2)` |
| `RAIZ( )` | `Math.Sqrt` | `RAIZ(16)` | `Math.Sqrt(16)` |
| `ABS( )` | `Math.Abs` | `ABS(b - a)` | `Math.Abs(b - a)` |
| `IMPRIMIR` / `MOSTRAR` | `Console.WriteLine` | `IMPRIMIR "Suma:", a + b` | `Console.WriteLine("Suma: " + (a + b));` |

### Precedencia (de mayor a menor)

| Nivel | Operadores | Asociatividad | Ejemplo |
|---|---|---|---|
| 1 | `( )`, `RAIZ( )`, `ABS( )` | — | `(2 + 3) * 4 = 20` |
| 2 | `^` | derecha | `2 ^ 3 ^ 2 = 2 ^ 9 = 512` |
| 3 | `-` (negativo) | derecha | `-2 ^ 2 = -4` |
| 4 | `*` `/` `%` | izquierda | `2 + 3 * 4 = 14` |
| 5 | `+` `-` | izquierda | `10 - 4 - 3 = 3` |

### Tipos del resultado

| Operación | Resultado |
|---|---|
| `ENTERO` con `ENTERO` en `+ - * %` | `ENTERO` |
| Si alguno es `DECIMAL` | `DECIMAL` |
| `/`, `^` y `RAIZ` | siempre `DECIMAL` (en la calculadora `7 / 2 = 3.5`) |
| `ABS` y el negativo | el mismo tipo del valor |

Un `DECIMAL` acepta un `ENTERO`; un `ENTERO` no acepta un `DECIMAL`, porque se perderían los decimales.

## Tokens

| Token | Ejemplos |
|---|---|
| `TOKEN_TIPO` | ENTERO, DECIMAL |
| `TOKEN_IMPRIMIR` | IMPRIMIR, MOSTRAR |
| `TOKEN_FUNCION` | RAIZ, ABS |
| `TOKEN_IDENTIFICADOR` | a, b, promedio |
| `TOKEN_NUMERO` / `TOKEN_DECIMAL` | 10 / 4.5 |
| `TOKEN_TEXTO` | "Suma:" |
| `TOKEN_ASIGNACION` | = |
| `TOKEN_SUMA` `TOKEN_RESTA` `TOKEN_MULTIPLICACION` `TOKEN_DIVISION` `TOKEN_MODULO` `TOKEN_POTENCIA` | + - * / % ^ |
| `TOKEN_PAREN_ABRE` / `TOKEN_PAREN_CIERRA` | ( / ) |
| `TOKEN_COMA` / `TOKEN_FIN` | , / ; |

## Gramática (BNF)

```
<programa>    ::= { <sentencia> }
<sentencia>   ::= <declaracion> | <asignacion> | <impresion>
<declaracion> ::= ( ENTERO | DECIMAL ) IDENT "=" <expresion> [ ";" ]
<asignacion>  ::= IDENT "=" <expresion> [ ";" ]
<impresion>   ::= ( IMPRIMIR | MOSTRAR ) ( TEXTO [ "," <expresion> ] | <expresion> ) [ ";" ]
<expresion>   ::= <termino> { ( "+" | "-" ) <termino> }
<termino>     ::= <unario> { ( "*" | "/" | "%" ) <unario> }
<unario>      ::= "-" <unario> | <potencia>
<potencia>    ::= <primario> [ "^" <unario> ]
<primario>    ::= NUMERO | DECIMAL | IDENT | "(" <expresion> ")" | ( RAIZ | ABS ) "(" <expresion> ")"
```

El analizador sintáctico es descendente recursivo: cada regla es un método de `AnalizadorSintactico.cs`
y el orden de las llamadas define la precedencia.

## Fases

| Fase | ¿Implementada? | Archivo |
|---|---|---|
| 1. Análisis léxico | Sí | `Lexico/AnalizadorLexico.cs` |
| 2. Análisis sintáctico (árbol de expresiones) | Sí | `Sintactico/AnalizadorSintactico.cs` |
| 3. Análisis semántico: tipos, tabla de símbolos y valores | Sí | `Semantico/AnalizadorSemantico.cs`, `Semantico/Aritmetica.cs` |
| 4. Generación de código C# | Sí | `Generacion/GeneradorCSharp.cs` |
| 5. Compilación .NET y ejecución | Sí (opción 5 del menú: crea `ProgramaGenerado` y ejecuta `dotnet run`) | `Generacion/Ejecutor.cs` |
| Optimización de código | Conceptual: como todos los valores se conocen al compilar, `2 ^ 3 * a` podría generarse como `8 * a` (plegado de constantes) | — |

Como el programa no lee datos del usuario, el análisis semántico conoce el valor de cada variable en
cada línea. Por eso puede avisar de una división entre cero **antes** de generar el código.
`Aritmetica.cs` reproduce las reglas de C# (`int`, `double`, `%` con signo, `-0`, formato de salida), así
que la salida simulada es la misma que da el programa compilado con .NET.

Validaciones semánticas: variable no declarada (con sugerencia si solo cambian mayúsculas), variable
declarada dos veces, `DECIMAL` guardado en `ENTERO`, división o residuo entre cero, raíz de un negativo,
potencia sin resultado real, `ENTERO` fuera de rango, nombres reservados de C# y variables sin usar (aviso).

## Cómo ejecutarlo

Requisitos: .NET 8 SDK y Visual Studio 2022 (o VS Code con la extensión C#).

- **Visual Studio:** abrir `CompiladorCalculadora.sln` y presionar F5.
- **Consola:**
  ```
  cd CompiladorCalculadora
  dotnet run                              # menú interactivo
  dotnet run -- ejemplos/correcto.txt     # compilar un archivo directo
  ```

Menú: 1) ejemplo correcto · 2) ejemplo con errores · 3) escribir código · 4) abrir .txt ·
5) compilar y ejecutar el C# generado con .NET.

## Pruebas

**Caso correcto** (`ejemplos/correcto.txt`). Resultado esperado (igual en la simulación y en .NET):
```
Suma: 15
Resta: 5
Multiplicación: 50
División: 2
7 / 2 = 3.5
7 % 2 = 1
Promedio: 7.333333333333333
Hipotenusa: 5
Distancia: 5
x ahora vale 20
```

**Casos incorrectos** (`ejemplos/con_errores.txt`, con `DECIMAL a = 10` y `DECIMAL b = 0`):

| Entrada | Mensaje |
|---|---|
| `IMPRIMIR a / b` | Error semántico: División entre cero: "b" vale 0. |
| `IMPRIMIR a +` | Error sintáctico: Falta un valor después de "+". |
| `ENTERO mitad = a / 2` | Error semántico: No se puede guardar un valor DECIMAL (a / 2) en una variable ENTERO. La división siempre da un DECIMAL. |
| `IMPRIMIR a # b` | Error léxico: Símbolo no reconocido: '#'. |
| `imprimir a + b` | Error sintáctico: "imprimir" no se reconoce como palabra clave. → IMPRIMIR |
| `IMPRIMIR total * 2` | Error semántico: La variable "total" no ha sido declarada. |
| `DECIMAL c = 4,5` | Error sintáctico: "4,5" no es un número válido. → El separador decimal es el punto: 4.5 |
| `IMPRIMIR (a + 1` | Error sintáctico: Falta cerrar el paréntesis abierto en la columna 10. |
| `IMPRIMIR RAIZ(b - a)` | Error semántico: No existe la raíz cuadrada real de un número negativo (-10). |
| `DECIMAL a = 3` | Error semántico: La variable "a" ya fue declarada en la línea 3. |

## Estructura

```
04-calculadora/
├── CompiladorCalculadora.sln
├── README.md
└── CompiladorCalculadora/
    ├── CompiladorCalculadora.csproj
    ├── Program.cs              # menú y salida por fases
    ├── Compilador.cs           # une las fases
    ├── ErrorCompilacion.cs
    ├── Pantalla.cs             # tablas y colores en consola
    ├── Lexico/                 # Token.cs, AnalizadorLexico.cs
    ├── Sintactico/             # Arbol.cs, AnalizadorSintactico.cs
    ├── Semantico/              # AnalizadorSemantico.cs, Aritmetica.cs
    ├── Generacion/             # GeneradorCSharp.cs, Ejecutor.cs
    └── ejemplos/               # correcto.txt, con_errores.txt
```
