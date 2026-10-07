import {notFound,redirect} from 'next/navigation';
import type {Metadata} from 'next';
import {modules,slugs} from '../../modules';
import Tutorial from '../../tutorial';
export function generateStaticParams(){return slugs.map(slug=>({slug}))}
export async function generateMetadata({params}:{params:Promise<{slug:string}>}):Promise<Metadata>{const {slug}=await params;const index=slugs.indexOf(slug);return {title:index<0?'Módulo no encontrado | Nexo':`${modules[index].name} | Aprende con Nexo`}}
export default async function CoursePage({params}:{params:Promise<{slug:string}>}){const {slug}=await params;if(slug==='generacion-csharp')redirect('/aprender/pseudocodigo-csharp');const index=slugs.indexOf(slug);if(index<0)notFound();return <Tutorial key={slug} index={index}/>}
