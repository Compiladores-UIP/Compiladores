/// <reference lib="webworker" />
import {env,pipeline,TextStreamer,InterruptableStoppingCriteria,type ProgressInfo} from '@huggingface/transformers';
import {navigationCatalog} from './navigation-catalog.ts';
import {resumableModelFetch} from './model-download.ts';
import {keywordSearch} from './learning-search.ts';
import {conversationModel,conversationContext,conversationInstructions,cleanModelText,groundedModelAnswer} from './conversation-policy.ts';
env.allowLocalModels=false;
env.useBrowserCache=true;
env.backends.onnx.wasm!.numThreads=self.crossOriginIsolated?Math.max(1,Math.min(4,(navigator.hardwareConcurrency||2)-1)):1;
const scope=self as unknown as DedicatedWorkerGlobalScope;
let generator:Awaited<ReturnType<typeof pipeline<'text-generation'>>>;
let ready:Promise<void>|null=null;
let lastProgress=0;
const stopping=new InterruptableStoppingCriteria();
async function initialize(){
 if(!ready)ready=(async()=>{
  let cache:Cache|null=null;try{cache=await caches.open('nexo-model-parts-v1')}catch{}
  env.fetch=resumableModelFetch(fetch.bind(self),cache);
  const progress_callback=(event:ProgressInfo)=>{
   if(event.status==='progress'){const now=Date.now();if(event.progress<100&&now-lastProgress<200)return;lastProgress=now}
   if(event.status==='initiate'||event.status==='download'||event.status==='progress'||event.status==='done')scope.postMessage({type:'progress',...event});
  };
  let device:'webgpu'|'wasm'='wasm';
  try{
   const gpu=(navigator as unknown as {gpu?:{requestAdapter():Promise<{info?:{vendor?:string;description?:string};isFallbackAdapter?:boolean;features?:{has(value:string):boolean}}|null>}}).gpu;
   const adapter=await gpu?.requestAdapter();
   if(adapter&&!adapter.isFallbackAdapter&&!/swiftshader|software|llvmpipe/i.test(`${adapter.info?.vendor} ${adapter.info?.description}`)){
    scope.postMessage({type:'backend',device:'webgpu'});
    const dtype=adapter.features?.has('shader-f16')?'q4f16':'q4';
    generator=await pipeline('text-generation',conversationModel.id,{revision:conversationModel.revision,device:'webgpu',dtype,progress_callback});
    const probe=await generator([{role:'user',content:'2 + 2 = ? Responde solo el número.'}],{max_new_tokens:8,do_sample:false,tokenizer_encode_kwargs:{enable_thinking:false}});
    const value=probe[0].generated_text;
    const answer=typeof value==='string'?value:value.at(-1)?.content;
    if(typeof answer!=='string'||!/^4[.!]?$/.test(answer.trim()))throw new Error('La GPU no pasó la comprobación.');
    device='webgpu';
   }
  }catch(error){console.warn('WebGPU validation failed:',error);await generator?.dispose()}
  if(device==='wasm')generator=await pipeline('text-generation','onnx-community/Qwen2.5-0.5B-Instruct',{device:'wasm',dtype:'q4',progress_callback});
  scope.postMessage({type:'backend',device,threads:device==='wasm'?env.backends.onnx.wasm!.numThreads:0});
 })().catch(error=>{ready=null;throw error});return ready;
}
let queue=Promise.resolve();
let cancellationVersion=0;
scope.onmessage=event=>{
 const {type,id,message,history,location,kind}=event.data;
 if(type==='cancel'){cancellationVersion++;stopping.interrupt();return}
 const version=cancellationVersion;
 queue=queue.then(async()=>{
  try{
   await initialize();if(type==='load'){scope.postMessage({type:'ready',id});return}
   if(version!==cancellationVersion)return;stopping.reset();
   const navigation=kind==='navigation';
   const candidates=Array.isArray(event.data.candidates)?event.data.candidates.filter((id:unknown)=>typeof id==='string'&&navigationCatalog.some(route=>route.id===id)):keywordSearch(String(message)).map(hit=>`mod-${hit.index+1}`);
   const started=performance.now();let streamed='',firstToken:number|null=null;
   const streamer=new TextStreamer(generator.tokenizer,{skip_prompt:true,skip_special_tokens:true,callback_function:chunk=>{
    if(firstToken===null)firstToken=performance.now()-started;
    streamed+=chunk;scope.postMessage({type:'partial',id,text:cleanModelText(streamed)});
   }});
   const messages=[{role:'system',content:conversationInstructions(String(location),navigation,candidates)},...conversationContext(history),{role:'user',content:String(message).slice(0,500)}];
   const output=await generator(messages,{max_new_tokens:160,do_sample:true,temperature:0.7,top_p:0.8,top_k:20,repetition_penalty:1.1,tokenizer_encode_kwargs:{enable_thinking:false},return_full_text:false,streamer,stopping_criteria:stopping});
   if(version!==cancellationVersion)return;
   const result=Array.isArray(output[0])?output[0][0]:output[0];
   const generated=result.generated_text,raw=typeof generated==='string'?generated:generated.at(-1)?.content;
   if(typeof raw!=='string')throw new Error('Respuesta vacía');
   scope.postMessage({type:'answer',id,metrics:{firstTokenMs:firstToken,totalMs:performance.now()-started},plan:groundedModelAnswer(raw,candidates,navigation)});
  }catch(error){scope.postMessage({type:'error',id,message:error instanceof Error?error.message:'El modelo no pudo responder.'})}
 });
};
