export type ChatBlock={kind:'code';text:string;language:string}|{kind:'paragraph'|'list';text:string};
// Los bloques sin cerrar se muestran como código mientras llega el resto de la respuesta.
export function chatBlocks(content:string):ChatBlock[]{
 const blocks:ChatBlock[]=[];let text:string[]=[],code:string[]|null=null,language='';
 const flush=()=>{if(text.length){const value=text.join('\n').trim();if(value)blocks.push({kind:/^(?:[-*+] |\d+\. )/m.test(value)?'list':'paragraph',text:value});text=[]}};
 for(const line of content.replace(/\r\n/g,'\n').split('\n')){
  const fence=line.match(/^\s*```\s*([\w#+.-]*)\s*$/);
  if(fence){if(code){blocks.push({kind:'code',text:code.join('\n'),language});code=null}else{flush();language=fence[1];code=[]}continue}
  if(code)code.push(line);else if(!line.trim())flush();else text.push(line);
 }
 if(code)blocks.push({kind:'code',text:code.join('\n'),language});flush();return blocks;
}
