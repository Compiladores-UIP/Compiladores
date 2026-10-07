# 10 · Mini-Compilador de Consultas simples

**Universidad Interamericana de Panamá · Compiladores · Portal Nexo**
**Grupo 3:** Ana Vallarino · Josimar Osorio

Mini-compilador que traduce consultas escritas en español a SQL. Comprueba que la tabla y los campos
existan y que cada condición compare tipos compatibles. Después ejecuta la consulta sobre una base de
datos de ejemplo y muestra las filas resultantes.

## Lenguaje fuente

```
BUSCAR estudiante DONDE edad > 18
BUSCAR nombre, carrera DE estudiante DONDE carrera = "Sistemas" Y promedio >= 80
BUSCAR estudiante DONDE edad < 18 O promedio < 70 ORDENAR POR edad DESC
BUSCAR curso
```

Reglas: una consulta por línea, palabras clave en mayúsculas, textos entre comillas dobles,
`;` al final es opcional y `//` inicia un comentario.

| Lenguaje fuente | SQL         |
|-----------------|-------------|
| `BUSCAR`        | `SELECT`    |
| `DE`            | `FROM`      |
| `DONDE`         | `WHERE`     |
| `Y` / `O`       | `AND` / `OR`|
| `ORDENAR POR`   | `ORDER BY`  |
| `ASC` / `DESC`  | `ASC` / `DESC` |
| `"texto"`       | `'texto'`   |
| `>  <  >=  <=  =  <>  !=` | `>  <  >=  <=  =  <>  <>` |

Ejemplo: `BUSCAR estudiante DONDE edad > 18` → `SELECT * FROM estudiante WHERE edad > 18;`

## Tokens

`TOKEN_BUSCAR`, `TOKEN_DE`, `TOKEN_DONDE`, `TOKEN_Y`, `TOKEN_O`, `TOKEN_ORDENAR`, `TOKEN_POR`,
`TOKEN_ASC`, `TOKEN_DESC`, `TOKEN_IDENTIFICADOR` (tablas y campos), `TOKEN_OPERADOR`,
`TOKEN_NUMERO`, `TOKEN_DECIMAL`, `TOKEN_TEXTO`, `TOKEN_COMA`, `TOKEN_FIN`.

```
BUSCAR estudiante DONDE edad > 18
→ TOKEN_BUSCAR(BUSCAR) TOKEN_IDENTIFICADOR(estudiante) TOKEN_DONDE(DONDE)
  TOKEN_IDENTIFICADOR(edad) TOKEN_OPERADOR(>) TOKEN_NUMERO(18)
```

## Gramática (BNF)

```
<consulta>  ::= BUSCAR <origen> [ DONDE <filtro> ] [ ORDENAR POR IDENTIFICADOR [ ASC | DESC ] ] [ ";" ]
<origen>    ::= IDENTIFICADOR | <campos> DE IDENTIFICADOR
<campos>    ::= IDENTIFICADOR { "," IDENTIFICADOR }
<filtro>    ::= <condicion> { ( Y | O ) <condicion> }
<condicion> ::= IDENTIFICADOR <operador> <valor>
<operador>  ::= ">" | "<" | ">=" | "<=" | "=" | "<>" | "!="
<valor>     ::= NUMERO | DECIMAL | TEXTO
```

Como en SQL, `Y` se evalúa antes que `O`.

## Base de datos de ejemplo

**estudiante** (id, nombre, edad, carrera, promedio)

| id | nombre | edad | carrera    | promedio |
|----|--------|------|------------|----------|
| 1  | Ana    | 20   | Sistemas   | 91.5     |
| 2  | Luis   | 17   | Industrial | 78.0     |
| 3  | Marta  | 18   | Sistemas   | 85.0     |
| 4  | Pedro  | 22   | Civil      | 69.5     |
| 5  | Sofía  | 19   | Industrial | 88.0     |
| 6  | Diego  | 21   | Sistemas   | 74.5     |

**curso** (codigo, nombre, creditos, cupos): INF-301 Compiladores, INF-210 Base de Datos,
MAT-101 Cálculo I, INF-150 Programación I.

## Fases

| Fase | ¿Implementada? | Archivo |
|------|----------------|---------|
| 1. Análisis léxico | Sí | `Lexico/AnalizadorLexico.cs` |
| 2. Análisis sintáctico (árbol) | Sí, descendente recursivo | `Sintactico/AnalizadorSintactico.cs` |
| 3. Análisis semántico | Sí: tabla y campos existen, tipos compatibles | `Semantico/AnalizadorSemantico.cs` |
| 4. Generación de código | Sí: SQL, y su equivalente en C# con LINQ | `Generacion/GeneradorSQL.cs` |
| 5. Ejecución / resultado | Sí, sobre la base de datos en memoria | `Generacion/Ejecutor.cs` |
| Ejecución en un gestor SQL real | Opcional: la opción 6 exporta `consultas_generadas.sql` con las tablas y las consultas | `Program.cs` |

## Cómo ejecutarlo

Requisitos: .NET 8 SDK y Visual Studio 2022 (o VS Code con la extensión C#).

- **Visual Studio:** abrir `CompiladorConsultas.sln` y presionar F5.
- **Consola:**
  ```
  cd CompiladorConsultas
  dotnet run                              # menú interactivo
  dotnet run -- ejemplos/correcto.txt     # compilar un archivo directo
  ```

Menú: 1) ejemplo correcto · 2) ejemplo con errores · 3) escribir consultas · 4) abrir .txt ·
5) ver tablas · 6) exportar script SQL.

## Pruebas

**Caso correcto:** `BUSCAR estudiante DONDE edad > 18`
```
SELECT * FROM estudiante WHERE edad > 18;
→ Ana (20), Pedro (22), Sofía (19), Diego (21)   4 fila(s)
```

**Casos incorrectos** (`ejemplos/con_errores.txt`):

| Entrada | Mensaje |
|---------|---------|
| `BUSCAR estudiante DONDE edad >` | Error sintáctico: Falta el valor después de ">". |
| `buscar estudiante DONDE edad > 18` | Error sintáctico: "buscar" no se reconoce como palabra clave. |
| `BUSCAR estudiantes DONDE edad > 18` | Error semántico: La tabla "estudiantes" no existe. ¿Quisiste decir "estudiante"? |
| `BUSCAR estudiante DONDE telefono = "6000-0000"` | Error semántico: La tabla "estudiante" no tiene un campo llamado "telefono". |
| `BUSCAR estudiante DONDE edad > "veinte"` | Error semántico: El campo "edad" es numérico y se compara con un texto. |
| `BUSCAR estudiante DONDE nombre = Ana` | Error sintáctico: Falta el valor después de "=". → escríbelo entre comillas |
| `BUSCAR estudiante DONDE edad # 18` | Error léxico: Símbolo no reconocido: '#'. |

## Estructura

```
10-consultas-simples/
├── CompiladorConsultas.sln
├── README.md
└── CompiladorConsultas/
    ├── CompiladorConsultas.csproj
    ├── Program.cs              # menú y salida por fases
    ├── Compilador.cs           # une las fases
    ├── ErrorCompilacion.cs
    ├── Pantalla.cs             # tablas y colores en consola
    ├── Lexico/                 # Token.cs, AnalizadorLexico.cs
    ├── Sintactico/             # Arbol.cs, AnalizadorSintactico.cs
    ├── Semantico/              # BaseDeDatos.cs, AnalizadorSemantico.cs
    ├── Generacion/             # GeneradorSQL.cs, Ejecutor.cs
    └── ejemplos/               # correcto.txt, con_errores.txt
```
