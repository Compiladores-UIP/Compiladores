import {modules,slugs} from './modules.ts';
const goals=[
 ['Quiero empezar a programar desde cero y escribir mi primer Hola Mundo.','Quiero imprimir un texto en la consola.'],
 ['Quiero entender el orden de las operaciones matemáticas y los paréntesis.','Quiero evaluar expresiones de suma y multiplicación.'],
 ['Quiero declarar variables para guardar números, textos y otros datos.','Quiero aprender tipos de datos: entero, decimal, texto y booleano.'],
 ['Quiero crear una calculadora que sume, reste, multiplique y divida.','Quiero practicar división y evitar dividir entre cero.'],
 ['Quiero saludar a una persona por su nombre con un mensaje personalizado.','Quiero concatenar cadenas y mostrar un mensaje de bienvenida.'],
 ['Quiero que mi programa tome decisiones según la edad de una persona.','Quiero aprender condiciones SI, if, else y comparaciones para elegir entre dos caminos.'],
 ['Quiero repetir instrucciones varias veces con un contador.','Quiero aprender bucles y ciclos mientras, para y repetir hasta.'],
 ['Quiero traducir pseudocódigo académico a C# y generar un proyecto compilable.','Quiero transformar un algoritmo en código C#.'],
 ['Quiero crear una interfaz gráfica de escritorio con ventanas y botones.','Quiero construir formularios Windows con etiquetas y eventos de clic.'],
 ['Quiero filtrar registros y buscar estudiantes en una base de datos.','Quiero aprender consultas SQL con SELECT y WHERE.'],
 ['Quiero guardar las preferencias y ajustes de mi aplicación.','Quiero generar archivos de configuración JSON o XML con claves y valores.'],
 ['Quiero construir mi propio lenguaje de programación y un compilador completo.','Quiero integrar variables, operadores, decisiones, ciclos e impresión en un proyecto avanzado.'],
];
export const searchDocuments=modules.map((m,index)=>({index,title:m.name,url:`/aprender/${slugs[index]}`,level:m.level,target:m.target,description:m.desc,goals:goals[index],text:`${m.name}. ${goals[index].join(' ')} ${m.desc}`}));
export type SearchHit={index:number;score:number};
export function normalize(text:string){return text.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'')}
const ignored=new Set('quiero como para que con una los las del aprender necesito hacer saber sobre algo puedo me mi mis por desde segun programa programar aplicacion'.split(' '));
function terms(text:string){return [...new Set((normalize(text).match(/[a-z0-9#]+/g)||[]).filter(w=>(w.length>2||['c#','if'].includes(w))&&!ignored.has(w)).map(w=>w.length>5?w.replace(/(?:es|s)$/,''):w))]}
export function keywordSearch(query:string):SearchHit[]{
 const words=terms(query);if(!words.length)return [];
 const docs=searchDocuments.map(doc=>({doc,terms:new Set(terms(doc.text)),title:new Set(terms(doc.title))}));
 const hits=docs.map(({doc,terms:tokens,title})=>({index:doc.index,score:words.reduce((sum,word)=>{if(!tokens.has(word))return sum;const frequency=docs.filter(d=>d.terms.has(word)).length;return sum+Math.log(1+docs.length/frequency)+(title.has(word)?1:0)},0)})).filter(hit=>hit.score>0).sort((a,b)=>b.score-a.score);
 return hits.filter(hit=>hit.score>=hits[0].score*.45).slice(0,3);
}
export function relevantHits(semantic:SearchHit[],query:string):SearchHit[]{
 const keywords=keywordSearch(query),maximum=Math.max(1,...keywords.map(hit=>hit.score));
 const hits=semantic.map(hit=>({...hit,score:hit.score+.12*(keywords.find(k=>k.index===hit.index)?.score||0)/maximum})).sort((a,b)=>b.score-a.score);
 if(!hits.length||hits[0].score<.55)return [];
 return hits.filter(hit=>hit.score>=.55&&hit.score>=hits[0].score-.14).slice(0,3);
}
export function rankEmbeddings(query:number[],documents:number[][],limit=3):SearchHit[]{
 if(!query.length||documents.some(v=>v.length!==query.length))throw new Error('Dimensiones de búsqueda incompatibles.');
 return documents.map((vector,index)=>({index,score:vector.reduce((sum,v,n)=>sum+v*query[n],0)})).sort((a,b)=>b.score-a.score).slice(0,limit);
}
