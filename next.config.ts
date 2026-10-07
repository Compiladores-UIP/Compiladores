import type { NextConfig } from 'next';
const config: NextConfig = { distDir: process.env.NEXO_BUILD_DIR || (process.env.NODE_ENV==='development'?'.next-dev':'.next'), devIndicators: false,
 async headers(){return [{source:'/:path*',headers:[{key:'Cross-Origin-Opener-Policy',value:'same-origin'},{key:'Cross-Origin-Embedder-Policy',value:'credentialless'}]}]}
};
export default config;
