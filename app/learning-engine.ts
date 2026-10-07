'use client';
import {useSyncExternalStore} from 'react';
import {keywordSearch,type SearchHit} from './learning-search';

type State={status:'basic'|'loading'|'ready';progress:string;error:string};
const initial:State={status:'basic',progress:'',error:''};
let state=initial,worker:Worker|null=null,nextId=0;
const listeners=new Set<()=>void>();
const pending=new Map<number,{resolve:(hits:SearchHit[])=>void;query:string;timer:ReturnType<typeof setTimeout>}>();
function publish(value:State){state=value;listeners.forEach(listener=>listener())}
function fallback(){pending.forEach(({resolve,query,timer})=>{clearTimeout(timer);resolve(keywordSearch(query))});pending.clear()}
export function useLearningEngine(){return useSyncExternalStore(listener=>{listeners.add(listener);return()=>listeners.delete(listener)},()=>state,()=>initial)}
export function activateSemantic(){
 if(state.status!=='basic')return;
 publish({status:'loading',progress:'Preparando la búsqueda…',error:''});
 try{
  const current=new Worker(new URL('./learning-worker.ts',import.meta.url),{type:'module'});worker=current;
  const fail=()=>{if(worker!==current)return;current.terminate();worker=null;fallback();publish({status:'basic',progress:'',error:'No se pudo cargar el modelo. La búsqueda por temas sigue disponible.'})};
  current.onerror=fail;
  current.onmessage=event=>{
   if(worker!==current)return;
   const data=event.data;
   if(data.type==='progress')publish({...state,progress:`Descargando ${data.file.endsWith('.onnx')?'modelo':'archivos'}… ${Math.round(data.progress||0)}%`});
   if(data.type==='indexing')publish({...state,progress:'Preparando las 12 lecciones…'});
   if(data.type==='ready')publish({status:'ready',progress:'',error:''});
   if(data.type==='error')fail();
   if(data.type==='results'){const request=pending.get(data.id);if(request){clearTimeout(request.timer);request.resolve(data.hits);pending.delete(data.id)}}
  };
  current.postMessage({type:'load',id:++nextId});
 }catch{worker=null;publish({status:'basic',progress:'',error:'Tu navegador no pudo iniciar el modelo. Puedes buscar por temas.'})}
}
export function stopSemantic(){worker?.terminate();worker=null;fallback();publish(initial)}
export function findLessons(query:string):Promise<SearchHit[]>{
 if(state.status!=='ready'||!worker)return Promise.resolve(keywordSearch(query));
 const id=++nextId;
 return new Promise(resolve=>{
  const recover=()=>{const request=pending.get(id);if(request){clearTimeout(request.timer);pending.delete(id);resolve(keywordSearch(query))}};
  const timer=setTimeout(recover,5000);
  pending.set(id,{resolve,query,timer});
  try{worker!.postMessage({type:'search',id,query})}catch{recover()}
 });
}
