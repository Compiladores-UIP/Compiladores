# Gramática del lenguaje de mensajes personalizados

Notación EBNF. Las palabras en mayúsculas entre comillas son terminales; `?` = opcional, `*` = cero o más.

```ebnf
programa     = { instruccion FIN_LINEA } ;
instruccion  = declaracion | asignacion | mostrar | pedir ;

declaracion  = tipo IDENTIFICADOR [ "=" expresion ] ;
tipo         = "TEXTO" | "ENTERO" | "DECIMAL" ;
asignacion   = IDENTIFICADOR "=" expresion ;
mostrar      = ( "MOSTRAR" | "IMPRIMIR" ) expresion ;
pedir        = ( "PEDIR" | "LEER" ) IDENTIFICADOR [ CADENA ] ;

expresion    = termino { ( "+" | "-" ) termino } ;
termino      = factor { ( "*" | "/" ) factor } ;
factor       = CADENA | NUMERO_ENTERO | NUMERO_DECIMAL | IDENTIFICADOR
             | funcion "(" expresion ")"
             | "(" expresion ")" ;
funcion      = "MAYUSCULAS" | "MINUSCULAS" | "LONGITUD" ;
```

## Tokens

| Token | Patrón |
| --- | --- |
| `IDENTIFICADOR` | `[A-Za-z_][A-Za-z0-9_]*`, que no sea palabra reservada ni función |
| `NUMERO_ENTERO` | `[0-9]+` dentro del rango de `int` |
| `NUMERO_DECIMAL` | `[0-9]+\.[0-9]+` |
| `CADENA` | `"` … `"` en una sola línea; escapes `\"` `\\` `\n` `\t` |
| `OPERADOR` | `+` `-` `*` `/` |
| `ASIGNACION` | `=` |
| `FIN_LINEA` | salto de línea (las líneas vacías y los comentarios se ignoran) |

## Plantillas

Dentro de una `CADENA`:

```ebnf
plantilla = { texto | "{" IDENTIFICADOR "}" | "{{" | "}}" } ;
```

## Precedencia y tipos

1. Paréntesis y funciones.
2. `*` y `/` (izquierda a derecha).
3. `+` y `-` (izquierda a derecha).

| Operación | Resultado |
| --- | --- |
| TEXTO + cualquier tipo | TEXTO (concatenación) |
| ENTERO (+ - * /) ENTERO | ENTERO (la división descarta decimales) |
| ENTERO o DECIMAL (+ - * /) DECIMAL | DECIMAL |
| TEXTO con `-`, `*` o `/` | Error semántico |
| `MAYUSCULAS` / `MINUSCULAS`(TEXTO) | TEXTO |
| `LONGITUD`(TEXTO) | ENTERO |
