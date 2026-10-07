import {searchDocuments,keywordSearch} from './learning-search.ts';
export const navigationCatalog=[
 {id:'inicio',title:'Inicio',url:'/',description:'Presentación de Nexo y ruta de 12 compiladores.'},
 {id:'modulos',title:'Todos los módulos',url:'/#modulos',description:'Catálogo de lecciones desde básico hasta avanzado.'},
 {id:'laboratorio',title:'Laboratorio paso a paso',url:'/#laboratorio',description:'Ejemplos editables con tokens, árbol, código generado y resultado.'},
 {id:'metodo',title:'Cómo aprender con Nexo',url:'/#metodo',description:'El método de aprendizaje de la web.'},
 {id:'csharp',title:'Compilador C# online',url:'/compilador',description:'Compilar C# completo, recibir salida y diagnósticos, usar entrada de consola.'},
 ...searchDocuments.map(doc=>({id:`mod-${doc.index+1}`,title:doc.title,url:doc.url,description:doc.description})),
];
export type NavigationPlan={message:string;query:string;routes:string[];action:string;question:string};
const allowedActions=new Set(['none','run-example','pause-example','stage-1','stage-2','stage-3','stage-4']);
export function groundNavigationPlan(plan:NavigationPlan,request:string):NavigationPlan{
 const text=request.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'');
 if(/c\s*#|csharp/.test(text)&&/ejecut|compil|editor|probar/.test(text)&&!/(traduc|generar|pseudocodigo)/.test(text))return {...plan,message:'Abre el compilador C# para editar tu programa, ejecutarlo y ver su salida.',routes:['csharp'],query:'',question:'',action:'none'};
 const hits=keywordSearch(request),primary=hits[0];
 if(primary&&primary.score>=3&&/calculadora|\bjson\b|\bxml\b|\bsql\b|winforms|pseudocodigo|hola mundo|variables|condiciones|ciclos|bucles|formularios/.test(text)){const routes=[`mod-${primary.index+1}`,...plan.routes].filter((id,n,all)=>all.indexOf(id)===n).slice(0,3);return {...plan,routes,message:plan.routes[0]===routes[0]?plan.message:`Para tu idea, empieza por ${searchDocuments[primary.index].title}. Puedes aplicar ese concepto a tu proyecto.`}}
 return plan;
}
export function validateNavigationPlan(raw:string):NavigationPlan{
 const start=raw.indexOf('{'),end=raw.lastIndexOf('}');
 if(start<0||end<start)throw new Error('El modelo no devolvió una propuesta de navegación válida.');
 const value=JSON.parse(raw.slice(start,end+1));
 if(!value||typeof value!=='object')throw new Error('Propuesta de navegación vacía.');
 const text=(key:string,max:number)=>typeof value[key]==='string'?value[key].slice(0,max):'';
 return {message:text('message',1500),query:text('query',250),question:text('question',250),routes:Array.isArray(value.routes)?[...new Set(value.routes.filter((id:unknown)=>typeof id==='string'&&navigationCatalog.some(route=>route.id===id)))].slice(0,3) as string[]:[],action:allowedActions.has(value.action)?value.action:'none'};
}
export function navigationPrompt(){return `Eres la guía de Nexo, una web para aprender programación. Responde en español con un JSON corto. Relaciona las ideas del usuario con estos temas disponibles:
${navigationCatalog.map(route=>`${route.id}: ${route.title}`).join('\n')}
Usa solo esos identificadores en routes. Si no hay un tema relacionado, routes=[] y pide una aclaración. Usa el historial para entender "eso". No inventes funciones ni páginas. action="none" siempre, salvo que pida controlar el ejemplo actual: run-example, pause-example, stage-1=tokens, stage-2=árbol, stage-3=C# generado, stage-4=salida.
Ejemplo usuario: Quiero hacer un videojuego con puntos y vidas.
Respuesta: {"message":"Empieza guardando puntos en variables y usando condiciones para las vidas.","query":"variables condiciones","routes":["mod-3","mod-6"],"action":"none","question":""}
Ejemplo usuario: Dónde ejecuto C#?
Respuesta: {"message":"Abre el compilador para editar y ejecutar tu programa.","query":"C#","routes":["csharp"],"action":"none","question":""}
Devuelve solo el JSON con las claves message, query, routes, action, question.`}
