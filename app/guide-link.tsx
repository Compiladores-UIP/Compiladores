'use client';
import Link from 'next/link';
import {usePathname} from 'next/navigation';
import {useEffect,useRef,useState,type ReactNode} from 'react';

// Mantiene el panel abierto al navegar y recupera los enlaces si la navegación se atasca.
export default function GuideLink({href,onClose,children,className}:{href:string;onClose:()=>void;children:ReactNode;className?:string}){
 const pathname=usePathname(),[opening,setOpening]=useState(false);
 const timer=useRef<ReturnType<typeof setTimeout>|null>(null),origin=useRef(pathname),close=useRef(onClose);
 close.current=onClose;
 useEffect(()=>{if(pathname!==origin.current){if(timer.current)clearTimeout(timer.current);setOpening(false);origin.current=pathname;close.current()}},[pathname]);
 useEffect(()=>()=>{if(timer.current)clearTimeout(timer.current)},[]);
 return <Link href={href} className={className} aria-busy={opening} onClick={event=>{
  if(event.defaultPrevented||event.button!==0||event.metaKey||event.ctrlKey||event.shiftKey||event.altKey)return;
  const destination=new URL(href,window.location.origin);
  if(destination.pathname===window.location.pathname){close.current();return}
  setOpening(true);
  if(timer.current)clearTimeout(timer.current);
  timer.current=setTimeout(()=>{if(window.location.pathname!==destination.pathname)window.location.assign(destination.href)},8000);
 }}>{children}{opening&&<small role="status">Abriendo lección…</small>}</Link>;
}
