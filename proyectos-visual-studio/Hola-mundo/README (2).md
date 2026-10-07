# Mini-Compilador "Hola Mundo" — Grupo 1 (UIP, Compiladores)

Integrantes: Daniela Insturaín, Aarón Fehrenbach, Eurys Rodriguez. Nivel: Básico.

## Qué hace
Reconoce `IMPRIMIR "Hola Mundo"`, valida que esté bien escrito, lo traduce a C# (`Hola.cs`) y lo compila a `Hola.exe`.

## Lenguaje fuente
    IMPRIMIR "Hola Mundo"
Una instrucción por línea. El `;` final es opcional.

## Tokens
| Token | Qué reconoce | Ejemplo |
|---|---|---|
| IMPRIMIR | Palabra reservada | IMPRIMIR |
| CADENA | Texto entre comillas dobles | "Hola Mundo" |
| PUNTOCOMA | Fin de instrucción (opcional) | ; |
| EOF | Fin del archivo | (automático) |

## Gramática
    programa  -> sentencia+ EOF
    sentencia -> IMPRIMIR CADENA [ ";" ]

## Fases
1. Análisis léxico — implementado (`lexer`)
2. Análisis sintáctico — implementado (`parser`)
3. Validación semántica — implementada, básica (`semantico`: cadena no vacía)
4. Generación de código C# — implementada (`generar`)
5. Compilación a .exe — herramienta externa (`csc`), invocada por el programa
6. Ejecución — evidencia

## Uso
    python compilador.py ejemplos/correcto.txt   # caso correcto
    python compilador.py ejemplos/error.txt      # error controlado

Si Windows tiene `csc.exe` (.NET Framework), el programa genera y ejecuta `salida/Hola.exe`. Si no, compilar a mano:

    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe salida\Hola.cs

## Pruebas
| Caso | Entrada | Resultado |
|---|---|---|
| Correcto | IMPRIMIR "Hola Mundo" | Genera Hola.cs; imprime Hola Mundo |
| Incorrecto | IMPRIMIR Hola | [Léxico] símbolo no reconocido 'H'; no se genera código |

## Estructura
    compilador.py   código del compilador
    ejemplos/       entradas de prueba
    salida/         Hola.cs, Hola.exe y capturas

