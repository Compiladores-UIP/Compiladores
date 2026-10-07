// Conserva los fragmentos descargados para retomar una carga interrumpida.
export function resumableModelFetch(fetcher:typeof fetch,cache:Cache|null){
 return async (input:RequestInfo|URL,init?:RequestInit):Promise<Response>=>{
  const url=typeof input==='string'?input:input instanceof URL?input.href:input.url;
  if(new Headers(init?.headers).has('Range')||!/^https:\/\/huggingface\.co\/onnx-community\/Qwen3-1\.7B-ONNX\/resolve\/[a-f0-9]{40}\/onnx\/model_q4(?:f16)?\.onnx$/.test(url))return fetcher(input,init);
  const total=url.includes('q4f16')?1426069098:2147212861,partSize=16*1024*1024;
  let offset=0;const cancellation=new AbortController();
  const pending=new Map<number,Promise<{bytes?:Uint8Array;error?:unknown}>>();
  const loadPart=async(start:number):Promise<Uint8Array>=>{
   const end=Math.min(total-1,start+partSize-1),key=`${url}?nexo-part=${start}`;
   try{const saved=await cache?.match(key);if(saved){const data=new Uint8Array(await saved.arrayBuffer());if(data.length===end-start+1)return data}}catch{}
   for(let attempt=0;attempt<3;attempt++){
    try{
     const headers=new Headers(init?.headers);headers.set('Range',`bytes=${start}-${end}`);
     const response=await fetcher(`${url}?nexo-range=${start}-${end}`,{...init,headers,signal:AbortSignal.any([cancellation.signal,...(init?.signal?[init.signal]:[]),AbortSignal.timeout(60000)])});
     if(response.status!==206||response.headers.get('content-range')!==`bytes ${start}-${end}/${total}`){await response.body?.cancel();throw new Error('El servidor no entregó el fragmento solicitado.')}
     const bytes=new Uint8Array(await response.arrayBuffer());
     if(bytes.length!==end-start+1)throw new Error('Fragmento de modelo incompleto.');
     try{await cache?.put(key,new Response(bytes.slice().buffer))}catch{}
     return bytes;
    }catch(error){if(cancellation.signal.aborted||init?.signal?.aborted||attempt===2)throw error}
   }
   throw new Error('No se pudo recuperar el fragmento.');
  };
  const stream=new ReadableStream<Uint8Array>({
   async pull(controller){
    if(offset>=total){controller.close();return}
    for(let n=0;n<3;n++){
     const start=offset+n*partSize;
     if(start<total&&!pending.has(start))pending.set(start,loadPart(start).then(bytes=>({bytes}),error=>({error})));
    }
    const result=await pending.get(offset)!;pending.delete(offset);
    if(cancellation.signal.aborted)return;
    if(!result.bytes){cancellation.abort();controller.error(result.error);return}
    offset+=result.bytes.length;controller.enqueue(result.bytes);
   },
   cancel(){cancellation.abort();pending.clear()},
  });
  return new Response(stream,{status:200,headers:{'Content-Length':String(total),'Content-Type':'application/octet-stream'}});
 };
}
