# 11 · Mini-Compilador Lenguaje de configuración

**Universidad Interamericana de Panamá · Compiladores · Portal Nexo**
**Grupo 4:** Karen Arauz · Carlos Zachrison

Mini-compilador que lee un archivo de configuración escrito en español y genera un documento
**JSON** o **XML**. Valida que cada clave sea única dentro de su sección, deduce el tipo de cada valor
(texto, entero, decimal, booleano o lista), lo compara con el tipo anotado y conserva los tipos en el
documento generado. Además genera un programa en C# que lee ese documento con .NET, para comprobar
que es válido.

## Lenguaje fuente

```
FORMATO JSON
CONFIG nombre = "Nexo"
CONFIG version = 1.5
CONFIG puerto = 3100
CONFIG activo = verdadero
CONFIG idiomas = ["es", "en"]

SECCION servidor
    CONFIG host = "localhost"
    CONFIG puerto : ENTERO = 8080
    SECCION seguridad
        CONFIG https = falso
    FINSECCION
FINSECCION
```

Reglas: una instrucción por línea, palabras clave en mayúsculas, `FORMATO` en la primera línea,
textos entre comillas dobles, punto como separador decimal, `;` al final es opcional y `//` o `#`
inician un comentario. Dentro de un texto se pueden usar `\"`, `\\`, `\n` y `\t`.

| Lenguaje fuente | JSON | XML |
|---|---|---|
| `FORMATO JSON` / `FORMATO XML` | `{ … }` | `<configuracion> … </configuracion>` |
| `CONFIG nombre = "Nexo"` | `"nombre": "Nexo"` | `<nombre tipo="texto">Nexo</nombre>` |
| `CONFIG puerto = 3100` | `"puerto": 3100` | `<puerto tipo="entero">3100</puerto>` |
| `CONFIG version = 1.5` | `"version": 1.5` | `<version tipo="decimal">1.5</version>` |
| `CONFIG activo = verdadero` | `"activo": true` | `<activo tipo="booleano">true</activo>` |
| `CONFIG idiomas = ["es", "en"]` | `"idiomas": ["es", "en"]` | `<idiomas tipo="lista" elementos="texto">` con un `<elemento>` por valor |
| `SECCION servidor … FINSECCION` | `"servidor": { … }` | `<servidor> … </servidor>` |

### Tipos

| Tipo | Ejemplo | Tipo en C# |
|---|---|---|
| `TEXTO` | `"localhost"` | `string` |
| `ENTERO` | `3100`, `-5` | `int` |
| `DECIMAL` | `1.5` | `double` |
| `BOOLEANO` | `verdadero`, `falso` | `bool` |
| `LISTA` | `[80, 443]` | arreglo (`int[]`, `string[]`, …) |

El tipo se deduce del valor. También se puede anotar: `CONFIG puerto : ENTERO = 8080`.
Un `DECIMAL` anotado acepta un entero y lo escribe con punto (`30.0`) para conservar el tipo.

## Tokens

| Token | Ejemplos |
|---|---|
| `TOKEN_FORMATO` / `TOKEN_NOMBRE_FORMATO` | FORMATO / JSON, XML |
| `TOKEN_CONFIG` | CONFIG |
| `TOKEN_SECCION` / `TOKEN_FINSECCION` | SECCION / FINSECCION |
| `TOKEN_TIPO` | TEXTO, ENTERO, DECIMAL, BOOLEANO, LISTA |
| `TOKEN_CLAVE` | nombre, puerto, servidor |
| `TOKEN_ASIGNACION` / `TOKEN_DOS_PUNTOS` | = / : |
| `TOKEN_TEXTO` | "Nexo" |
| `TOKEN_ENTERO` / `TOKEN_DECIMAL` | 3100 / 1.5 |
| `TOKEN_BOOLEANO` | verdadero, falso |
| `TOKEN_CORCHETE_ABRE` / `TOKEN_CORCHETE_CIERRA` | [ / ] |
| `TOKEN_COMA` / `TOKEN_FIN` | , / ; |

## Gramática (BNF)

```
<archivo>  ::= <formato> { <elemento> }
<formato>  ::= FORMATO ( JSON | XML )
<elemento> ::= <config> | <seccion>
<config>   ::= CONFIG CLAVE [ ":" TIPO ] "=" <valor> [ ";" ]
<seccion>  ::= SECCION CLAVE { <elemento> } FINSECCION
<valor>    ::= TEXTO | ENTERO | DECIMAL | BOOLEANO | <lista>
<lista>    ::= "[" [ <simple> { "," <simple> } ] "]"
<simple>   ::= TEXTO | ENTERO | DECIMAL | BOOLEANO
```

El analizador sintáctico es descendente recursivo. Las secciones se anidan con una pila: cada
`SECCION` se apila y cada `FINSECCION` la cierra.

## Fases

| Fase | ¿Implementada? | Archivo |
|---|---|---|
| 1. Análisis léxico | Sí | `Lexico/AnalizadorLexico.cs` |
| 2. Análisis sintáctico (árbol de secciones y claves) | Sí | `Sintactico/AnalizadorSintactico.cs` |
| 3. Análisis semántico + tabla de símbolos (rutas como `servidor.puerto`) | Sí | `Semantico/AnalizadorSemantico.cs` |
| 4. Generación de JSON / XML | Sí | `Generacion/GeneradorDocumento.cs` |
| 5. Resultado: guarda el archivo y lo vuelve a leer con `System.Text.Json` / `System.Xml.Linq` | Sí | `Generacion/Ejecutor.cs` |
| Programa C# que lee la configuración, compilado con .NET | Sí (opción 6 del menú) | `Generacion/GeneradorCSharp.cs` |

Validaciones semánticas: clave repetida en la misma sección, valor que no coincide con el tipo anotado,
listas vacías o que mezclan tipos (`ENTERO` y `DECIMAL` se combinan como `DECIMAL`), enteros fuera del
rango de `int`, decimales demasiado grandes y, en XML, nombres que empiezan con `xml`. Avisa también
de secciones vacías.

## Cómo ejecutarlo

Requisitos: .NET 8 SDK y Visual Studio 2022 (o VS Code con la extensión C#).

- **Visual Studio:** abrir `CompiladorConfiguracion.sln` y presionar F5.
- **Consola:**
  ```
  cd CompiladorConfiguracion
  dotnet run                                   # menú interactivo
  dotnet run -- ejemplos/correcto_json.cfg     # compilar un archivo directo
  ```

Menú: 1) ejemplo correcto JSON · 2) ejemplo correcto XML · 3) ejemplo con errores ·
4) escribir configuración · 5) abrir un archivo · 6) compilar y ejecutar con .NET el programa que lee
la última configuración.

El documento generado se guarda en `bin/Debug/net8.0/salida/configuracion.json` (o `.xml`).

## Pruebas

**Caso correcto** (`ejemplos/correcto_json.cfg` y `ejemplos/correcto_xml.cfg`). Lo que imprime el
programa C# generado al leer el archivo (igual para JSON y XML):
```
Configuración leída de configuracion.json:
  nombre = Nexo
  version = 1.5
  puerto = 3100
  activo = True
  idiomas = [es, en]
  servidor.host = localhost
  servidor.puerto = 8080
  servidor.tiempo_espera = 30
  servidor.seguridad.https = False
  servidor.seguridad.puertos_permitidos = [80, 443]
  mensaje = Bienvenido a "Nexo"
```

**Casos incorrectos** (`ejemplos/con_errores.cfg`):

| Entrada | Mensaje |
|---|---|
| `CONFIG nombre = Nexo` | Error sintáctico: "Nexo" no es un valor: los textos van entre comillas. |
| `CONFIG puerto = 8080` (repetida) | Error semántico: La clave "puerto" ya existe en el nivel principal (línea 4). |
| `config activo = verdadero` | Error sintáctico: "config" no se reconoce como palabra clave. → CONFIG |
| `CONFIG version : ENTERO = 1.5` | Error semántico: La clave "version" se declaró como ENTERO y su valor es DECIMAL. |
| `CONFIG idiomas = ["es", 2]` | Error semántico: La lista "idiomas" mezcla TEXTO y ENTERO. |
| `CONFIG ruta = 'C:/datos'` | Error léxico: Los textos van entre comillas dobles, no simples. |
| `CONFIG vacia = []` | Error semántico: La lista "vacia" está vacía y no se puede saber de qué tipo es. |
| `CONFIG limite = 99999999999` | Error semántico: El número 99999999999 no cabe en un ENTERO. |
| `puerto = 5000` | Error sintáctico: Falta CONFIG antes de "puerto". |
| `SECCION servidor` sin `FINSECCION` | Error sintáctico: La sección "servidor" no se cerró. |

## Estructura

```
11-configuracion/
├── CompiladorConfiguracion.sln
├── README.md
└── CompiladorConfiguracion/
    ├── CompiladorConfiguracion.csproj
    ├── Program.cs              # menú y salida por fases
    ├── Compilador.cs           # une las fases
    ├── ErrorCompilacion.cs
    ├── Pantalla.cs             # tablas y colores en consola
    ├── Lexico/                 # Token.cs, AnalizadorLexico.cs
    ├── Sintactico/             # Arbol.cs, AnalizadorSintactico.cs
    ├── Semantico/              # AnalizadorSemantico.cs
    ├── Generacion/             # GeneradorDocumento.cs, GeneradorCSharp.cs, Ejecutor.cs
    └── ejemplos/               # correcto_json.cfg, correcto_xml.cfg, con_errores.cfg
```
