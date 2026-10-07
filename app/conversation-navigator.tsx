'use client';
import {useEffect,useRef,useState} from 'react';
import GuideLink from './guide-link';
import ChatMessage from './chat-message';
import {usePathname} from 'next/navigation';
import {ArrowRight,LoaderCircle,Send,Square,Check,Copy,MapPin,Trash2} from 'lucide-react';
import {useConversation,activateConversation,stopConversation,cancelConversationResponse,converse} from './conversation-engine';
import {navigationCatalog,type NavigationPlan} from './navigation-catalog';
import {performGuideAction,type GuideAction} from './learning-actions';
import {findLessons} from './learning-engine';
import {searchDocuments} from './learning-search';
import type {ConversationTurn as Turn} from './conversation-memory';
import {useConversationSession,hydrateConversation,setTurns,setInput,setBusy,setError,setNotice,setPhase,setCopied} from './conversation-session';
import {catalogResponse,contextualLearningResponse} from './navigation-response';
import {decideIntent,conversationFallback} from './intent-router';
export default function ConversationNavigator({onClose}:{onClose:()=>void}){
 const engine=useConversation(),pathname=usePathname();
 const {input,turns,busy,error,notice,phase,copied}=useConversationSession();
 const field=useRef<HTMLTextAreaElement>(null);
 const currentPage=navigationCatalog.find(route=>route.url===pathname);
 const [loadingSeconds,setLoadingSeconds]=useState(0);
 useEffect(()=>{if(engine.status==='idle'&&!engine.error)activateConversation()},[]);
 useEffect(()=>{if(engine.status!=='loading'){setLoadingSeconds(0);return}const started=Date.now();const timer=setInterval(()=>setLoadingSeconds(Math.floor((Date.now()-started)/1000)),1000);return()=>clearInterval(timer)},[engine.status]);
 useEffect(()=>{hydrateConversation();field.current?.focus({preventScroll:true})},[]);
 async function send(text=input){
  if(!text.trim()||busy)return;setBusy(true);setPhase('interpret');setError('');setNotice('');setInput('');
  const history=turns.slice(-6).map(turn=>({role:turn.role,content:turn.plan?JSON.stringify(turn.plan):turn.content}));
  setTurns(previous=>[...previous.slice(-14),{role:'user',content:text}]);
  const decision=decideIntent(text);
  let streamed=false;
  const onPartial=(content:string)=>{if(!streamed){streamed=true;setTurns(previous=>[...previous,{role:'assistant',content}])}else setTurns(previous=>[...previous.slice(0,-1),{role:'assistant',content}])};
  try{
   let plan=contextualLearningResponse(text,turns)??decision.answer;
   if(!plan&&decision.intent!=='navigation')plan=await converse(text,history,pathname,'conversation',onPartial);
   if(!plan){
    setPhase('retrieve');
    const hits=await Promise.race([findLessons(text),new Promise<never[]>(resolve=>setTimeout(()=>resolve([]),1200))]);
    const candidates=hits.map(hit=>`mod-${hit.index+1}`);
    if(/c\s*#|csharp/i.test(text)&&/ejecut|compil|editor|probar/i.test(text))candidates.unshift('csharp');
    setPhase('interpret');
    plan=await converse(text,history,pathname,'navigation',onPartial,candidates);
   }
   if(!plan.routes.length&&plan.query){setPhase('retrieve');const results=await Promise.race([findLessons(plan.query),new Promise<never[]>(resolve=>setTimeout(()=>resolve([]),2000))]);plan.routes=results.map(hit=>`mod-${searchDocuments[hit.index].index+1}`)}
   setTurns(previous=>[...(streamed?previous.slice(0,-1):previous),{role:'assistant',content:plan.message||'Estas son las opciones disponibles en Nexo.',plan}]);
  }catch(e){if(e instanceof Error&&e.message==='Respuesta cancelada.'){setNotice('Respuesta detenida. Puedes enviar otra pregunta.');return}if(streamed){setNotice('La respuesta se interrumpió antes de terminar.');return}const plan=decision.intent==='navigation'?catalogResponse(text):conversationFallback();setTurns(previous=>[...previous,{role:'assistant',content:plan.message,plan}]);if(decision.intent==='navigation')setNotice('Te orienté con el contenido de las lecciones disponibles.');}
  finally{setBusy(false)}
 }
 async function action(name:string){
  setError('');setNotice('');setBusy(true);
  try{
   if(!pathname.startsWith('/aprender/'))throw new Error('Abre primero una lección para controlar su ejemplo.');
   if(name==='run-example'||name.startsWith('stage-')&&!document.querySelector('.course-example .journey-controls')){
    if(!document.querySelector('.course-example textarea')){await performGuideAction('run-example');await new Promise<void>(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>resolve())))}
    await performGuideAction('run-example');await new Promise<void>(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>resolve())));
   }
   if(name!=='run-example')await performGuideAction(name as GuideAction);
   setNotice(name==='run-example'?'El ejemplo inició su recorrido paso a paso.':name==='pause-example'?'El recorrido quedó pausado.':'La etapa solicitada está abierta en el ejemplo.');
  }catch(e){setError(e instanceof Error?e.message:'No se pudo activar esa etapa.')}
  finally{setBusy(false)}
 }
 async function copyTurn(turn:Turn,index:number){
  try{await navigator.clipboard.writeText([turn.content,turn.plan?.question,...(turn.plan?.routes||[]).map(id=>{const route=navigationCatalog.find(item=>item.id===id);return route?`${route.title}: ${new URL(route.url,window.location.origin).href}`:''})].filter(Boolean).join('\n'));setCopied(index)}catch{setError('No se pudo copiar la respuesta en este navegador.')}
 }
 function clearConversation(){setTurns([]);setError('');setNotice('Conversación borrada de este navegador.');setCopied(null)}
 return <div className="conversation-navigator">
  {!turns.length&&<div className="conversation-intro"><span className="conversation-eyebrow">DEL «¿Y SI…?» AL CÓDIGO</span><h2>¿Qué tienes<br/>en <em>mente?</em></h2><p>Una calculadora. Un juego. Tu primer programa.<br/>Busquemos por dónde empezar.</p></div>}
  {currentPage&&pathname!=='/'&&<div className="conversation-context"><MapPin size={12}/><span>Trabajando con <strong>{currentPage.title}</strong></span></div>}
  <div className="conversation-readiness" role="status" data-backend={engine.backend} data-threads={engine.threads} data-first-token-ms={engine.metrics?.firstTokenMs} data-total-ms={engine.metrics?.totalMs}><span className={engine.status==='ready'?'tiny-dot':'guide-spin'}>{engine.status!=='ready'&&<LoaderCircle size={12}/>}</span><span>{engine.status==='ready'?'Vamos a darle forma.':engine.status==='loading'?engine.progress:'La guía está detenida.'}</span>{engine.status==='idle'&&<button onClick={activateConversation}>Reintentar</button>}</div>
  {engine.status==='loading'&&<div className="conversation-load-progress">{engine.percentage!==null?<progress aria-label="Carga de archivos de la guía" max={100} value={engine.percentage}/>:<progress aria-label="Abriendo la guía"/>}<small>{loadingSeconds}s{engine.percentage===100?' · Los archivos ya están cargados; falta iniciar la conversación.':engine.percentage===null?' · Comprobando los archivos antes de comenzar.':' · La primera carga puede tardar más.'}</small>{loadingSeconds>=45&&<button onClick={()=>{stopConversation();activateConversation()}}>Reintentar carga</button>}</div>}
  {!turns.length&&<div className="conversation-starters">{[{mark:'01',label:'Empiezo desde cero',text:'No sé nada de programación, ¿por dónde empiezo?',color:'mint'},{mark:'02',label:'Quiero construir algo',text:'Quiero una calculadora para una nave espacial',color:'yellow'},{mark:'03',label:'Voy directo al código',text:'Llévame donde pueda ejecutar C#',color:'cyan'}].map(item=><button key={item.mark} data-color={item.color} onClick={()=>{setInput(item.text);field.current?.focus()}}><span>{item.mark}</span><strong>{item.label}</strong><ArrowRight size={15}/></button>)}</div>}
  <div className="conversation-turns" aria-live="polite">{turns.map((turn,n)=><div className={`conversation-turn ${turn.role}`} key={n}><div className="conversation-turn-heading"><small>{turn.role==='user'?'TU IDEA':'GUÍA NEXO'}</small>{turn.role==='assistant'&&<button className="conversation-copy" aria-label={copied===n?'Respuesta copiada':'Copiar respuesta'} onClick={()=>void copyTurn(turn,n)}>{copied===n?<Check size={13}/>:<Copy size={13}/>}</button>}</div>{turn.role==='assistant'?<ChatMessage content={turn.content}/>:<p>{turn.content}</p>}{turn.plan?.question&&<p className="conversation-question">{turn.plan.question}</p>}{turn.plan?.routes.length?<div className="conversation-route-list"><small>TU SIGUIENTE PASO</small>{turn.plan.routes.map((id,index)=>{const route=navigationCatalog.find(route=>route.id===id),doc=searchDocuments.find(item=>item.url===route?.url);return route&&<GuideLink href={route.url} key={id} onClose={onClose} className={index===0?'conversation-route-primary':''}><span className="conversation-route-number">{doc?String(doc.index+1).padStart(2,'0'):'↗'}</span><div><small>{doc?`${doc.level} · ${doc.target}`:'Explora Nexo'}</small><strong>{route.title}</strong><p>{route.description}</p></div><ArrowRight size={14}/></GuideLink>})}</div>:null}{turn.plan?.action&&turn.plan.action!=='none'&&<button disabled={busy||engine.status!=='ready'} onClick={()=>void action(turn.plan!.action)}>{({'run-example':'Ejecutar paso a paso','pause-example':'Pausar recorrido','stage-1':'Ver tokens','stage-2':'Ver árbol','stage-3':'Ver C# generado','stage-4':'Ver salida'} as Record<string,string>)[turn.plan.action]} <ArrowRight size={13}/></button>}</div>)}</div>
  {busy&&<div className="conversation-working" role="status"><div><LoaderCircle className="guide-spin" size={15}/><strong>{engine.status!=='ready'&&phase==='interpret'?'Esperando a que la guía esté lista':phase==='retrieve'?'Buscando lecciones relacionadas':'Preparando tu respuesta'}</strong><button aria-label="Detener respuesta" onClick={cancelConversationResponse}><Square size={12}/></button></div><p>{engine.status!=='ready'&&phase==='interpret'?'Tu pregunta está guardada. La responderé cuando termine la preparación.':phase==='retrieve'?'Comprobando coincidencias con el catálogo de Nexo.':'La respuesta aparecerá aquí mientras se genera.'}</p></div>}
  {notice&&<div className="conversation-receipt" role="status"><Check size={14}/>{notice}</div>}
  <form onSubmit={e=>{e.preventDefault();void send()}}><label htmlFor="conversation-input">TU IDEA, CON TUS PALABRAS</label><div className="guide-query conversation-composer"><textarea id="conversation-input" ref={field} value={input} maxLength={500} rows={2} placeholder="¿Y si construimos…?" onChange={e=>setInput(e.target.value)} disabled={busy} onKeyDown={e=>{if(e.key==='Enter'&&!e.shiftKey&&!e.nativeEvent.isComposing){e.preventDefault();void send()}}}/><button aria-label="Enviar mensaje" disabled={busy||!input.trim()}><Send size={17}/></button></div><small className="conversation-composer-hint">{engine.status==='ready'?'Enter para enviar · Shift + Enter para otra línea':'Ya puedes consultar. La guía termina de prepararse en segundo plano.'}</small></form>
  <div className="conversation-memory"><span>{turns.length?`${turns.length} mensajes · Retoma cuando quieras`:'Tu próxima idea empieza aquí.'}</span><button className="conversation-clear" disabled={busy||!turns.length} onClick={clearConversation}><Trash2 size={12}/> Borrar conversación</button></div>
  {(error||engine.error)&&<p className="guide-error" role="alert">{error||engine.error}</p>}
 </div>;
}
