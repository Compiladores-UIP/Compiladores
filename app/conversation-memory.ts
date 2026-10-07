import {validateNavigationPlan,type NavigationPlan} from './navigation-catalog.ts';

export const conversationMemoryKey='nexo.conversation.v1';
export type ConversationTurn={role:'user'|'assistant';content:string;plan?:NavigationPlan};

export function decodeConversationMemory(raw:string|null):ConversationTurn[]{
 if(!raw||raw.length>64000)return [];
 try{
  const value=JSON.parse(raw);
  if(value.version!==1||!Array.isArray(value.turns))return [];
  return value.turns.slice(-16).flatMap((turn:ConversationTurn)=>{
   if(!turn||!['user','assistant'].includes(turn.role)||typeof turn.content!=='string')return [];
   const result:ConversationTurn={role:turn.role,content:turn.content.slice(0,1500)};
   if(turn.role==='assistant'&&turn.plan)result.plan=validateNavigationPlan(JSON.stringify(turn.plan));
   return [result];
  });
 }catch{return []}
}
export function encodeConversationMemory(turns:ConversationTurn[]):string{
 return JSON.stringify({version:1,turns:turns.slice(-16).map(turn=>({...turn,content:turn.content.slice(0,1500)}))});
}
