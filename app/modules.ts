export const modules = [
 {
  "name": "Hola Mundo",
  "topic": "Una instrucción. Tu primer programa.",
  "desc": "Reconoce IMPRIMIR y convierte el mensaje en Console.WriteLine.",
  "symbol": "{}",
  "color": "cyan",
  "code": "IMPRIMIR \"Hola Mundo\"",
  "lesson": "Reconoce IMPRIMIR y convierte el mensaje en Console.WriteLine.",
  "question": "¿Qué produce la traducción de IMPRIMIR?",
  "answers": [
   "Console.WriteLine",
   "Una consulta SQL",
   "Un formulario"
  ],
  "correct": 0,
  "level": "Básico",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Operaciones matemáticas",
  "topic": "El orden cambia el resultado.",
  "desc": "Construye un árbol de expresiones respetando la precedencia.",
  "symbol": "+ ×",
  "color": "green",
  "code": "10 + 5 * 2",
  "lesson": "Construye un árbol de expresiones respetando la precedencia.",
  "question": "¿Cuánto vale 10 + 5 * 2?",
  "answers": [
   "30",
   "20",
   "25"
  ],
  "correct": 1,
  "level": "Básico",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Variables",
  "topic": "Un nombre para cada dato.",
  "desc": "Relaciona declaraciones, tipos y valores antes de generar C#.",
  "symbol": "x =",
  "color": "violet",
  "code": "ENTERO edad = 20\nIMPRIMIR edad",
  "lesson": "Relaciona declaraciones, tipos y valores antes de generar C#.",
  "question": "¿Qué tipo C# corresponde a ENTERO?",
  "answers": [
   "string",
   "bool",
   "int"
  ],
  "correct": 2,
  "level": "Básico",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Calculadora",
  "topic": "Cuatro operaciones. Un mismo lenguaje.",
  "desc": "Genera un programa de consola con suma, resta, multiplicación y división.",
  "symbol": "÷",
  "color": "orange",
  "code": "DECIMAL a = 10\nDECIMAL b = 5\nIMPRIMIR a + b\nIMPRIMIR a - b\nIMPRIMIR a * b\nIMPRIMIR a / b",
  "lesson": "Genera un programa de consola con suma, resta, multiplicación y división.",
  "question": "¿Qué operación debe comprobar un divisor distinto de cero?",
  "answers": [
   "Suma",
   "División",
   "Multiplicación"
  ],
  "correct": 1,
  "level": "Básico",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Mensajes personalizados",
  "topic": "Haz que el programa te hable.",
  "desc": "Une textos y variables para construir mensajes personalizados.",
  "symbol": "Aa",
  "color": "blue",
  "code": "TEXTO nombre = \"Ana\"\nMOSTRAR \"Bienvenido, \" + nombre",
  "lesson": "Une textos y variables para construir mensajes personalizados.",
  "question": "¿Qué operador une texto y una variable?",
  "answers": [
   "+",
   "/",
   ">="
  ],
  "correct": 0,
  "level": "Básico",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Estructuras IF",
  "topic": "Una pregunta. Dos caminos.",
  "desc": "Convierte SI, ENTONCES y SINO en bloques if y else.",
  "symbol": "?",
  "color": "yellow",
  "code": "ENTERO edad = 20\nSI edad >= 18 ENTONCES\n  MOSTRAR \"Mayor de edad\"\nSINO\n  MOSTRAR \"Menor de edad\"\nFINSI",
  "lesson": "Convierte SI, ENTONCES y SINO en bloques if y else.",
  "question": "Si edad vale 16, ¿qué rama se ejecuta?",
  "answers": [
   "SI",
   "SINO",
   "Las dos"
  ],
  "correct": 1,
  "level": "Intermedio",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Ciclos",
  "topic": "Reconoce patrones que se repiten.",
  "desc": "Traduce MIENTRAS, PARA y REPETIR a estructuras de repetición.",
  "symbol": "↻",
  "color": "cyan",
  "code": "ENTERO i = 1\nMIENTRAS i <= 3 HACER\n  IMPRIMIR i\n  i = i + 1\nFINMIENTRAS",
  "lesson": "Traduce MIENTRAS, PARA y REPETIR a estructuras de repetición.",
  "question": "¿Cuántas veces se ejecuta REPETIR antes de comprobar su condición?",
  "answers": [
   "Al menos una",
   "Ninguna",
   "Siempre diez"
  ],
  "correct": 0,
  "level": "Intermedio",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Pseudocódigo a C#",
  "topic": "De un algoritmo a un proyecto.",
  "desc": "Combina declaraciones, decisiones y ciclos del pseudocódigo académico.",
  "symbol": "</>",
  "color": "green",
  "code": "ENTERO puntos = 3\nMIENTRAS puntos < 5 HACER\n  puntos = puntos + 1\nFINMIENTRAS\nSI puntos >= 5 ENTONCES\n  MOSTRAR \"Objetivo alcanzado\"\nFINSI",
  "lesson": "Combina declaraciones, decisiones y ciclos del pseudocódigo académico.",
  "question": "¿Desde qué estructura se genera el código destino?",
  "answers": [
   "Desde el árbol de sintaxis",
   "Desde una imagen",
   "Desde la salida"
  ],
  "correct": 0,
  "level": "Intermedio",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Lenguaje para formularios",
  "topic": "Instrucciones que toman forma.",
  "desc": "Genera una aplicación Windows con ventana, etiquetas y botones.",
  "symbol": "▣",
  "color": "violet",
  "code": "VENTANA \"Mi primera ventana\"\nETIQUETA \"Escribe tu nombre\"\nBOTON \"Saludar\" MENSAJE \"Hola desde Nexo\"",
  "lesson": "Genera una aplicación Windows con ventana, etiquetas y botones.",
  "question": "¿Dónde se ejecuta el proyecto WinForms generado?",
  "answers": [
   "En cualquier navegador",
   "En Windows con .NET",
   "En una base de datos"
  ],
  "correct": 1,
  "level": "Intermedio",
  "target": "WinForms",
  "artifact": ".cs + proyecto .NET"
 },
 {
  "name": "Consultas simples",
  "topic": "Pregunta a los datos.",
  "desc": "Convierte una consulta del lenguaje educativo a SELECT y WHERE.",
  "symbol": "⌕",
  "color": "orange",
  "code": "BUSCAR estudiante DONDE edad > 18",
  "lesson": "Convierte una consulta del lenguaje educativo a SELECT y WHERE.",
  "question": "¿Qué cláusula SQL filtra las filas?",
  "answers": [
   "SELECT",
   "WHERE",
   "ORDER BY"
  ],
  "correct": 1,
  "level": "Intermedio",
  "target": "SQL",
  "artifact": "SQL"
 },
 {
  "name": "Lenguaje de configuración",
  "topic": "Texto que configura una aplicación.",
  "desc": "Valida claves y valores y genera documentos JSON o XML.",
  "symbol": "⚙",
  "color": "blue",
  "code": "FORMATO JSON\nCONFIG nombre = \"Nexo\"\nCONFIG puerto = 3100\nCONFIG activo = verdadero",
  "lesson": "Valida claves y valores y genera documentos JSON o XML.",
  "question": "¿Qué valor representa un booleano en JSON?",
  "answers": [
   "\"verdadero\"",
   "true",
   "1.0"
  ],
  "correct": 1,
  "level": "Intermedio",
  "target": "JSON/XML",
  "artifact": "JSON/XML"
 },
 {
  "name": "Mini lenguaje completo",
  "topic": "Las piezas trabajan juntas.",
  "desc": "Integra variables, operadores, decisiones, ciclos e impresión en un compilador pequeño.",
  "symbol": "{}",
  "color": "yellow",
  "code": "ENTERO total = 0\nPARA i = 1 HASTA 5 HACER\n  total = total + i\nFINPARA\nSI total >= 15 ENTONCES\n  MOSTRAR \"Total: \" + total\nSINO\n  MOSTRAR \"Revisa el cálculo\"\nFINSI",
  "lesson": "Integra variables, operadores, decisiones, ciclos e impresión en un compilador pequeño.",
  "question": "¿Qué evita que un ciclo bloquee el laboratorio?",
  "answers": [
   "El límite de pasos",
   "El color del editor",
   "El nombre del archivo"
  ],
  "correct": 0,
  "level": "Avanzado",
  "target": "C#",
  "artifact": ".cs + proyecto .NET"
 }
];
export const slugs = ["hola-mundo", "operaciones", "variables", "calculadora", "mensajes-personalizados", "condicionales", "ciclos", "pseudocodigo-csharp", "formularios", "consultas-simples", "configuracion", "mini-lenguaje"];
