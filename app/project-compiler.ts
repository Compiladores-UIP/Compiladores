import {compile,tokenize} from './compiler.ts';
import {wrapSnippet} from './csharp-service.ts';

type Tree={label:string;detail?:string;children?:Tree[]};
type Control={type:'label'|'button';text:string;message?:string};
export type ProjectResult=ReturnType<typeof compile>&{
 target:string;filename:string;file:string;project?:string;tree?:Tree;
 preview?:{title:string;controls:Control[]};
};
const projectFile=(forms:boolean)=>`<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>${forms?'WinExe':'Exe'}</OutputType>
    <TargetFramework>${forms?'net8.0-windows':'net8.0'}</TargetFramework>
    ${forms?'<UseWindowsForms>true</UseWindowsForms>':''}
  </PropertyGroup>
</Project>`;

// Traduce las instrucciones por línea sin modificar el contenido de las cadenas de texto.
export function academicSource(source:string):string{
 const stack:string[]=[];
 const expression=(text:string)=>tokenize(text).map(t=>({verdadero:'true',falso:'false',Y:'&&',O:'||',NO:'!'}[t]||t)).join(' ');
 const close=(kind:string)=>{if(stack.pop()!==kind)throw new Error(`El cierre ${kind} no coincide con el bloque abierto.`);return '}'};
 const translated=source.split(/\r?\n/).map((raw,n)=>{
  const line=raw.trim();if(!line||line.startsWith('//'))return '';
  let m:RegExpMatchArray|null;
  if(m=line.match(/^(IMPRIMIR|MOSTRAR)\s+(.+?)\s*;?$/))return `Console.WriteLine(${expression(m[2].replace(/;$/,''))});`;
  if(m=line.match(/^(ENTERO|DECIMAL|TEXTO|BOOLEANO)\s+([A-Za-z_]\w*)\s*=\s*(.+?)\s*;?$/))return `${{ENTERO:'int',DECIMAL:'double',TEXTO:'string',BOOLEANO:'bool'}[m[1]]} ${m[2]} = ${expression(m[3].replace(/;$/,''))};`;
  if(m=line.match(/^SI\s+(.+)\s+ENTONCES$/)){stack.push('SI');return `if (${expression(m[1])}) {`}
  if(line==='SINO'){if(stack.at(-1)!=='SI')throw new Error('SINO requiere un bloque SI.');stack[stack.length-1]='SINO';return '} else {'}
  if(line==='FINSI'){const kind=stack.pop();if(kind!=='SI'&&kind!=='SINO')throw new Error('FINSI requiere un bloque SI.');return '}'}
  if(m=line.match(/^MIENTRAS\s+(.+)\s+HACER$/)){stack.push('MIENTRAS');return `while (${expression(m[1])}) {`}
  if(line==='FINMIENTRAS')return close('MIENTRAS');
  if(m=line.match(/^PARA\s+([A-Za-z_]\w*)\s*=\s*(.+)\s+HASTA\s+(.+)\s+HACER$/)){stack.push('PARA');return `for (int ${m[1]} = ${expression(m[2])}; ${m[1]} <= ${expression(m[3])}; ${m[1]}++) {`}
  if(line==='FINPARA')return close('PARA');
  if(line==='REPETIR'){stack.push('REPETIR');return 'do {'}
  if(m=line.match(/^HASTA\s+(.+)$/)){close('REPETIR');return `} while (!(${expression(m[1])}));`}
  if(/^[A-Za-z_]\w*\s*=/.test(line))return expression(line.replace(/;$/,''))+';';
  throw new Error(`Instrucción no reconocida en la línea ${n+1}: ${line}`);
 }).join('\n');
 if(stack.length)throw new Error(`Falta cerrar el bloque ${stack.at(-1)}.`);
 return translated;
}
function special(tokens:string[],generated:string,output:string,tree:Tree,target:string,filename:string):ProjectResult{
 return {tokens,generated,output,steps:tree.children?.length||1,ast:{type:'Program',body:[]},tree,target,filename,file:generated};
}
const quoted='"(?:\\\\.|[^"\\\\])*"';
function forms(source:string):ProjectResult{
 let title='';const controls:Control[]=[];const nodes:Tree[]=[];
 for(const [n,raw] of source.split(/\r?\n/).entries()){
  const line=raw.trim();if(!line||line.startsWith('//'))continue;
  const m=line.match(new RegExp(`^(VENTANA|ETIQUETA|BOTON)\\s+(${quoted})(?:\\s+MENSAJE\\s+(${quoted}))?$`));
  if(!m)throw new Error(`Control no válido en la línea ${n+1}.`);
  const text=JSON.parse(m[2]) as string;
  if(m[1]==='VENTANA'){if(title)throw new Error('Usa una sola VENTANA.');if(m[3])throw new Error('VENTANA no admite MENSAJE.');title=text}
  else {if(m[1]==='ETIQUETA'&&m[3])throw new Error('ETIQUETA no admite MENSAJE.');controls.push({type:m[1]==='BOTON'?'button':'label',text,message:m[3]?JSON.parse(m[3]):undefined})}
  nodes.push({label:m[1],detail:text,children:m[3]?[{label:'Evento clic',detail:JSON.parse(m[3])}]:undefined});
 }
 if(!title)throw new Error('Declara VENTANA "Título" antes de generar el formulario.');
 const definitions=controls.map((c,n)=>`    var control${n} = new ${c.type==='button'?'Button':'Label'} { Text = ${JSON.stringify(c.text)}, AutoSize = true };\n    panel.Controls.Add(control${n});${c.message!==undefined?`\n    control${n}.Click += (sender, e) => MessageBox.Show(${JSON.stringify(c.message)});`:''}`).join('\n');
 const generated=`using System;\nusing System.Windows.Forms;\n\nclass Program\n{\n  [STAThread]\n  static void Main()\n  {\n    Application.EnableVisualStyles();\n    var form = new Form { Text = ${JSON.stringify(title)}, Width = 440, Height = 320 };\n    var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(24) };\n    form.Controls.Add(panel);\n${definitions}\n    Application.Run(form);\n  }\n}`;
 return {...special(tokenize(source),generated,'Vista previa del formulario. Compila el proyecto en Windows para ejecutar WinForms.',{label:'Formulario',detail:title,children:nodes},'WinForms','Program.cs'),project:projectFile(true),preview:{title,controls}};
}
const students=[{nombre:'Ana',edad:20},{nombre:'Luis',edad:17},{nombre:'Marta',edad:18},{nombre:'Pedro',edad:22}];
function query(source:string):ProjectResult{
 const m=source.trim().match(/^BUSCAR\s+estudiante\s+DONDE\s+(edad|nombre)\s*(>=|<=|!=|=|>|<)\s*("(?:\\.|[^"\\])*"|\d+)\s*;?$/);
 if(!m)throw new Error('Usa BUSCAR estudiante DONDE edad > 18 o nombre = "Ana".');
 const value=JSON.parse(m[3]) as string|number,field=m[1] as 'edad'|'nombre',operator=m[2];
 if(typeof value!==(field==='edad'?'number':'string'))throw new Error('edad requiere un número y nombre requiere texto.');
 if(field==='nombre'&&!['=','!='].includes(operator))throw new Error('Para nombre usa = o !=.');
 const rows=students.filter(row=>{const a=row[field];switch(operator){case '=':return a===value;case '!=':return a!==value;case '>':return Number(a)>Number(value);case '<':return Number(a)<Number(value);case '>=':return Number(a)>=Number(value);default:return Number(a)<=Number(value)}});
 const literal=typeof value==='string'?`'${value.replace(/'/g,"''")}'`:String(value);
 const generated=`SELECT *\nFROM estudiante\nWHERE ${field} ${operator==='!='?'<>':operator} ${literal};`;
 return special(source.match(/"(?:\\.|[^"\\])*"|>=|<=|!=|\w+|\S/g)||[],generated,rows.length?rows.map(row=>`${row.nombre} · ${row.edad} años`).join('\n'):'(Sin coincidencias)',{label:'Consulta',children:[{label:'Tabla',detail:'estudiante'},{label:'Filtro',detail:operator,children:[{label:'Campo',detail:field},{label:'Valor',detail:String(value)}]}]},'SQL','consulta.sql');
}
function configuration(source:string):ProjectResult{
 const lines=source.split(/\r?\n/).map(s=>s.trim()).filter(s=>s&&!s.startsWith('//'));
 const format=lines.shift()?.match(/^FORMATO (JSON|XML)$/)?.[1];if(!format)throw new Error('Comienza con FORMATO JSON o FORMATO XML.');
 const values:Record<string,string|number|boolean>=Object.create(null);
 for(const line of lines){const m=line.match(/^CONFIG ([A-Za-z_]\w*)\s*=\s*(.+)$/);if(!m)throw new Error(`Configuración inválida: ${line}`);if(m[1] in values)throw new Error(`La clave ${m[1]} está duplicada.`);const raw=m[2];let value:unknown;try{value=JSON.parse(raw==='verdadero'?'true':raw==='falso'?'false':raw)}catch{throw new Error(`Valor inválido para ${m[1]}.`)}if(!['string','number','boolean'].includes(typeof value)||typeof value==='number'&&!Number.isFinite(value))throw new Error('Usa texto, número o booleano.');values[m[1]]=value as string|number|boolean}
 const escape=(s:string)=>s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/'/g,'&apos;');
 const generated=format==='JSON'?JSON.stringify(values,null,2):`<?xml version="1.0" encoding="UTF-8"?>\n<configuracion>\n${Object.entries(values).map(([k,v])=>`  <${k} tipo="${typeof v}">${escape(String(v))}</${k}>`).join('\n')}\n</configuracion>`;
 return special(source.match(/"(?:\\.|[^"\\])*"|\w+|\S/g)||[],generated,generated,{label:'Configuración',detail:format,children:Object.entries(values).map(([k,v])=>({label:k,detail:`${typeof v}: ${v}`}))},format,`configuracion.${format.toLowerCase()}`);
}
export function compileProject(source:string,index:number):ProjectResult{
 if(!source.trim())throw new Error('Escribe al menos una instrucción.');if(source.length>30000)throw new Error('El programa supera el límite de 30 000 caracteres.');
 if(index===8)return forms(source);if(index===9)return query(source);if(index===10)return configuration(source);
 const academic=/^\s*(IMPRIMIR|MOSTRAR|ENTERO|DECIMAL|TEXTO|BOOLEANO|SI|MIENTRAS|PARA|REPETIR)\b/m.test(source);
 const normalized=academic?academicSource(source):index===1&&!source.includes(';')?`Console.WriteLine(${source});`:source;
 const result=compile(normalized);
 return {...result,tokens:tokenize(source),target:'C#',filename:'Program.cs',file:wrapSnippet(result.generated),project:projectFile(false)};
}
