'use client';
import {useSyncExternalStore} from 'react';
import type {NavigationPlan} from './navigation-catalog';
type ChatMessage={role:'user'|'assistant';content:string};
type State={status:'idle'|'loading'|'ready';progress:string;percentage:number|null;error:string;backend?:string;threads?:number;metrics?:{firstTokenMs:number;totalMs:number}};
const initial:State={status:'idle',progress:'',percentage:null,error:''};
let state=initial,worker:Worker|null=null,id=0;
const listeners=new Set<()=>void>(),pending=new Map<number,{resolve:(value:NavigationPlan)=>void;reject:(error:Error)=>void;timer:ReturnType<typeof setTimeout>;onPartial?:(text:string)=>void}>();
const readinessWaiters=new Set<{resolve:()=>void;reject:(error:Error)=>void}>();
function publish(value:State){state=value;listeners.forEach(listener=>listener())}
export function useConversation(){return useSyncExternalStore(listener=>{listeners.add(listener);return()=>listeners.delete(listener)},()=>state,()=>initial)}
export function stopConversation(){readinessWaiters.forEach(request=>request.reject(new Error('Conversación detenida.')));readinessWaiters.clear();worker?.terminate();worker=null;pending.forEach(request=>{clearTimeout(request.timer);request.reject(new Error('Conversación detenida.'))});pending.clear();publish(initial)}
export function cancelConversationResponse(){worker?.postMessage({type:'cancel'});readinessWaiters.forEach(request=>request.reject(new Error('Respuesta cancelada.')));readinessWaiters.clear();pending.forEach(request=>{clearTimeout(request.timer);request.reject(new Error('Respuesta cancelada.'))});pending.clear()}
function waitUntilReady():Promise<void>{
 if(state.status==='ready'&&worker)return Promise.resolve();
 if(state.status==='idle')activateConversation();
 return new Promise((resolve,reject)=>{
  const cleanup=()=>{clearTimeout(timer);readinessWaiters.delete(request);listeners.delete(check)};
  const request={resolve:()=>{cleanup();resolve()},reject:(error:Error)=>{cleanup();reject(error)}};
  const check=()=>{if(state.status==='ready'&&worker)request.resolve();else if(state.error)request.reject(new Error(state.error))};
  const timer=setTimeout(()=>request.reject(new Error('La guía todavía se está preparando.')),900000);
  readinessWaiters.add(request);listeners.add(check);check();
 });
}
export function activateConversation(){
 if(state.status!=='idle')return;
 publish({status:'loading',progress:'Abriendo la guía…',percentage:null,error:''});
 const downloads=new Map<string,{loaded:number;total:number}>();
 try{
  const current=new Worker(new URL('./conversation-worker.ts',import.meta.url),{type:'module'});worker=current;
  current.onmessage=event=>{
   if(worker!==current)return;const data=event.data;
   if(data.type==='backend')publish({...state,progress:'Buscando los archivos guardados…',backend:data.device,threads:data.threads});
   if(data.type==='progress'){
    if(data.status==='progress'&&typeof data.file==='string'&&data.total>0){
     downloads.set(data.file,{loaded:Math.min(data.loaded,data.total),total:data.total});
     const values=[...downloads.values()],loaded=values.reduce((sum,file)=>sum+file.loaded,0),total=values.reduce((sum,file)=>sum+file.total,0);
     const percent=Math.min(99,Math.floor(100*loaded/total));
     publish({...state,percentage:percent,progress:`Cargando la guía · ${percent}% · ${Math.round(loaded/1048576)} / ${Math.round(total/1048576)} MB`});
    }else if(data.status==='download')publish({...state,progress:'Comprobando archivos guardados y preparando la guía…'});
    else if(data.status==='done')publish({...state,progress:'Preparando la conversación…'});
   }
   if(data.type==='ready')publish({...state,status:'ready',progress:'',percentage:100,error:''});
   if(data.type==='partial'||data.type==='heartbeat'){const request=pending.get(data.id);if(request){clearTimeout(request.timer);request.timer=setTimeout(()=>{pending.delete(data.id);stopConversation();activateConversation();request.reject(new Error('La respuesta se detuvo.'))},20000);if(data.type==='partial')request.onPartial?.(data.text)}}
   if(data.type==='answer'){const request=pending.get(data.id);if(request){clearTimeout(request.timer);request.resolve(data.plan);pending.delete(data.id);if(data.metrics)publish({...state,metrics:data.metrics})}}
   if(data.type==='error'){const request=pending.get(data.id);if(request){clearTimeout(request.timer);request.reject(new Error('No pude completar esta respuesta. Puedes volver a intentarlo.'));pending.delete(data.id)}else if(state.status==='loading'){stopConversation();publish({...initial,error:'No se pudo cargar el modelo conversacional. La búsqueda de lecciones sigue disponible.'})}}
  };
  current.onerror=()=>{stopConversation();publish({...initial,error:'Este navegador no pudo ejecutar el modelo conversacional. Puedes seguir usando la búsqueda de lecciones.'})};
  current.postMessage({type:'load',id:++id});
 }catch{publish({...initial,error:'No se pudo iniciar el modelo conversacional.'})}
}
export async function converse(message:string,history:ChatMessage[],location:string,kind:'navigation'|'conversation'='navigation',onPartial?:(text:string)=>void,candidates:string[]=[]):Promise<NavigationPlan>{
 await waitUntilReady();
 const requestId=++id;
 return new Promise((resolve,reject)=>{
  const timer=setTimeout(()=>{pending.delete(requestId);stopConversation();activateConversation();reject(new Error('El modelo tardó demasiado.'))},state.backend==='webgpu'?30000:60000);
  pending.set(requestId,{resolve,reject,timer,onPartial});worker!.postMessage({type:'chat',id:requestId,message,history,location,kind,candidates});
 });
}

