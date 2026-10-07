'use client';
import {useState} from 'react';
import {Download} from 'lucide-react';
import type {ProjectResult} from './project-compiler';

function saveFile(filename:string,content:string){
 const url=URL.createObjectURL(new Blob([content],{type:'text/plain;charset=utf-8'}));
 const link=document.createElement('a');link.href=url;link.download=filename;link.click();
 setTimeout(()=>URL.revokeObjectURL(url),1000);
}
export function ProjectDownloads({result}:{result:ProjectResult}){
 return <div className="project-downloads"><button onClick={()=>saveFile(result.filename,result.file)}><Download size={14}/> Descargar {result.filename}</button>{result.project&&<button onClick={()=>saveFile('Nexo.csproj',result.project!)}><Download size={14}/> Descargar proyecto .NET</button>}</div>;
}
export function ProjectOutput({result}:{result:ProjectResult}){
 const [message,setMessage]=useState('');
 if(!result.preview)return <pre><span className="terminal-prompt">❯ </span>{result.output}</pre>;
 return <div className="form-preview"><div className="form-preview-title">{result.preview.title}<small>VISTA PREVIA</small></div><div className="form-preview-controls">{result.preview.controls.map((control,n)=>control.type==='label'?<p key={n}>{control.text}</p>:<button key={n} onClick={()=>setMessage(control.message||'Este botón todavía no tiene un mensaje asociado.')}>{control.text}</button>)}</div>{message&&<div className="form-preview-message" role="status">{message}<button onClick={()=>setMessage('')}>Cerrar</button></div>}<small>El proyecto WinForms se ejecuta en Windows.</small></div>;
}
