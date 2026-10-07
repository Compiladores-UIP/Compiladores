'use client';
import {useEffect,useRef} from 'react';
import {Flower} from './artwork';
export function Reveal(){
 useEffect(()=>{
  const media=matchMedia('(prefers-reduced-motion: reduce)');
  let cleanup=()=>{};
  const setup=()=>{
   cleanup();
   if(media.matches)return;
   const sections=Array.from(document.querySelectorAll('main > section, .lesson-section'));
   const nodes:HTMLElement[]=[];
   const cards:{node:HTMLElement;anchor:HTMLElement;index:number;count:number;baseRotation:number;kind:string}[]=[];
   sections.forEach((section,index)=>{
    if(!section.classList.contains('hero')){
     section.classList.add(`reveal-style-${index%3}`);
     const children=Array.from(section.querySelectorAll<HTMLElement>('.section-heading,.learning-strip > div,.learning-strip > a,.module-card,.practice-notes > div,.lab,.method-title,.method-step,.goal-chip,.goals-copy,.project-heading,.project-card,.closing > h2,.closing > .button'));
     (children.length?children:[section as HTMLElement]).forEach((node,n)=>{node.classList.add('scroll-reveal');node.style.setProperty('--reveal-delay',`${Math.min(n,4)*65}ms`);nodes.push(node)});
    }
    const elements=Array.from(section.querySelectorAll<HTMLElement>('.hero-course,.learning-strip > a,.module-card,.practice-notes > div,.lab,.method-step,.goal-chip,.project-card'));
    elements.forEach((node,n)=>{
     cards.push({node,anchor:node.classList.contains('goal-chip')||node.classList.contains('hero-course')?section as HTMLElement:node.parentElement!,index:n,count:elements.length,baseRotation:parseFloat(getComputedStyle(node).rotate)||0,kind:node.classList.contains('hero-course')?'hero':node.classList.contains('goal-chip')?'bubble':node.classList.contains('project-card')?'fan':'rise'});
     node.classList.add('scroll-card');
    });
   });
   // Deja de observar el elemento al aparecer para evitar reinicios en el borde de la pantalla.
   const observer=new IntersectionObserver(entries=>entries.forEach(entry=>{if(entry.isIntersecting){entry.target.classList.add('is-revealed');observer.unobserve(entry.target)}}),{threshold:.05,rootMargin:'0px 0px -20px 0px'});
   nodes.forEach(node=>observer.observe(node));
   let frame=0;
   const update=()=>{
    frame=0;
    const height=innerHeight,mobile=innerWidth<701,factor=mobile?.55:1;
    // Agrupa las lecturas antes de cambiar los estilos.
    const bounds=new Map<HTMLElement,DOMRect>();
    cards.forEach(({anchor})=>{if(!bounds.has(anchor))bounds.set(anchor,anchor.getBoundingClientRect())});
    cards.forEach(({node,anchor,index,count,baseRotation,kind})=>{
     const rect=bounds.get(anchor)!;
     if(rect.top>height+250||rect.bottom<-250)return;
     const progress=Math.max(0,Math.min(1,(height-rect.top)/(height+Math.min(rect.height,650))));
     const entry=Math.max(0,1-progress/.58),exit=Math.max(0,(progress-.66)/.34),side=index-(count-1)/2;
     let x=0,y=entry*(85+index%3*27)-exit*(20+index%3*9),angle=0;
     if(kind==='fan'){x=side*entry*55;angle=side*entry*11;y=entry*(110+Math.abs(side)*35)-exit*35}
     if(kind==='bubble'){x=side*entry*19;y=(.5-progress)*(55+index%3*28);angle=(.5-progress)*(index%2?-8:8)}
     if(kind==='hero'){y=-progress*(80+index*15);angle=progress*(index%2?6:-6)}
     node.style.setProperty('--scroll-x',`${(x*factor).toFixed(2)}px`);
     node.style.setProperty('--scroll-y',`${(y*factor).toFixed(2)}px`);
     node.style.setProperty('--scroll-rotation',`${(baseRotation+angle*factor).toFixed(2)}deg`);
     node.style.setProperty('--scroll-scale',String(1-entry*.055*factor));
    });
   };
   const schedule=()=>{if(!frame)frame=requestAnimationFrame(update)};
   window.addEventListener('scroll',schedule,{passive:true});window.addEventListener('resize',schedule);update();
   cleanup=()=>{
    cancelAnimationFrame(frame);observer.disconnect();window.removeEventListener('scroll',schedule);window.removeEventListener('resize',schedule);
    nodes.forEach(node=>{node.classList.remove('scroll-reveal','is-revealed');node.style.removeProperty('--reveal-delay')});
    cards.forEach(({node})=>{node.classList.remove('scroll-card');['--scroll-x','--scroll-y','--scroll-rotation','--scroll-scale'].forEach(key=>node.style.removeProperty(key))});
    sections.forEach((section,index)=>section.classList.remove(`reveal-style-${index%3}`));
   };
  };
  setup();media.addEventListener('change',setup);
  return()=>{cleanup();media.removeEventListener('change',setup)};
 },[]);
 return null;
}
export function CursorTrail(){const scene=useRef<HTMLDivElement>(null);
 useEffect(()=>{const el=scene.current,section=el?.closest('section');if(!el||!section)return;const media=matchMedia('(prefers-reduced-motion: reduce)'),fine=matchMedia('(pointer: fine)');if(media.matches||!fine.matches)return;
 const shapes=Array.from(el.children) as HTMLElement[];const points=shapes.map(()=>({x:0,y:0,time:-2000,angle:0}));let next=0,frame=0,previous:{x:number;y:number}|null=null,visible=true;
 const emit=(x:number,y:number)=>{points[next%points.length]={x,y,time:performance.now(),angle:next*137.5};next++};
 const move=(event:PointerEvent)=>{const rect=el.getBoundingClientRect(),point={x:event.clientX-rect.left,y:event.clientY-rect.top};if(!previous){emit(point.x,point.y);previous=point;return}const distance=Math.hypot(point.x-previous.x,point.y-previous.y);if(distance<34)return;const steps=Math.min(12,Math.floor(distance/34));for(let i=1;i<=steps;i++)emit(previous.x+(point.x-previous.x)*i/steps,previous.y+(point.y-previous.y)*i/steps);previous=point;};
 const leave=()=>{previous=null};const loop=(now:number)=>{if(!visible){frame=0;return}shapes.forEach((shape,i)=>{const p=points[i],age=(now-p.time)/1900;if(age>=1){shape.style.opacity='0';return}shape.style.opacity=String(Math.pow(1-Math.max(0,age),1.3)*.9);shape.style.transform=`translate3d(${p.x-35}px,${p.y-35}px,0) rotate(${p.angle+age*65}deg) scale(${1-age*.6})`;});frame=requestAnimationFrame(loop)};
 const observer=new IntersectionObserver(entries=>{visible=entries[0].isIntersecting;if(visible&&!frame)frame=requestAnimationFrame(loop);if(!visible&&frame){cancelAnimationFrame(frame);frame=0}},{rootMargin:'100px'});observer.observe(section);section.addEventListener('pointermove',move);section.addEventListener('pointerleave',leave);frame=requestAnimationFrame(loop);return()=>{cancelAnimationFrame(frame);observer.disconnect();section.removeEventListener('pointermove',move);section.removeEventListener('pointerleave',leave)};
 },[]);
 return <div ref={scene} className="cursor-trail" aria-hidden="true">{Array.from({length:28},(_,n)=><div className={`cursor-trail-shape trail-color-${n%6}`} key={n}>{n%4===0?<span className="shape-orb"/>:n%4===1?<Flower/>:n%4===2?<svg viewBox="0 0 100 100"><path d="M50 50C-20-20 120-20 50 50C120 120-20 120 50 50C-20 120-20-20 50 50C120-20 120 120 50 50" fill="currentColor"/></svg>:<svg viewBox="0 0 100 100"><path d="M50 0Q50 50 100 50Q50 50 50 100Q50 50 0 50Q50 50 50 0Z" fill="currentColor"/></svg>}</div>)}</div>
}


