'use client';
import {useEffect} from 'react';
import {activateSemantic} from './learning-engine';
import {activateConversation} from './conversation-engine';

export default function ModelStartup(){
 useEffect(()=>{
  // El navegador puede rechazar la petición de conservar la caché.
  void navigator.storage?.persist?.().catch(()=>false);
  activateSemantic();
  activateConversation();
 },[]);
 return null;
}
