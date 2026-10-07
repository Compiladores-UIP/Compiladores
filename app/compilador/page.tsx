import type {Metadata} from 'next';
import Editor from '../real-editor';
import {modules,slugs} from '../modules';
import {courses} from '../courses';
import {compileProject} from '../project-compiler';
import {wrapSnippet} from '../csharp-service';
export const metadata:Metadata={title:'Compilador C# online | Nexo',description:'Escribe, compila y ejecuta C# en el navegador, sin clave API.'};
export default async function CompilerPage({searchParams}:{searchParams:Promise<{modulo?:string}>}){const {modulo}=await searchParams;const index=slugs.indexOf(modulo||'');const source=index>=0&&modules[index].target==='C#'?wrapSnippet(compileProject(courses[index].source,index).generated):undefined;return <Editor initialSource={source}/>}
