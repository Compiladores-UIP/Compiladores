import {NextRequest,NextResponse} from 'next/server';
import {normalizeWandbox,validateCSharpRequest} from '../../csharp-service';
export const runtime='nodejs';
export const maxDuration=60;
export async function POST(request:NextRequest){
 const headers={'Cache-Control':'no-store'};
 const origin=request.headers.get('origin');
 if(origin){try{if(new URL(origin).host!==(request.headers.get('host')||request.nextUrl.host))return NextResponse.json({error:'Origen no permitido.'},{status:403,headers})}catch{return NextResponse.json({error:'Origen no permitido.'},{status:403,headers})}}
 if(Number(request.headers.get('content-length')||0)>170000)return NextResponse.json({error:'La petición es demasiado grande.'},{status:413,headers});
 let body;try{const raw=await request.text();if(raw.length>170000)throw new Error('La petición es demasiado grande.');body=validateCSharpRequest(JSON.parse(raw))}catch(e){return NextResponse.json({error:(e as Error).message},{status:400,headers})}
 try{const response=await fetch('https://wandbox.org/api/compile.json',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({compiler:'mono-6.12.0.199',code:body.code,stdin:body.stdin,options:'','compiler-option-raw':'-langversion:7.2',save:false}),signal:AbortSignal.any([AbortSignal.timeout(45000),request.signal]),cache:'no-store'});
 if(!response.ok)return NextResponse.json({error:response.status===429?'El servicio alcanzó su límite de peticiones. Espera un momento y vuelve a intentar.':'El servicio de compilación no está disponible. Inténtalo de nuevo.'},{status:response.status===429?429:502,headers});
 const value=await response.json();return NextResponse.json(normalizeWandbox(value),{headers});
 }catch(e){const timeout=(e as Error).name==='TimeoutError';return NextResponse.json({error:timeout?'La ejecución tardó demasiado. Revisa los ciclos o vuelve a intentar.':'No se pudo conectar con Wandbox. Inténtalo de nuevo.'},{status:timeout?504:502,headers})}
}
