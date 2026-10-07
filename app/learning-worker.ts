/// <reference lib="webworker" />
import {env,pipeline} from '@huggingface/transformers';
import {searchDocuments,rankEmbeddings,relevantHits} from './learning-search.ts';

env.allowLocalModels=false;
env.useBrowserCache=true;
env.backends.onnx.wasm!.numThreads=1;
const scope=self as unknown as DedicatedWorkerGlobalScope;
let ready:Promise<void>|null=null;
let extractor:Awaited<ReturnType<typeof pipeline<'feature-extraction'>>>;
let vectors:number[][]=[];
let lastProgress=0;
async function initialize(){
 if(!ready)ready=(async()=>{
  extractor=await pipeline('feature-extraction','Xenova/paraphrase-multilingual-MiniLM-L12-v2',{
   dtype:'q8',device:'wasm',progress_callback:event=>{
    if(event.status==='progress'){const now=Date.now();if(event.progress<100&&now-lastProgress<200)return;lastProgress=now;scope.postMessage({type:'progress',file:event.file,progress:event.progress})}
   },
  });
  scope.postMessage({type:'indexing'});
  for(const doc of searchDocuments){for(const goal of doc.goals){const vector=await extractor(goal,{pooling:'mean',normalize:true});vectors.push(Array.from(vector.data) as number[])}}
 })().catch(error=>{ready=null;vectors=[];throw error});
 return ready;
}
// Procesa las peticiones en orden para que la indexación no coincida con las búsquedas.
let queue=Promise.resolve();
scope.onmessage=event=>{
 const {type,query,id}=event.data as {type:string;query?:string;id:number};
 queue=queue.then(async()=>{
  try{await initialize();if(type==='load'){scope.postMessage({type:'ready',id});return}
   if(!query?.trim())return;
   const vector=await extractor(query,{pooling:'mean',normalize:true});
   const ranked=rankEmbeddings(Array.from(vector.data) as number[],vectors,vectors.length);
   const scores=searchDocuments.map(doc=>({index:doc.index,score:Math.max(...ranked.filter(hit=>Math.floor(hit.index/2)===doc.index).map(hit=>hit.score))}));
   const hits=relevantHits(scores,query);
   scope.postMessage({type:'results',id,hits});
  }catch(error){scope.postMessage({type:'error',id,message:error instanceof Error?error.message:'No se pudo cargar la búsqueda.'})}
 });
};
