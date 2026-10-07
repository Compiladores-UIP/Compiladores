'use client';

export type GuideAction='run-example'|'stage-1'|'stage-2'|'stage-3'|'stage-4'|'pause-example';
export async function performGuideAction(action:GuideAction){
 const target=document.querySelector<HTMLElement>(`[data-nexo-action="${action}"]`);
 if(!target)throw new Error('Abre una lección para usar este recorrido.');
 target.scrollIntoView({behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth',block:'center'});
 const {PageController}=await import('@page-agent/page-controller');
 const controller=new PageController({enableMask:false,viewportExpansion:-1,highlightOpacity:0,highlightLabelOpacity:0,includeAttributes:['data-nexo-action'],interactiveWhitelist:[target],interactiveBlacklist:Array.from(document.querySelectorAll('a,button,input,textarea,select')).filter(el=>el!==target)});
 try{
  const state=await controller.getBrowserState();
  const escaped=action.replace(/[.*+?^${}()|[\]\\]/g,'\\$&');
  const match=state.content.match(new RegExp(`\\[(\\d+)\\]<[^>]*data-nexo-action=["']?${escaped}\\b`));
  if(!match)throw new Error('El control no está disponible. Desplázate hasta el ejemplo e inténtalo otra vez.');
  const result=await controller.clickElement(Number(match[1]));
  if(!result.success)throw new Error('No se pudo activar el control del ejemplo.');
 }finally{controller.dispose()}
}
