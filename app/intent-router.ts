import type {NavigationPlan} from './navigation-catalog.ts';

export type Intent='social'|'explanation'|'navigation'|'conversation';
const normalize=(text:string)=>text.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/[¿?¡!.,]/g,'').replace(/\s+/g,' ').trim();
const reply=(message:string):NavigationPlan=>({message,routes:[],query:'',action:'none',question:''});
export function isBeginnerRequest(request:string){return /\b(?:no se (?:nada(?: de)?|programar|hacerlo)|no (?:se|entiendo) (?:de )?programacion|nunca he programado|soy (?:principiante|nuevo)|desde cero|por donde empiezo|primer programa|empezar a programar)\b/.test(normalize(request))}
export function decideIntent(request:string,clock:{now?:Date;timeZone?:string}={}):{intent:Intent;answer:NavigationPlan|null}{
 const text=normalize(request);
 if(isBeginnerRequest(request))return {intent:'navigation',answer:null};
 const timeQuestion=/^(?:(?:hola|nexo|dime|me dices|puedes decirme)\s+)?(?:que hora (?:es|son)|que horas son|cual es la hora|hora actual|what time is it)(?:\s+(?:ahora|por favor))?$/.test(text);
 const dateQuestion=/^(?:que (?:dia|fecha) es(?: hoy)?|que dia estamos|a que fecha estamos|fecha (?:actual|de hoy)|what is todays date)$/.test(text);
 if(timeQuestion||dateQuestion){
  const now=clock.now??new Date(),timeZone=clock.timeZone??Intl.DateTimeFormat().resolvedOptions().timeZone;
  const value=new Intl.DateTimeFormat('es',{timeZone,...(timeQuestion?{hour:'2-digit',minute:'2-digit',hour12:false}:{weekday:'long',day:'numeric',month:'long',year:'numeric'})}).format(now);
  return {intent:'social',answer:reply(timeQuestion?`Son las ${value}, según el reloj de tu dispositivo (${timeZone}).`:`Hoy es ${value}, según el reloj de tu dispositivo (${timeZone}).`)};
 }
 if(/^(?:(?:hola|hey|hello)\s+)?(?:quien eres(?: tu)?|quien sos|que eres|como te llamas|cual es tu nombre|who are you|what is your name)(?:\s+nexo)?$/.test(text))return {intent:'social',answer:reply('Soy Nexo, tu guía de aprendizaje. Puedo explicarte conceptos de programación, ayudarte a encontrar lecciones y acompañarte con los ejemplos de código.')};
 if(/^(hola|hello|hi|hey|buenas|buenos dias|buenas tardes|buenas noches)(\s+(como estas|que tal|nexo))?$/.test(text)||/^(como estas|que tal|how are you)$/.test(text))return {intent:'social',answer:reply(/hello|^hi$|how are/.test(text)?'Hello! I’m here and ready to help. How are you?':'¡Hola! Estoy listo para ayudarte. ¿Cómo estás?')};
 if(/^(gracias|muchas gracias|thanks|thank you|ok gracias)$/.test(text))return {intent:'social',answer:reply('¡De nada! Si quieres, seguimos con lo que estabas haciendo.')};
 if(/^(adios|hasta luego|bye|nos vemos)$/.test(text))return {intent:'social',answer:reply('¡Hasta luego! Puedes retomar esta conversación cuando quieras.')};
 if(/^(bien|muy bien|todo bien|estoy bien|fine|im fine)( y tu)?$/.test(text))return {intent:'social',answer:reply('¡Me alegra! ¿Qué te gustaría hacer hoy?')};
 const definitions:[RegExp,string][]=[
  [/\bint\b.*\bdouble\b|\bdouble\b.*\bint\b/,'int guarda números enteros, como 20, y double guarda números con parte decimal, como 20.5. En C#: int edad = 20; double precio = 20.5;. La división de dos int trunca la parte decimal: 5 / 2 produce 2; 5.0 / 2 produce 2.5.'],
  [/\bvariable\b/,'Una variable es un nombre asociado a un valor que puede cambiar. Por ejemplo, en C#: int edad = 20; crea una variable llamada edad con el valor 20. Después, edad = 21; cambia su valor.'],
  [/\bcompilador\b/,'Un compilador traduce un programa de un lenguaje a otro. Primero reconoce sus palabras y símbolos, después analiza su estructura y finalmente genera el código destino. También informa de los errores que encuentra.'],
  [/\b(?:bucle|ciclo|while)\b/,'Un ciclo repite instrucciones. Un while continúa mientras su condición sea verdadera. Por ejemplo: int i = 0; while (i < 3) { Console.WriteLine(i); i++; } muestra 0, 1 y 2. El incremento permite que el ciclo termine.'],
  [/\b(?:if|condicion)\b/,'Una condición permite elegir qué instrucciones ejecutar. En C#, if (edad >= 18) { Console.WriteLine("Mayor de edad"); } solo muestra el mensaje cuando edad es 18 o más. Puedes añadir else para el caso contrario.'],
  [/\b(?:token|tokens)\b/,'Los tokens son las piezas que reconoce el analizador léxico: palabras clave, nombres, números y símbolos. Por ejemplo, int edad = 20; se divide en int, edad, =, 20 y ;.'],
  [/(?:^|\s)c#(?:\s|$)|csharp/,'C# es un lenguaje de programación de la plataforma .NET. Se usa para aplicaciones de escritorio, servicios web, herramientas y juegos, entre otros. Un ejemplo de salida en consola es Console.WriteLine("Hola");.'],
 ];
 if(/que es|que son|explica|significa|diferencia/.test(text)){const simple=/^(?:que es|que son|explica(?:me)?|que significa) (?:un |una |el |la |los |las )?(?:variable|compilador|bucle|ciclo|while|if|condicion|tokens?|c#|csharp)(?: en (?:c#|programacion))?$/.test(text);const match=definitions.find(([pattern],index)=>pattern.test(text)&&(index===0?/diferencia|int.*double|double.*int/.test(text):simple));return {intent:'explanation',answer:match?reply(match[1]):null}}
 const topic=/juego|videojuego|aplicacion|\bapp\b|pagina web|sitio web|robot|simulador|automatiz|nexo|leccion|modulo|program|hola mundo|imprimir|calculadora|c#|csharp|codigo|sql|json|xml|variable|bucle|ciclo|token|compilador|formulario|ventana|boton|estudiante|repita.*mensaje|filtrar.*(?:datos|registros)|guardar.*(?:ajustes|preferencias)/.test(text);
 if(topic&&/aprender|empiezo|desde cero|primer programa|lleva|donde|abrir|abre|ejecut|quiero|crear|construir|hacer|filtrar|repetir|repita|guardar|calculadora|mostrar|programa debe|traducir/.test(text))return {intent:'navigation',answer:null};
 return {intent:'conversation',answer:null};
}
export const conversationFallback=()=>reply('No pude terminar la respuesta porque la guía no estuvo lista o la generación se interrumpió. Puedes volver a intentarlo; no necesitas reformular tu pregunta.');
