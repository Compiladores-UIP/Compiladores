'use client';
import {useEffect,useRef,useState} from 'react';
import GuideLink from './guide-link';
import dynamic from 'next/dynamic';
const ConversationNavigator=dynamic(()=>import('./conversation-navigator'),{ssr:false});
import {usePathname} from 'next/navigation';
import {ArrowRight,Compass,LoaderCircle,Search,X,Play,Pause} from 'lucide-react';
import {searchDocuments,type SearchHit} from './learning-search';
import {useLearningEngine,activateSemantic,stopSemantic,findLessons} from './learning-engine';
import {performGuideAction,type GuideAction} from './learning-actions';

export default function GuidePanel({onClose,active}:{onClose:()=>void;active:boolean}){
 const pathname=usePathname(),lesson=pathname.startsWith('/aprender/'),engine=useLearningEngine(),status=engine.status,loadingText=engine.progress;
 const [query,setQuery]=useState(''),[hits,setHits]=useState<SearchHit[]>([]),[searched,setSearched]=useState(false),[searching,setSearching]=useState(false),[acting,setActing]=useState(false),[notice,setNotice]=useState(''),[error,setError]=useState(''),[canPause,setCanPause]=useState(false),[lastSearch,setLastSearch]=useState(''),[mode,setMode]=useState<'search'|'conversation'>('conversation');
 const lastQuery=useRef(''),request=useRef(0),field=useRef<HTMLInputElement>(null),mounted=useRef(true);
 useEffect(()=>{mounted.current=true;field.current?.focus({preventScroll:true});return()=>{mounted.current=false}},[]);
 useEffect(()=>{if(!active)return;field.current?.focus({preventScroll:true});const key=(event:KeyboardEvent)=>{if(event.key==='Escape'){event.preventDefault();onClose()}};document.addEventListener('keydown',key);return()=>document.removeEventListener('keydown',key)},[onClose,active]);
 useEffect(()=>{setNotice('');setError('')},[pathname]);
 useEffect(()=>{if(!active||!lesson)return;const example=document.querySelector('.course-example');if(!example)return;const update=()=>setCanPause(!!example.querySelector('[data-nexo-action="pause-example"]'));const observer=new MutationObserver(update);observer.observe(example,{subtree:true,childList:true,attributes:true,attributeFilter:['data-nexo-action']});update();return()=>observer.disconnect()},[active,lesson,pathname]);
 useEffect(()=>{
  if(status!=='ready'||!lastQuery.current)return;
  const id=++request.current;setSearching(true);
  void findLessons(lastQuery.current).then(results=>{if(!mounted.current||id!==request.current)return;setHits(results);setSearching(false)});
 },[status]);
 function enableSemantic(){setError('');activateSemantic()}
 function cancelLoading(){stopSemantic();setSearching(false);setNotice('Descarga detenida. Puedes buscar por temas.');request.current++}
 async function guide(action:GuideAction){
  setActing(true);setError('');setNotice('');
  try{
   if(action==='run-example'||action.startsWith('stage-')&&!document.querySelector('.course-example .journey-controls')){
    if(!document.querySelector('.course-example textarea')){await performGuideAction('run-example');await new Promise<void>(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>resolve())))}
    await performGuideAction('run-example');
    await new Promise<void>(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>resolve())));
   }
   if(action!=='run-example')await performGuideAction(action);
   if(mounted.current)setNotice(action==='run-example'?'Recorrido iniciado. Sigue las cinco etapas del ejemplo.':action==='pause-example'?'Recorrido pausado. Puedes revisar la etapa actual.':`Etapa abierta: ${action==='stage-1'?'tokens':action==='stage-2'?'árbol de sintaxis':action==='stage-3'?'código generado':'resultado'}.`);
  }catch(e){if(mounted.current)setError(e instanceof Error?e.message:'No se pudo iniciar el recorrido.')}
  finally{if(mounted.current)setActing(false)}
 }
 function search(text=query){
  if(!text.trim())return;setQuery(text);setError('');setNotice('');
  const normalized=text.toLowerCase();
  if(lesson){
   const action:GuideAction|undefined=/\b(ejecuta|ejecutar|inicia|repite)\b/.test(normalized)?'run-example':/\b(tokens|léxico|lexico)\b/.test(normalized)?'stage-1':/\b(árbol|arbol|ast)\b/.test(normalized)?'stage-2':/\b(generado|traducción|traduccion)\b/.test(normalized)?'stage-3':/\b(resultado|salida)\b/.test(normalized)?'stage-4':/\b(pausa|pausar)\b/.test(normalized)?'pause-example':undefined;
   if(action&&/\b(ejecuta|ejecutar|inicia|repite|muestra|mostrar|abre|abrir|pausa|pausar)\b/.test(normalized)){void guide(action);return}
  }
  setSearched(true);setLastSearch(text);lastQuery.current=text;if(status==='basic')activateSemantic();
  const id=++request.current;setSearching(true);void findLessons(text).then(results=>{if(!mounted.current||id!==request.current)return;setHits(results);setSearching(false)});
 }
 return <aside id="guide-panel" className="guide-panel" role="dialog" aria-label="Guía Nexo"><header><div><Compass size={23}/><div><strong>Encuentra tu próximo paso.</strong><small>NEXO / GUÍA DE APRENDIZAJE</small></div></div><button aria-label="Cerrar guía" onClick={onClose}><X size={18}/></button></header><div className="guide-content"><div className="guide-mode-tabs"><button aria-pressed={mode==='search'} onClick={()=>setMode('search')}>Explorar</button><button aria-pressed={mode==='conversation'} onClick={()=>setMode('conversation')}>Mi idea</button></div><div hidden={mode!=='conversation'}><ConversationNavigator onClose={onClose}/></div><div hidden={mode!=='search'}><p>Cuéntame qué quieres construir y encuentra una lección para empezar.</p><form onSubmit={e=>{e.preventDefault();search()}}><label htmlFor="guide-query">¿Qué quieres aprender?</label><div className="guide-query"><input ref={field} id="guide-query" value={query} maxLength={250} placeholder="Quiero crear ventanas con botones" onChange={e=>{setQuery(e.target.value);request.current++;setSearching(false);setSearched(false);setHits([])}}/><button type="submit" disabled={searching||acting||!query.trim()} aria-label="Buscar lección">{searching?<LoaderCircle className="guide-spin" size={18}/>:<Search size={18}/>}</button></div></form><div className="guide-suggestions">{['Repetir instrucciones','Filtrar datos','Crear ventanas'].map(text=><button key={text} disabled={acting} onClick={()=>search(text)}>{text}</button>)}</div><div className="guide-status" role="status">{notice}{searching&&(status==='ready'?' Buscando por significado…':' Buscando lecciones…')}{lastSearch&&status==='loading'&&' Mostramos coincidencias por tema mientras se prepara la búsqueda por significado.'}</div>{(error||engine.error)&&<p className="guide-error" role="alert">{error||engine.error}</p>}{searched&&!searching&&<div className="guide-results">{hits.length>0&&<p className="guide-recommendation">{status==='ready'?"Para esa idea, empieza por esta lección:":"Estos temas coinciden con tu búsqueda:"}</p>}{hits.length?hits.map(hit=>{const doc=searchDocuments[hit.index];return <GuideLink href={doc.url} key={doc.index} onClose={onClose}><small>{doc.level} · {doc.target}</small><strong>{doc.title}<ArrowRight size={16}/></strong><p>{doc.description}</p></GuideLink>}):<p>No encontré una lección relacionada en esta ruta de compiladores. Prueba con una idea concreta: crear una calculadora, repetir instrucciones o guardar ajustes.</p>}</div>}{lesson&&<section className="guide-lesson"><small>EXPLORA ESTE EJEMPLO</small><button disabled={acting} onClick={()=>void guide('run-example')}><Play size={14}/> Ejecutar paso a paso</button><div>{(['stage-1','stage-2','stage-3','stage-4'] as GuideAction[]).map((action,n)=><button disabled={acting} key={action} onClick={()=>void guide(action)}>{['Tokens','Árbol','Generado','Resultado'][n]}</button>)}</div><button disabled={acting||!canPause} onClick={()=>void guide('pause-example')}><Pause size={14}/> Pausar recorrido</button></section>}<div className="guide-search-state" role="status"><span className="tiny-dot"/>{status==='ready'?'12 módulos. Encuentra el que conecta con tu idea.':status==='loading'?'Preparando la búsqueda. Ya puedes explorar por tema.':'Explora la ruta de compiladores.'}{engine.error&&<button onClick={enableSemantic}>Reintentar</button>}</div></div></div></aside>;
}
