export type CSharpResult={stdout:string;stderr:string;diagnostics:string;exitCode:number|null;signal:string;compiler:string;success:boolean};
export function wrapSnippet(source:string){return `using System;\n\nclass Program\n{\n    static void Main()\n    {\n        Console.InputEncoding = new System.Text.UTF8Encoding(false);\n        Console.OutputEncoding = new System.Text.UTF8Encoding(false);\n\n${source.split('\n').map(line=>'        '+line).join('\n')}\n    }\n}\n`}
export function validateCSharpRequest(value:unknown):{code:string;stdin:string}{if(!value||typeof value!=='object')throw new Error('Se requiere un programa C#.');const {code,stdin=''}=value as Record<string,unknown>;if(typeof code!=='string'||!code.trim())throw new Error('Escribe un programa antes de compilar.');if(code.length>30000)throw new Error('El programa supera el límite de 30 000 caracteres.');if(typeof stdin!=='string'||stdin.length>10000)throw new Error('La entrada supera el límite de 10 000 caracteres.');return {code,stdin}}
export function normalizeWandbox(value:unknown):CSharpResult{if(!value||typeof value!=='object')throw new Error('El compilador devolvió una respuesta inválida.');const r=value as Record<string,unknown>;if(typeof r.status!=='string'&&typeof r.status!=='number')throw new Error('El compilador no pudo procesar la petición.');const text=(key:string)=>typeof r[key]==='string'?(r[key] as string).slice(0,100000):'';const exitCode=String(r.status).trim()!==''&&Number.isFinite(Number(r.status))?Number(r.status):null;return {stdout:text('program_output'),stderr:text('program_error'),diagnostics:text('compiler_message')||text('compiler_error')||text('compiler_output'),exitCode,signal:text('signal'),compiler:'Mono 6.12 · C#',success:exitCode===0&&!text('signal')}}
export async function executeCSharp(code:string,stdin:string,signal?:AbortSignal):Promise<CSharpResult>{
 const payload=validateCSharpRequest({code,stdin});
 let response:Response;
 try{response=await fetch('/api/csharp',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload),signal})}
 catch(error){if(signal?.aborted||error instanceof Error&&error.name==='AbortError')throw error;throw new Error('No se pudo conectar con el compilador. Comprueba tu conexión y vuelve a intentar.')}
 let value:unknown;
 try{value=await response.json()}catch{throw new Error(response.ok?'El compilador devolvió una respuesta inválida. Vuelve a intentar.':`El servicio no pudo completar la petición (${response.status}). Vuelve a intentar más tarde.`)}
 if(!response.ok){const message=value&&typeof value==='object'&&(value as Record<string,unknown>).error;throw new Error(typeof message==='string'?message:'No se pudo contactar con el compilador.')}
 if(!value||typeof value!=='object')throw new Error('El compilador devolvió una respuesta inválida.');
 const result=value as Record<string,unknown>;
 if(!['stdout','stderr','diagnostics','signal','compiler'].every(key=>typeof result[key]==='string')||typeof result.success!=='boolean'||!(result.exitCode===null||typeof result.exitCode==='number'&&Number.isFinite(result.exitCode)))throw new Error('El compilador devolvió una respuesta incompleta. Vuelve a intentar.');
 return value as CSharpResult;
}
