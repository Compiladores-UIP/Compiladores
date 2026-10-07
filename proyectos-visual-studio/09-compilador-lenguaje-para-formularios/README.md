# 09 - Compilador de lenguaje para formularios

## Descripción

Este mini-compilador interpreta un lenguaje sencillo para describir formularios y genera una aplicación Windows Forms en C#.

El usuario puede escribir instrucciones como:

```text
FORMULARIO "Registro"
ETIQUETA "Nombre"
CAMPO nombre
ETIQUETA "Correo"
CAMPO correo
BOTON "Guardar"
FIN_FORMULARIO
```

El programa realiza análisis léxico, análisis sintáctico y una validación semántica básica. Después genera código C#, compila ese código y permite abrir la aplicación Windows Forms resultante.

Cuando el formulario contiene `BOTON "Guardar"`, el botón generado permite almacenar los valores escritos en los campos en un archivo CSV local.

## Objetivo

Representar de forma visible el proceso:

```text
Lenguaje fuente
↓
Análisis léxico
↓
Análisis sintáctico
↓
Validación semántica básica
↓
Generación de C#
↓
Compilación
↓
Aplicación Windows Forms
↓
Ingreso y guardado local de datos
```

## Nivel

Intermedio.

## Producto esperado

Aplicación Windows Forms generada a partir del lenguaje fuente escrito por el usuario.

El formulario no está definido de forma fija. El título, las etiquetas, los campos y los botones dependen de las instrucciones proporcionadas.

## Tecnologías

- C#.
- .NET 8.
- Windows Forms.
- UTF-8.
- `System.Diagnostics.Process` para compilar el código C# generado.
- Archivo CSV para el guardado local de los datos del formulario.

## Requisitos

- Windows 10 u 11.
- .NET 8 SDK.
- Visual Studio 2022 con la carga de trabajo de desarrollo de escritorio de .NET, o una terminal con acceso al comando `dotnet`.

## Cómo ejecutar

### Desde Visual Studio 2022

1. Abrir `CompiladoresGrupo1.sln`.
2. Seleccionar `MiniCompiladorFormularios`.
3. Establecerlo como proyecto de inicio.
4. Ejecutar con `F5` o `Ctrl + F5`.

### Desde PowerShell

Desde la raíz del repositorio:

```powershell
dotnet run --project ".\compiladores\09-compilador-lenguaje-para-formularios\MiniCompiladorFormularios.csproj"
```

## Interfaz del mini-compilador

La ventana principal contiene cinco acciones:

| Acción | Función |
|---|---|
| `Cargar ejemplo` | Inserta un ejemplo válido en el área de código fuente. |
| `Analizar` | Ejecuta el análisis léxico y sintáctico, muestra los tokens y genera el C# si la entrada es válida. |
| `Generar formulario` | Analiza, genera el C# y compila una aplicación Windows Forms. |
| `Abrir formulario generado` | Ejecuta la aplicación Windows Forms generada previamente. |
| `Limpiar` | Borra la entrada y los resultados de la interfaz. |

La interfaz también muestra:

- código fuente;
- tabla de tokens;
- código C# generado;
- resultado del análisis o compilación;
- estado actual del proceso.

## Lenguaje fuente

La primera instrucción debe definir el formulario:

```text
FORMULARIO "Título"
```

Después pueden utilizarse etiquetas, campos y botones:

```text
ETIQUETA "Texto"
CAMPO identificador
BOTON "Texto"
```

El programa debe finalizar con:

```text
FIN_FORMULARIO
```

## Palabras reservadas

| Palabra | Función |
|---|---|
| `FORMULARIO` | Inicia la definición y establece el título de la ventana. |
| `ETIQUETA` | Crea una etiqueta visible. |
| `CAMPO` | Crea un cuadro de texto identificado por un nombre. |
| `BOTON` | Crea un botón con el texto indicado. |
| `FIN_FORMULARIO` | Finaliza la definición del formulario. |

## Tokens reconocidos

| Tipo de token | Uso |
|---|---|
| `Formulario` | Palabra reservada `FORMULARIO`. |
| `Etiqueta` | Palabra reservada `ETIQUETA`. |
| `Campo` | Palabra reservada `CAMPO`. |
| `Boton` | Palabra reservada `BOTON`. |
| `FinFormulario` | Palabra reservada `FIN_FORMULARIO`. |
| `Cadena` | Texto escrito entre comillas dobles. |
| `Identificador` | Nombre utilizado para identificar un campo. |
| `FinArchivo` | Marca interna que indica el final de la entrada. |

Ejemplo:

```text
FORMULARIO "Registro"
ETIQUETA "Nombre"
CAMPO nombre
BOTON "Guardar"
FIN_FORMULARIO
```

produce unidades equivalentes a:

| Entrada | Tipo |
|---|---|
| `FORMULARIO` | `Formulario` |
| `Registro` | `Cadena` |
| `ETIQUETA` | `Etiqueta` |
| `Nombre` | `Cadena` |
| `CAMPO` | `Campo` |
| `nombre` | `Identificador` |
| `BOTON` | `Boton` |
| `Guardar` | `Cadena` |
| `FIN_FORMULARIO` | `FinFormulario` |

## Análisis sintáctico

El parser exige estas reglas:

1. `FORMULARIO` debe aparecer primero.
2. `FORMULARIO` debe ir seguido de un título entre comillas.
3. `ETIQUETA` debe ir seguida de una cadena.
4. `CAMPO` debe ir seguido de un identificador.
5. `BOTON` debe ir seguido de una cadena.
6. La definición debe terminar con `FIN_FORMULARIO`.
7. No se acepta contenido adicional después de `FIN_FORMULARIO`.

## Validación semántica básica

Los identificadores de los campos no pueden repetirse dentro del mismo formulario.

Ejemplo inválido:

```text
FORMULARIO "Registro"
CAMPO nombre
CAMPO nombre
FIN_FORMULARIO
```

El programa informa que el campo `nombre` está repetido.

## Generación de código C#

Cada instrucción se transforma en un control real de Windows Forms:

| Lenguaje fuente | C# generado |
|---|---|
| `FORMULARIO "Registro"` | `Form` con `Text = "Registro"`. |
| `ETIQUETA "Nombre"` | `Label`. |
| `CAMPO nombre` | `TextBox` identificado como `nombre`. |
| `BOTON "Guardar"` | `Button` con evento de guardado. |

El código generado se muestra con saltos de línea e indentación para facilitar su revisión.

## Funcionamiento del botón Guardar

La instrucción:

```text
BOTON "Guardar"
```

genera un botón funcional.

Al presionarlo:

1. se comprueba que todos los campos tengan información;
2. si algún campo está vacío, se muestra una advertencia y no se guarda el registro;
3. si todos los campos tienen información, se crea o actualiza un archivo `registros.csv`;
4. la primera vez se escriben los nombres de los campos como encabezados;
5. cada guardado posterior agrega una nueva fila;
6. se muestra una confirmación con la ruta exacta del archivo;
7. los campos se limpian para permitir un nuevo registro.

Los datos se guardan dentro de la carpeta Documentos del usuario actual de Windows:

```text
Documentos\MiniCompiladoresUIP\Formularios\registros.csv
```

El archivo se escribe en UTF-8 y utiliza punto y coma (`;`) como separador.

Ejemplo:

```text
nombre;correo
Ana Pérez;ana@ejemplo.com
```

## Cómo localizar y comprobar el archivo guardado

La aplicación muestra la ruta exacta después de guardar. Si se cerró ese mensaje o se desea verificar manualmente, desde PowerShell se puede ejecutar:

```powershell
$archivo = Join-Path ([Environment]::GetFolderPath("MyDocuments")) "MiniCompiladoresUIP\Formularios\registros.csv"
Write-Host $archivo
```

Para comprobar que el archivo existe:

```powershell
Test-Path $archivo
```

El resultado esperado es:

```text
True
```

Para ver los registros guardados:

```powershell
Get-Content $archivo
```

Para abrir directamente la carpeta donde se encuentra el CSV:

```powershell
explorer (Split-Path $archivo)
```

Estas comprobaciones no son necesarias para utilizar la aplicación; sirven para verificar físicamente que la persistencia local funciona.

## Comprobación rápida del proyecto

Desde la raíz del repositorio puede verificarse que el proyecto compila con:

```powershell
dotnet build ".\compiladores\09-compilador-lenguaje-para-formularios\MiniCompiladorFormularios.csproj" -c Release
```

El resultado esperado es una compilación sin errores.

Después puede ejecutarse con:

```powershell
dotnet run --project ".\compiladores\09-compilador-lenguaje-para-formularios\MiniCompiladorFormularios.csproj"
```

Secuencia recomendada para la prueba funcional:

```text
Cargar ejemplo
→ Analizar
→ Generar formulario
→ Abrir formulario generado
→ completar Nombre y Correo
→ Guardar
```

Al finalizar deben cumplirse estas condiciones:

- el análisis es correcto;
- el código C# generado es visible y legible;
- la compilación del formulario generado termina correctamente;
- el formulario se abre;
- el botón `Guardar` valida campos vacíos;
- se crea `registros.csv`;
- el registro aparece al consultar el archivo.

## Caso válido

Entrada:

```text
FORMULARIO "Registro"
ETIQUETA "Nombre"
CAMPO nombre
ETIQUETA "Correo"
CAMPO correo
BOTON "Guardar"
FIN_FORMULARIO
```

Proceso esperado:

1. `Analizar` muestra los tokens y el mensaje `El formulario es válido.`.
2. En `Código C# generado` aparece un programa Windows Forms legible.
3. `Generar formulario` compila el programa.
4. El resultado muestra:

```text
Compilación correcta.
0 advertencias
0 errores
```

5. `Abrir formulario generado` abre una ventana titulada `Registro`.
6. El usuario escribe valores en `Nombre` y `Correo`.
7. Al pulsar `Guardar`, se crea o actualiza `registros.csv`.
8. La aplicación muestra `Datos guardados correctamente.` y la ruta del archivo.

Ejemplo de contenido:

```text
nombre;correo
Ana Pérez;ana@ejemplo.com
```

## Caso inválido: campo vacío al guardar

Si el usuario abre el formulario generado e intenta pulsar `Guardar` dejando uno o más campos vacíos, aparece:

```text
Complete todos los campos antes de guardar.
```

No se agrega ninguna fila al archivo.

## Caso inválido: CAMPO sin identificador

Entrada:

```text
FORMULARIO "Registro"
CAMPO
FIN_FORMULARIO
```

Mensaje esperado:

```text
Error sintáctico: falta el identificador del campo.
```

## Caso inválido: falta FIN_FORMULARIO

Entrada:

```text
FORMULARIO "Registro"
ETIQUETA "Nombre"
CAMPO nombre
```

Mensaje esperado:

```text
Error sintáctico: falta FIN_FORMULARIO.
```

## Caso inválido: cadena sin cerrar

Entrada:

```text
FORMULARIO "Registro
FIN_FORMULARIO
```

El analizador léxico informa que la cadena no tiene comillas de cierre e indica la línea donde se encontró el problema.

## Compilación del formulario generado

Cuando se selecciona `Generar formulario`:

1. el código C# generado se escribe en una carpeta temporal;
2. se crea un proyecto temporal de Windows Forms para .NET 8;
3. se ejecuta `dotnet build`;
4. si no existen errores, se conserva la ruta del ejecutable temporal;
5. el botón `Abrir formulario generado` ejecuta ese archivo.

Los archivos temporales generados por este proceso no forman parte del repositorio.

## Archivos del proyecto

| Archivo | Función |
|---|---|
| `Program.cs` | Contiene lexer, parser, validación semántica, generador de C#, compilación e interfaz del mini-compilador. |
| `MiniCompiladorFormularios.csproj` | Define el proyecto Windows Forms en .NET 8. |
| `README.md` | Documenta sintaxis, tokens, pruebas, errores, generación, guardado y verificación. |

## Integrantes

- Daniela Insturaín
- Aaron Fehrenbach
- Euris J. Rodríguez V.
