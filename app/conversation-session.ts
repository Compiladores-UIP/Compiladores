'use client';
import {useSyncExternalStore} from 'react';
import {conversationMemoryKey,decodeConversationMemory,encodeConversationMemory,type ConversationTurn} from './conversation-memory';

type Session={turns:ConversationTurn[];input:string;busy:boolean;error:string;notice:string;phase:'interpret'|'retrieve';copied:number|null};
const initial:Session={turns:[],input:'',busy:false,error:'',notice:'',phase:'interpret',copied:null};
let session=initial,hydrated=false;
const listeners=new Set<()=>void>();
type Update<T>=T|((previous:T)=>T);
function set<K extends keyof Session>(key:K,value:Update<Session[K]>){
 const next=typeof value==='function'?(value as (previous:Session[K])=>Session[K])(session[key]):value;
 session={...session,[key]:next};
 if(key==='turns'&&hydrated){try{if(session.turns.length)localStorage.setItem(conversationMemoryKey,encodeConversationMemory(session.turns));else localStorage.removeItem(conversationMemoryKey)}catch{session={...session,notice:'No se pudo guardar el historial en este navegador.'}}}
 listeners.forEach(listener=>listener());
}
export function hydrateConversation(){
 if(hydrated)return;hydrated=true;
 try{const turns=decodeConversationMemory(localStorage.getItem(conversationMemoryKey));const last=turns.at(-1);if(last?.role==='user'){turns.push({role:'assistant',content:'Esta pregunta quedó pendiente cuando se recargó la página. Puedes enviarla otra vez para continuar.'})}set('turns',turns)}catch{set('notice','Tu navegador no permite guardar esta conversación.')}
}
export const setTurns=(value:Update<Session['turns']>)=>set('turns',value);
export const setInput=(value:string)=>set('input',value);
export const setBusy=(value:boolean)=>set('busy',value);
export const setError=(value:string)=>set('error',value);
export const setNotice=(value:string)=>set('notice',value);
export const setPhase=(value:Session['phase'])=>set('phase',value);
export const setCopied=(value:number|null)=>set('copied',value);
export function useConversationSession(){return useSyncExternalStore(listener=>{listeners.add(listener);return()=>listeners.delete(listener)},()=>session,()=>initial)}
