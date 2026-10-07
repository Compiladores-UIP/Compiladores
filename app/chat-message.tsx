'use client';
import {useState} from 'react';
import {Check,Copy} from 'lucide-react';
import {SyntaxCode} from './code-editor';
import {chatBlocks} from './chat-format';
function inline(text:string){return text.split(/(`[^`\n]+`|\*\*[^*\n]+\*\*)/g).map((part,i)=>part.startsWith('`')?<code key={i}>{part.slice(1,-1)}</code>:part.startsWith('**')?<strong key={i}>{part.slice(2,-2)}</strong>:part)}
function CodeBlock({text,language}:{text:string;language:string}){
 const [copied,setCopied]=useState(false);
 return <div className="chat-code"><div className="chat-code-heading"><span>{language||'Código'}</span><button type="button" aria-label="Copiar código" onClick={()=>void navigator.clipboard.writeText(text).then(()=>setCopied(true)).catch(()=>setCopied(false))}>{copied?<Check size={12}/>:<Copy size={12}/>} {copied?'Copiado':'Copiar'}</button></div><pre><code><SyntaxCode code={text} language={language}/></code></pre></div>;
}
export default function ChatMessage({content}:{content:string}){return <div className="chat-message">{chatBlocks(content).map((block,i)=>block.kind==='code'?<CodeBlock key={i} text={block.text} language={block.language}/>:block.kind==='list'?<div key={i}>{block.text.split('\n').map((line,n)=>/^(?:[-*+] |\d+\. )/.test(line)?<div className="chat-list-item" key={n}><span aria-hidden="true">•</span><span>{inline(line.replace(/^(?:[-*+] |\d+\. )/,''))}</span></div>:<p key={n}>{inline(line)}</p>)}</div>:<p key={i}>{inline(block.text)}</p>)}</div>}
