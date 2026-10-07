'use client';
import {useEffect,useRef,useState} from 'react';
import Link from 'next/link';
import {useRouter} from 'next/navigation';
import {ArrowUpRight,BookOpen,LoaderCircle,Search,X} from 'lucide-react';
import {findLessons,activateSemantic,stopSemantic,useLearningEngine} from './learning-engine';
import {searchDocuments,type SearchHit} from './learning-search';

export default function CourseSearch({onResults}:{onResults:(query:string,hits:SearchHit[])=>void}){
 const engine=useLearningEngine(),router=useRouter(),[query,setQuery]=useState(''),[hits,setHits]=useState<SearchHit[]>([]),[open,setOpen]=useState(false),[busy,setBusy]=useState(false),[selected,setSelected]=useState(-1);
 const root=useRef<HTMLDivElement>(null),field=useRef<HTMLInputElement>(null),version=useRef(0),callback=useRef(onResults);callback.current=onResults;
 useEffect(()=>{
  const id=++version.current;setSelected(-1);
  if(!query.trim()){setHits([]);setBusy(false);callback.current('',[]);return}
  setBusy(true);
  const timer=setTimeout(()=>{void findLessons(query).then(results=>{if(id!==version.current)return;setHits(results);setBusy(false);callback.current(query,results)})},220);
  return()=>{clearTimeout(timer);version.current++};
 },[query,engine.status]);
 useEffect(()=>{const outside=(event:PointerEvent)=>{if(!root.current?.contains(event.target as Node))setOpen(false)};document.addEventListener('pointerdown',outside);return()=>document.removeEventListener('pointerdown',outside)},[]);
 const options=query.trim()?hits:[0,5,8].map(index=>({index,score:0}));
 function navigate(index:number){const doc=searchDocuments[index];setOpen(false);router.push(doc.url)}
 return <div className="course-search nav-search" ref={root}><BookOpen size={16}/><input ref={field} role="combobox" aria-label="Buscar módulos" aria-autocomplete="list" aria-expanded={open} aria-controls="course-search-options" aria-activedescendant={open&&selected>=0?`course-option-${options[selected]?.index}`:undefined} placeholder="¿Qué quieres aprender hoy?" value={query} maxLength={250} onFocus={()=>setOpen(true)} onChange={e=>{setQuery(e.target.value);setOpen(true)}} onKeyDown={e=>{
  if(e.key==='Escape'){setOpen(false);e.stopPropagation()}
  if(e.key==='ArrowDown'||e.key==='ArrowUp'){e.preventDefault();setOpen(true);setSelected(n=>options.length?(n+(e.key==='ArrowDown'?1:-1)+options.length)%options.length:-1)}
  if(e.key==='Enter'){e.preventDefault();if(!busy&&options.length)navigate(options[Math.max(0,selected)].index)}
 }}/>{query?<button className="course-search-clear" aria-label="Limpiar búsqueda" onClick={()=>{setQuery('');field.current?.focus()}}><X size={14}/></button>:<kbd>/</kbd>}{open&&<div className="course-search-menu"><div className="course-search-caption"><span>{query.trim()?'LECCIONES PARA TU IDEA':'¿POR DÓNDE EMPEZAMOS?'}</span>{busy?<LoaderCircle size={13} className="guide-spin"/>:<Search size={13}/>}</div><div role="listbox" id="course-search-options" aria-label="Lecciones sugeridas">{options.map((hit,n)=>{const doc=searchDocuments[hit.index];return <Link role="option" aria-selected={selected===n} id={`course-option-${doc.index}`} key={doc.index} href={doc.url} className={selected===n?'search-selected':''} onClick={()=>setOpen(false)}><div><small>{doc.level} · {doc.target}</small><strong>{doc.title}</strong><p>{doc.description}</p></div><ArrowUpRight size={16}/></Link>})}</div>{!busy&&query.trim()&&!hits.length&&<p className="course-search-empty">Prueba una idea como «crear ventanas», «repetir instrucciones» o «guardar ajustes».</p>}<div className="course-search-model">{engine.status==='ready'?<span><i/> Búsqueda por significado activa</span>:engine.status==='loading'?<><span role="status">{engine.progress}</span><button onClick={stopSemantic}>Detener</button></>:<><button onClick={activateSemantic}>Reintentar búsqueda <ArrowUpRight size={12}/></button><small>La búsqueda se prepara automáticamente y guarda el modelo en este navegador.</small></>}{engine.error&&<small role="alert">{engine.error}</small>}</div><div className="course-search-hint" role="status">{busy?'Buscando lecciones…':'↑ ↓ para elegir · Enter para abrir · Esc para cerrar'}</div></div>}</div>;
}
