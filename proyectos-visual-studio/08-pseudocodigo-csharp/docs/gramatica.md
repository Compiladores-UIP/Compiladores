# Gramática del lenguaje didáctico

Esta especificación describe el subconjunto que reconoce `src/compiler.js`. No pretende aceptar cualquier pseudocódigo ni archivos de PSeInt.

## Convenciones

`{...}` significa repetición, `[...]` es opcional y `|` separa alternativas. Las palabras entre comillas son elementos del programa. `NL` representa un salto de línea; `EOF`, fin de archivo. Se permiten líneas vacías entre instrucciones. El último `FIN` puede terminar directamente en EOF.

```ebnf
programa     = { NL }, "INICIO", fin_linea, bloque,
               "FIN", [ ";" ], { NL }, EOF ;
fin_linea    = [ ";" ], NL, { NL } ;
bloque       = { instruccion } ;
instruccion  = declaracion | asignacion | impresion
             | decision | mientras | para ;

tipo         = "ENTERO" | "DECIMAL" | "TEXTO" | "LOGICO" ;
asignador    = "=" | "<-" ;
declaracion  = tipo, identificador, asignador, expresion, fin_linea ;
asignacion   = identificador, asignador, expresion, fin_linea ;
impresion    = ("IMPRIMIR" | "ESCRIBIR"), expresion,
               { ",", expresion }, fin_linea ;

decision     = "SI", expresion, [ "ENTONCES" ], fin_linea, bloque,
               [ "SINO", fin_linea, bloque ], "FIN_SI", fin_linea ;
mientras     = "MIENTRAS", expresion, [ "HACER" ], fin_linea,
               bloque, "FIN_MIENTRAS", fin_linea ;
para         = "PARA", identificador, asignador, expresion,
               "HASTA", expresion, [ "PASO", paso ], fin_linea,
               bloque, "FIN_PARA", fin_linea ;
paso         = [ "+" | "-" ], entero_literal ;

expresion    = disyuncion ;
disyuncion   = conjuncion, { ("O" | "||"), conjuncion } ;
conjuncion   = igualdad, { ("Y" | "&&"), igualdad } ;
igualdad     = comparacion, { ("==" | "!=" | "<>"), comparacion } ;
comparacion  = suma, { ("<" | ">" | "<=" | ">="), suma } ;
suma         = producto, { ("+" | "-"), producto } ;
producto     = unaria, { ("*" | "/" | "%"), unaria } ;
unaria       = ("+" | "-" | "NO" | "!"), unaria | primaria ;
primaria     = numero | cadena | "VERDADERO" | "FALSO"
             | identificador | "(", expresion, ")" ;

identificador = (letra_ascii | "_"), { letra_ascii | digito | "_" } ;
entero_literal = digito, { digito } ;
numero        = entero_literal, [ ".", digito, { digito } ] ;
```

Las reglas de bloque se detienen ante su cierre correspondiente: `SINO`, `FIN_SI`, `FIN_MIENTRAS`, `FIN_PARA` o `FIN`. No se permite mezclar esos cierres. Las palabras reservadas no se pueden usar como identificadores.

## Tokens y posiciones

El lexer produce palabras reservadas, `IDENTIFIER`, `NUMBER`, `STRING`, `ASSIGN`, `OPERATOR`, `LPAREN`, `RPAREN`, `COMMA`, `SEMICOLON`, `NEWLINE` y `EOF`. Cada token guarda `type`, `value`, `raw`, `line` y `column`. Las posiciones empiezan en 1; los saltos CRLF de Windows cuentan como un solo salto y cada tabulador cuenta como un carácter, no como cuatro columnas visuales. Se acepta un BOM UTF-8 inicial.

Los comentarios `#` y `//` se descartan hasta el fin de línea, pero no dentro de cadenas. Las cadenas usan comillas dobles y no pueden cruzar líneas reales. Admiten `\"`, `\\`, `\n`, `\t` y `\r`. El lexer normaliza `<>` a `!=`; el parser normaliza `Y`, `O` y `NO` a operadores lógicos.

## Precedencia y asociatividad

| Prioridad, de menor a mayor | Operadores |
| --- | --- |
| 1 | `O`, `||` |
| 2 | `Y`, `&&` |
| 3 | `==`, `!=`, `<>` |
| 4 | `<`, `>`, `<=`, `>=` |
| 5 | `+`, `-` binarios |
| 6 | `*`, `/`, `%` |
| 7 | `+`, `-`, `NO`, `!` unarios |

Los operadores binarios se agrupan de izquierda a derecha. Los paréntesis permiten cambiar la agrupación. Ejemplos: `2 + 3 * 4` da `14`; `(2 + 3) * 4` da `20`. Una comparación encadenada como `1 < n < 5` no es una abreviatura admitida: escribe `1 < n Y n < 5`.

## Validación semántica

Una expresión debe tener el tipo que necesita su contexto. Las condiciones de `SI` y `MIENTRAS` son lógicas; los límites de `PARA` son enteros. El paso es un entero literal de 32 bits, distinto de cero, y no puede ser una variable ni una expresión.

Todas las declaraciones requieren inicialización. Se permite promoción de `ENTERO` a `DECIMAL`; el camino inverso se rechaza. Un nombre debe declararse antes de usarlo, ser único en todo el programa y encontrarse en un ámbito visible. El contador de `PARA` solo es visible dentro del ciclo y no puede recibir asignaciones del programa.

`+` acepta dos operandos numéricos o dos textos. Los demás operadores aritméticos requieren números. La igualdad acepta tipos iguales o dos tipos numéricos; los operadores de orden solo aceptan números. La lógica exige valores `LOGICO`.

Los literales fuera de rango, ciertos desbordamientos constantes y las divisiones enteras constantes por cero se rechazan antes de generar C#. No se hace análisis global de los valores posibles de las variables.

## Representación y generación

El AST tiene nodos `Program`, `Declaration`, `Assignment`, `Print`, `If`, `While`, `For`, `Literal`, `Identifier`, `Unary` y `Binary`. El análisis anota tipos y construye la tabla de símbolos. El generador usa esos nodos, no sustituciones de texto.

Los identificadores se prefijan con `v_`, de modo que un nombre fuente como `class` no se convierta en una palabra reservada de C#. `PARA` usa temporales internos `long` para que el incremento posterior a un límite extremo de `int` no cause un desbordamiento innecesario. El contador visible sigue siendo `int`.

El programa generado establece cultura invariante para imprimir números y un contexto `checked` para operaciones enteras. No modifica el significado de las expresiones mediante optimización.
