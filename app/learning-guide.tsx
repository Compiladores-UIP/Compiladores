'use client';
import {useEffect,useRef,useState} from 'react';
import {useConversationSession} from './conversation-session';
import dynamic from 'next/dynamic';
import {Compass} from 'lucide-react';
const GuidePanel=dynamic(()=>import('./learning-guide-panel'),{ssr:false,loading:()=> <div className="guide-loading">Abriendo tu guía…</div>});
export default function LearningGuide(){
 const [open,setOpen]=useState(false),[loaded,setLoaded]=useState(false),[unread,setUnread]=useState(false);
 const {busy,turns}=useConversationSession(),previousBusy=useRef(false);
 useEffect(()=>{if(previousBusy.current&&!busy&&!open&&turns.at(-1)?.role==='assistant')setUnread(true);previousBusy.current=busy;if(open)setUnread(false)},[busy,open,turns]);
 return <div className="learning-guide" data-page-agent-not-interactive="true">{loaded&&<div hidden={!open}><GuidePanel active={open} onClose={()=>{setOpen(false);requestAnimationFrame(()=>document.getElementById('guide-launcher')?.focus())}}/></div>}<button id="guide-launcher" className="guide-launcher" aria-label={open?'Cerrar Guía Nexo':'Abrir Guía Nexo'} aria-expanded={open} aria-controls={open?'guide-panel':undefined} onClick={()=>{setLoaded(true);setOpen(!open)}}><Compass size={20}/><span>{busy?'Nexo está respondiendo…':unread?'Tu respuesta está lista':'Guía Nexo'}</span></button></div>;
}
