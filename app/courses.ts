export const courses = [
 {
  "title": "Compilador Hola Mundo",
  "subtitle": "Una instrucción. Tu primer programa.",
  "introduction": [
   "Reconoce IMPRIMIR y convierte el mensaje en Console.WriteLine.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "IMPRIMIR \"Hola Mundo\"",
    "Envía el texto a la consola."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "IMPRIMIR \"Hola Mundo\"",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Cambia el texto por tu nombre. Conserva las comillas.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "",
  "after": " \"Hola Mundo\"",
  "choices": [
   "IMPRIMIR",
   "ENTERO",
   "SI"
  ],
  "solution": "IMPRIMIR",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Cambia el texto por tu nombre. Conserva las comillas."
 },
 {
  "title": "Compilador de Operaciones matemáticas",
  "subtitle": "El orden cambia el resultado.",
  "introduction": [
   "Construye un árbol de expresiones respetando la precedencia.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "10 + 5 * 2",
    "Multiplica antes de sumar: el resultado es 20."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "10 + 5 * 2",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Agrupa (10 + 5) con paréntesis y compara el árbol.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "10 + 5 ",
  "after": " 2",
  "choices": [
   "+",
   "*",
   "-"
  ],
  "solution": "*",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Agrupa (10 + 5) con paréntesis y compara el árbol."
 },
 {
  "title": "Compilador de Variables",
  "subtitle": "Un nombre para cada dato.",
  "introduction": [
   "Relaciona declaraciones, tipos y valores antes de generar C#.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "ENTERO edad = 20",
    "Declara un entero con un valor inicial."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "ENTERO edad = 20\nIMPRIMIR edad",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Declara TEXTO nombre = \"Nexo\" y muestra nombre y edad.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "",
  "after": " edad = 20",
  "choices": [
   "ENTERO",
   "SI",
   "MOSTRAR"
  ],
  "solution": "ENTERO",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Declara TEXTO nombre = \"Nexo\" y muestra nombre y edad."
 },
 {
  "title": "Compilador de Calculadora",
  "subtitle": "Cuatro operaciones. Un mismo lenguaje.",
  "introduction": [
   "Genera un programa de consola con suma, resta, multiplicación y división.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "IMPRIMIR a / b",
    "Divide los valores; el divisor no puede ser cero."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "DECIMAL a = 10\nDECIMAL b = 5\nIMPRIMIR a + b\nIMPRIMIR a - b\nIMPRIMIR a * b\nIMPRIMIR a / b",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Prueba a = 12 y b = 4. Después prueba b = 0 y revisa el error.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "IMPRIMIR a ",
  "after": " b",
  "choices": [
   "+",
   "/",
   "*"
  ],
  "solution": "/",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Prueba a = 12 y b = 4. Después prueba b = 0 y revisa el error."
 },
 {
  "title": "Compilador de Mensajes personalizados",
  "subtitle": "Haz que el programa te hable.",
  "introduction": [
   "Une textos y variables para construir mensajes personalizados.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "MOSTRAR \"Bienvenido\"",
    "Produce una instrucción de salida en C#."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "TEXTO nombre = \"Ana\"\nMOSTRAR \"Bienvenido, \" + nombre",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Cambia nombre y agrega un segundo mensaje con MOSTRAR.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "",
  "after": " \"Bienvenido\"",
  "choices": [
   "MOSTRAR",
   "MIENTRAS",
   "ENTERO"
  ],
  "solution": "MOSTRAR",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Cambia nombre y agrega un segundo mensaje con MOSTRAR."
 },
 {
  "title": "Compilador de Estructuras IF",
  "subtitle": "Una pregunta. Dos caminos.",
  "introduction": [
   "Convierte SI, ENTONCES y SINO en bloques if y else.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "SI edad >= 18 ENTONCES",
    "Abre un bloque condicionado; FINSI lo cierra."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "ENTERO edad = 20\nSI edad >= 18 ENTONCES\n  MOSTRAR \"Mayor de edad\"\nSINO\n  MOSTRAR \"Menor de edad\"\nFINSI",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Cambia edad a 16. Compara la salida y el C# generado.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "SI edad ",
  "after": " 18 ENTONCES",
  "choices": [
   ">=",
   "+",
   "*"
  ],
  "solution": ">=",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Cambia edad a 16. Compara la salida y el C# generado."
 },
 {
  "title": "Compilador de Ciclos",
  "subtitle": "Reconoce patrones que se repiten.",
  "introduction": [
   "Traduce MIENTRAS, PARA y REPETIR a estructuras de repetición.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "PARA i = 1 HASTA 3 HACER",
    "Crea un contador entero; FINPARA termina el bloque."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "ENTERO i = 1\nMIENTRAS i <= 3 HACER\n  IMPRIMIR i\n  i = i + 1\nFINMIENTRAS",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Prueba PARA j = 1 HASTA 3 HACER, IMPRIMIR j y FINPARA, en líneas distintas.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "i = i ",
  "after": " 1",
  "choices": [
   "-",
   "+",
   "/"
  ],
  "solution": "+",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Prueba PARA j = 1 HASTA 3 HACER, IMPRIMIR j y FINPARA, en líneas distintas."
 },
 {
  "title": "Compilador de Pseudocódigo a C#",
  "subtitle": "De un algoritmo a un proyecto.",
  "introduction": [
   "Combina declaraciones, decisiones y ciclos del pseudocódigo académico.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "FINSI / FINMIENTRAS",
    "Cierran estructuras y delimitan el alcance de las variables."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "ENTERO puntos = 3\nMIENTRAS puntos < 5 HACER\n  puntos = puntos + 1\nFINMIENTRAS\nSI puntos >= 5 ENTONCES\n  MOSTRAR \"Objetivo alcanzado\"\nFINSI",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Agrega una variable y úsala en la condición. Descarga Program.cs y el proyecto.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "Console.",
  "after": "(\"Objetivo alcanzado\");",
  "choices": [
   "ReadLine",
   "WriteLine",
   "Parse"
  ],
  "solution": "WriteLine",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Agrega una variable y úsala en la condición. Descarga Program.cs y el proyecto."
 },
 {
  "title": "Compilador de Lenguaje para formularios",
  "subtitle": "Instrucciones que toman forma.",
  "introduction": [
   "Genera una aplicación Windows con ventana, etiquetas y botones.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción.",
   "La vista previa del navegador muestra los controles. La aplicación WinForms se compila y ejecuta en Windows con .NET; el navegador no ejecuta el archivo .exe."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "BOTON \"Saludar\" MENSAJE \"Hola\"",
    "Crea un botón y su manejador de clic."
   ],
   [
    "Una instrucción por línea",
    "Separa cada componente del documento."
   ]
  ],
  "source": "VENTANA \"Mi primera ventana\"\nETIQUETA \"Escribe tu nombre\"\nBOTON \"Saludar\" MENSAJE \"Hola desde Nexo\"",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Agrega otra etiqueta y otro botón con su propio mensaje.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce WinForms a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "",
  "after": " \"Escribe tu nombre\"",
  "choices": [
   "ETIQUETA",
   "ENTERO",
   "BUSCAR"
  ],
  "solution": "ETIQUETA",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Agrega otra etiqueta y otro botón con su propio mensaje."
 },
 {
  "title": "Compilador de Consultas simples",
  "subtitle": "Pregunta a los datos.",
  "introduction": [
   "Convierte una consulta del lenguaje educativo a SELECT y WHERE.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción.",
   "La vista previa filtra una tabla de estudiantes de ejemplo en el navegador. El archivo SQL generado puede ejecutarse en tu base de datos."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "BUSCAR estudiante DONDE edad > 18",
    "Selecciona filas de la tabla estudiante que cumplen la comparación."
   ],
   [
    "Una instrucción por línea",
    "Separa cada componente del documento."
   ]
  ],
  "source": "BUSCAR estudiante DONDE edad > 18",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Cambia > por >= o busca nombre = \"Ana\" y observa los datos de ejemplo.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce SQL a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "BUSCAR estudiante ",
  "after": " edad > 18",
  "choices": [
   "DONDE",
   "HACER",
   "ENTONCES"
  ],
  "solution": "DONDE",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido.",
  "mission": "Cambia > por >= o busca nombre = \"Ana\" y observa los datos de ejemplo."
 },
 {
  "title": "Compilador de Lenguaje de configuración",
  "subtitle": "Texto que configura una aplicación.",
  "introduction": [
   "Valida claves y valores y genera documentos JSON o XML.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción.",
   "Cada clave debe ser única. Los valores admitidos son texto, número y booleano; la generación conserva sus tipos."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "FORMATO JSON / FORMATO XML",
    "Elige el formato del archivo que vas a generar."
   ],
   [
    "Una instrucción por línea",
    "Separa cada componente del documento."
   ]
  ],
  "source": "FORMATO JSON\nCONFIG nombre = \"Nexo\"\nCONFIG puerto = 3100\nCONFIG activo = verdadero",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Cambia FORMATO JSON por FORMATO XML y compara los archivos.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce JSON/XML a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "FORMATO ",
  "after": "",
  "choices": [
   "JSON",
   "SI",
   "PARA"
  ],
  "solution": "JSON",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido.",
  "mission": "Cambia FORMATO JSON por FORMATO XML y compara los archivos."
 },
 {
  "title": "Compilador de Mini lenguaje completo",
  "subtitle": "Las piezas trabajan juntas.",
  "introduction": [
   "Integra variables, operadores, decisiones, ciclos e impresión en un compilador pequeño.",
   "El recorrido separa cinco etapas: código fuente, tokens, estructura, generación y resultado. Puedes pausar cada etapa para comparar el programa con su traducción."
  ],
  "syntaxTitle": "Las reglas de este lenguaje",
  "syntax": [
   [
    "REPETIR ... HASTA condicion",
    "Ejecuta el cuerpo una vez y lo repite hasta cumplir la condición."
   ],
   [
    "IMPRIMIR / MOSTRAR",
    "Escriben un valor en la salida."
   ]
  ],
  "source": "ENTERO total = 0\nPARA i = 1 HASTA 5 HACER\n  total = total + i\nFINPARA\nSI total >= 15 ENTONCES\n  MOSTRAR \"Total: \" + total\nSINO\n  MOSTRAR \"Revisa el cálculo\"\nFINSI",
  "explain": "Ejecuta el ejemplo y sigue las cinco etapas. Construye una suma de 1 a 10 y muestra un mensaje según el resultado.",
  "connection": "El analizador reconoce las instrucciones de este módulo y construye su estructura. El generador produce C# a partir de esa estructura, conservando los textos y valores.",
  "challenge": "Completa la instrucción con la pieza correcta.",
  "before": "PARA i = 1 ",
  "after": " 5 HACER",
  "choices": [
   "HASTA",
   "DONDE",
   "SINO"
  ],
  "solution": "HASTA",
  "tip": "Escribe una instrucción por línea. Las palabras del lenguaje se escriben en mayúsculas; las cadenas mantienen su contenido. Descarga Program.cs y Nexo.csproj en la misma carpeta. Usa dotnet build; para producir un ejecutable de Windows, usa dotnet publish -r win-x64 --self-contained true.",
  "mission": "Construye una suma de 1 a 10 y muestra un mensaje según el resultado."
 }
];
