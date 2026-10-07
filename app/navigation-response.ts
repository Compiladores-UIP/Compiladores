import {keywordSearch,searchDocuments} from './learning-search.ts';
import {groundNavigationPlan,type NavigationPlan} from './navigation-catalog.ts';
import {decideIntent,isBeginnerRequest} from './intent-router.ts';

export function beginnerResponse(request:string):NavigationPlan|null{
 const text=request.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'');
 if(!isBeginnerRequest(text))return null;
 return {message:'Empieza por Hola Mundo: aprenderás a escribir una instrucción y ver su resultado. Después sigue con Variables para guardar datos y Operaciones para hacer cálculos. En cada lección, cambia el ejemplo y ejecútalo paso a paso: verás cómo pasa del texto al código C#.',query:'',routes:['mod-1','mod-3','mod-2'],action:'none',question:'¿Quieres empezar mostrando un mensaje o prefieres construir una calculadora?'};
}
export function contextualLearningResponse(request:string,history:{role:string;content:string;plan?:NavigationPlan}[]):NavigationPlan|null{
 const text=request.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'').trim();
 const previous=[...history].reverse().find(turn=>turn.role==='assistant'&&turn.plan?.routes.length);
 const beginner=isBeginnerRequest(request);
 const clarification=/^(?:no entiendo(?: nada| eso)?|no se hacerlo|como (?:empiezo|lo hago)|explicamelo(?: mas facil)?|paso a paso|ayudame)(?:[.!?])?$/.test(text);
 if(!beginner&&!(clarification&&previous))return null;
 const goal=previous?.plan?.routes[0];
 const doc=searchDocuments.find(doc=>`mod-${doc.index+1}`===goal);
 const objective=doc&&goal!=='mod-1'?`Podemos seguir con ${doc.title}, pero empezaremos por una instrucción sencilla. `:'';
 return {message:`No necesitas saber programar todavía. ${objective}Vamos a mostrar un mensaje.\n\n1. Abre Hola Mundo.\n2. Busca IMPRIMIR "Hola Mundo": muestra el texto entre comillas.\n3. Cambia "Hola Mundo" por tu nombre y pulsa Ejecutar código.\n\nVerás la traducción a Console.WriteLine y tu mensaje en la salida. Después añadiremos ${goal==='mod-4'?'los números y las operaciones de tu calculadora':'variables y operaciones'}, uno a la vez.`,routes:['mod-1'],query:'',action:'none',question:'¿Quieres que te explique qué hace esa primera línea?'};
}
export function catalogResponse(request:string):NavigationPlan{
 const direct=decideIntent(request).answer;if(direct)return direct;
 const beginner=beginnerResponse(request);if(beginner)return beginner;
 const hits=keywordSearch(request);
 const plan:NavigationPlan={message:hits.length?`Puedes empezar por ${searchDocuments[hits[0].index].title}. ${searchDocuments[hits[0].index].description} Abre la lección, modifica su ejemplo y ejecútalo para observar cada etapa.`:'Nexo enseña a construir compiladores. Puedo ayudarte con mensajes, cálculos, variables, decisiones, ciclos, formularios, SQL y configuración.',query:'',routes:hits.map(hit=>`mod-${hit.index+1}`),action:'none',question:hits.length?'':'¿Qué quieres que haga tu programa? Dame un ejemplo de su entrada y el resultado que esperas.'};
 return groundNavigationPlan(plan,request);
}
export function lessonResponse(indices:number[]):NavigationPlan{
 const docs=[...new Set(indices)].map(index=>searchDocuments[index]).filter(Boolean).slice(0,3);
 return {message:docs.map((doc,n)=>`${n===0?'Empieza por':'Después puedes explorar'} ${doc.title}: ${doc.description}`).join('\n\n')+'\n\nAbre la lección y cambia su ejemplo para ver cómo funciona paso a paso.',routes:docs.map(doc=>`mod-${doc.index+1}`),query:'',action:'none',question:''};
}
export function fastResponse(request:string):NavigationPlan|null{
 const beginner=beginnerResponse(request);if(beginner)return beginner;
 const text=request.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'');
 if(/c\s*#|csharp/.test(text)&&/ejecut|compil|editor|probar/.test(text)&&!/(traduc|generar|pseudocodigo)/.test(text))return catalogResponse(request);
 const topics:[number,RegExp][]=[
  [3,/calculadora/],[6,/repetir|repit[ae]|repite|bucle|ciclo|\bwhile\b|\bfor\b/],
  [9,/\bsql\b|consulta|filtrar|registros|base de datos/],[10,/\bjson\b|\bxml\b|configuraci[oó]n|preferencias|ajustes/],
  [8,/ventana|boton|formulario|winforms/],[5,/condicion|decisi[oó]n|\bif\b|mayor de edad|vidas/],
  [2,/variable|tipos de datos|guardar (?:un )?(?:numero|texto|puntos)/],[7,/pseudocodigo|traducir.*c#/],
  [11,/crear.*(?:lenguaje|compilador completo)|mini lenguaje/],[1,/operacion|precedencia|parentesis|sumar|multiplicar|dividir/],
  [4,/saludar|bienvenida|mensaje personalizado/],[0,/hola mundo|imprimir|mostrar un mensaje/],
 ];
 const matches=topics.filter(([,pattern])=>pattern.test(text)).map(([index])=>index);
 if(!matches.length)return null;
 return lessonResponse(matches);
}
