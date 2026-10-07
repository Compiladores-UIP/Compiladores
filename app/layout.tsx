import type { Metadata } from 'next';
import './globals.css';
import './redesign.css';
import './tutorial.css';
import './real-editor.css';
import './polish.css';
import './learning-guide.css';
import './course-search.css';
import LearningGuide from './learning-guide';
import ModelStartup from './model-startup';
export const metadata: Metadata = { title: 'Nexo | Aprende a programar', description: 'Entiende cómo piensa un lenguaje. Aprende compiladores con lecciones, ejercicios y un laboratorio interactivo.' };
export default function RootLayout({children}:{children:React.ReactNode}) {return <html lang="es"><body>{children}<ModelStartup/><LearningGuide/></body></html>}
