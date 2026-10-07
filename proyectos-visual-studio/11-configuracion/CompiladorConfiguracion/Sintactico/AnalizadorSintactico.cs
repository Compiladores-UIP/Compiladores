using CompiladorConfiguracion.Lexico;

namespace CompiladorConfiguracion.Sintactico
{
    /// <summary>
    /// FASE 2 · Análisis sintáctico (descendente recursivo).
    /// Cada línea es una instrucción; las secciones se anidan con una pila.
    ///
    ///   &lt;archivo&gt;  ::= &lt;formato&gt; { &lt;elemento&gt; }
    ///   &lt;formato&gt;  ::= FORMATO ( JSON | XML )
    ///   &lt;elemento&gt; ::= &lt;config&gt; | &lt;seccion&gt;
    ///   &lt;config&gt;   ::= CONFIG CLAVE [ ":" TIPO ] "=" &lt;valor&gt;
    ///   &lt;seccion&gt;  ::= SECCION CLAVE { &lt;elemento&gt; } FINSECCION
    ///   &lt;valor&gt;    ::= TEXTO | ENTERO | DECIMAL | BOOLEANO | &lt;lista&gt;
    ///   &lt;lista&gt;    ::= "[" [ &lt;simple&gt; { "," &lt;simple&gt; } ] "]"
    /// </summary>
    public class AnalizadorSintactico
    {
        public List<ErrorCompilacion> Errores { get; } = new();

        private List<Token> _tokens = new();
        private int _pos;

        // Pila de secciones abiertas. null = sección con errores (solo sirve para emparejar su FINSECCION).
        private readonly Stack<Seccion?> _abiertas = new();
        private readonly List<Elemento> _descartados = new();

        private class ErrorDeLinea : Exception
        {
            public ErrorCompilacion Error { get; }
            public ErrorDeLinea(ErrorCompilacion e) { Error = e; }
        }

        public Archivo Analizar(List<Token> tokens, HashSet<int> lineasConErrorLexico)
        {
            var archivo = new Archivo();
            bool hayContenido = false, hayLineaFormato = false;

            foreach (var grupo in tokens.GroupBy(t => t.Linea).OrderBy(g => g.Key))
            {
                _tokens = grupo.ToList();
                _pos = 0;
                Token primero = _tokens[0];
                if (primero.Tipo == TipoToken.Formato) hayLineaFormato = true;

                if (lineasConErrorLexico.Contains(grupo.Key))
                {
                    // La línea no se analiza, pero se respeta su estructura para no generar errores en cadena.
                    if (primero.Tipo == TipoToken.Seccion) _abiertas.Push(null);
                    else if (primero.Tipo == TipoToken.FinSeccion && _abiertas.Count > 0) _abiertas.Pop();
                    if (primero.Tipo != TipoToken.Formato) hayContenido = true;
                    continue;
                }

                try
                {
                    switch (primero.Tipo)
                    {
                        case TipoToken.Formato:
                            Formato(archivo, hayContenido);
                            break;
                        case TipoToken.Config:
                            hayContenido = true;
                            Contenedor(archivo).Add(Config());
                            break;
                        case TipoToken.Seccion:
                            hayContenido = true;
                            AbrirSeccion(archivo);
                            break;
                        case TipoToken.FinSeccion:
                            hayContenido = true;
                            CerrarSeccion();
                            break;
                        default:
                            hayContenido = true;
                            throw InstruccionDesconocida(primero);
                    }
                }
                catch (ErrorDeLinea e)
                {
                    Errores.Add(e.Error);
                    if (primero.Tipo == TipoToken.Seccion) _abiertas.Push(null);
                }
            }

            foreach (var s in _abiertas.Where(s => s != null))
                Errores.Add(new ErrorCompilacion(Fase.Sintactico, s!.Linea, s.Nombre.Columna,
                    $"La sección \"{s.Nombre.Lexema}\" no se cerró.", "Agrega FINSECCION al final de la sección."));

            if (archivo.Formato == null && tokens.Count > 0 && !hayLineaFormato)
                Errores.Add(new ErrorCompilacion(Fase.Sintactico, tokens[0].Linea, 1,
                    "Falta indicar el formato de salida al inicio del archivo.", "Escribe FORMATO JSON o FORMATO XML en la primera línea."));

            return archivo;
        }

        // ------------------------------------------------------------ Instrucciones

        private void Formato(Archivo archivo, bool hayContenido)
        {
            Token t = Avanzar();
            if (archivo.Formato != null)
                throw Falla(t.Linea, t.Columna, $"Solo puede haber una instrucción FORMATO (ya está en la línea {archivo.Formato.Linea}).",
                    "Borra esta línea o cambia el formato de la primera.");
            if (hayContenido)
                throw Falla(t.Linea, t.Columna, "FORMATO debe ser la primera instrucción del archivo.", "Mueve esta línea al inicio.");

            Token? f = Actual;
            if (f == null)
                throw Falla(t.Linea, t.ColumnaFinal, "Falta el formato después de FORMATO.", "Escribe FORMATO JSON o FORMATO XML.");
            if (f.Tipo != TipoToken.NombreFormato)
            {
                string mayus = f.Lexema.ToUpperInvariant();
                throw mayus is "JSON" or "XML"
                    ? Falla(f.Linea, f.Columna, $"\"{f.Lexema}\" no se reconoce como formato.", $"Los formatos van en mayúsculas: FORMATO {mayus}")
                    : Falla(f.Linea, f.Columna, $"El formato \"{f.Lexema}\" no está soportado.", "Los formatos disponibles son JSON y XML.");
            }
            Avanzar();
            Fin();
            archivo.Formato = f;
        }

        private Clave Config()
        {
            Token t = Avanzar();
            Token nombre = NombreDe(t, "CONFIG", "CONFIG puerto = 3100");

            Token? tipo = null;
            if (Actual?.Tipo == TipoToken.DosPuntos)
            {
                Token dp = Avanzar();
                if (Actual?.Tipo != TipoToken.Tipo)
                    throw Falla(dp.Linea, Actual?.Columna ?? dp.ColumnaFinal, "Después de \":\" va el tipo de la clave.",
                        "Tipos: TEXTO, ENTERO, DECIMAL, BOOLEANO o LISTA. Ejemplo: CONFIG puerto : ENTERO = 3100");
                tipo = Avanzar();
            }

            if (Actual?.Tipo != TipoToken.Asignacion)
            {
                Token r = Actual ?? _tokens[_pos - 1];
                throw Falla(r.Linea, Actual == null ? r.ColumnaFinal : r.Columna,
                    Actual == null ? $"Falta \"=\" y el valor de \"{nombre.Lexema}\"." : $"Se esperaba \"=\" después de \"{nombre.Lexema}\" y se encontró \"{Actual.Lexema}\".",
                    $"Ejemplo: CONFIG {nombre.Lexema} = \"valor\"");
            }
            Avanzar();

            Valor valor = LeerValor();
            Fin();
            return new Clave { Nombre = nombre, TipoAnotado = tipo, Valor = valor };
        }

        private void AbrirSeccion(Archivo archivo)
        {
            Token t = Avanzar();
            Token nombre = NombreDe(t, "SECCION", "SECCION servidor");
            Fin();
            var seccion = new Seccion { Nombre = nombre };
            Contenedor(archivo).Add(seccion);
            _abiertas.Push(seccion);
        }

        private void CerrarSeccion()
        {
            Token t = Avanzar();
            Fin();
            if (_abiertas.Count == 0)
                throw Falla(t.Linea, t.Columna, "FINSECCION no tiene una SECCION abierta.", "Revisa que cada SECCION tenga un solo FINSECCION.");
            var s = _abiertas.Pop();
            if (s != null) s.LineaFin = t.Linea;
        }

        private Token NombreDe(Token instruccion, string palabra, string ejemplo)
        {
            Token? n = Actual;
            if (n == null)
                throw Falla(instruccion.Linea, instruccion.ColumnaFinal, $"Falta el nombre después de {palabra}.", $"Ejemplo: {ejemplo}");
            if (n.Tipo == TipoToken.Clave) return Avanzar();
            if (n.Tipo is TipoToken.Formato or TipoToken.NombreFormato or TipoToken.Config or TipoToken.Seccion
                       or TipoToken.FinSeccion or TipoToken.Tipo or TipoToken.Booleano)
                throw Falla(n.Linea, n.Columna, $"\"{n.Lexema}\" es una palabra reservada y no puede usarse como nombre.",
                    $"Escríbelo en minúsculas o elige otro nombre: {n.Lexema.ToLowerInvariant()}_valor");
            if (n.Tipo == TipoToken.Texto)
                throw Falla(n.Linea, n.Columna, "El nombre de una clave no lleva comillas.", $"Ejemplo: {ejemplo}");
            throw Falla(n.Linea, n.Columna, $"Se esperaba el nombre después de {palabra} y se encontró \"{n.Lexema}\".",
                "Un nombre empieza con una letra y solo tiene letras, números y _.");
        }

        private Valor LeerValor()
        {
            Token previo = _tokens[_pos - 1];
            Token? t = Actual;
            if (t == null || t.Tipo == TipoToken.FinSentencia)
                throw Falla(previo.Linea, previo.ColumnaFinal, "Falta el valor después de \"=\".",
                    "Puede ser un texto entre comillas, un número, verdadero / falso o una lista [ … ].");

            if (t.EsValorSimple) { Avanzar(); return new ValorSimple { Token = t }; }

            if (t.Tipo == TipoToken.CorcheteAbre)
            {
                Avanzar();
                var lista = new ValorLista { Token = t };
                if (Actual?.Tipo == TipoToken.CorcheteCierra) { Avanzar(); return lista; }
                while (true)
                {
                    Token? e = Actual;
                    if (e == null)
                        throw Falla(t.Linea, _tokens[^1].ColumnaFinal, $"Falta cerrar la lista abierta en la columna {t.Columna}.", "Agrega \"]\" al final.");
                    if (e.Tipo == TipoToken.CorcheteAbre)
                        throw Falla(e.Linea, e.Columna, "Una lista no puede contener otra lista.", "Usa una SECCION para agrupar datos.");
                    if (e.Tipo == TipoToken.Clave)
                        throw SinComillas(e);
                    if (!e.EsValorSimple)
                        throw Falla(e.Linea, e.Columna, $"Se esperaba un valor de la lista y se encontró \"{e.Lexema}\".", "");
                    lista.Elementos.Add(Avanzar());

                    if (Actual?.Tipo == TipoToken.Coma) { Avanzar(); continue; }
                    if (Actual?.Tipo == TipoToken.CorcheteCierra) { Avanzar(); return lista; }
                    if (Actual == null)
                        throw Falla(t.Linea, _tokens[^1].ColumnaFinal, $"Falta cerrar la lista abierta en la columna {t.Columna}.", "Agrega \"]\" al final.");
                    throw Falla(Actual.Linea, Actual.Columna, $"Falta una coma antes de \"{Actual.Lexema}\".", "Separa los valores de la lista con comas.");
                }
            }

            if (t.Tipo == TipoToken.Clave || t.Tipo == TipoToken.NombreFormato || t.Tipo == TipoToken.Tipo) throw SinComillas(t);
            throw Falla(t.Linea, t.Columna, $"\"{t.Lexema}\" no es un valor válido.",
                "Puede ser un texto entre comillas, un número, verdadero / falso o una lista [ … ].");
        }

        private ErrorDeLinea SinComillas(Token t)
        {
            string resto = string.Join(" ", _tokens.Skip(_pos).TakeWhile(x => x.Tipo is TipoToken.Clave or TipoToken.Entero or TipoToken.Decimal).Select(x => x.Lexema));
            string sugerido = string.IsNullOrEmpty(resto) ? t.Lexema : resto;
            string? booleano = t.Lexema.ToLowerInvariant() is "true" or "si" or "sí" ? "verdadero" : t.Lexema.ToLowerInvariant() is "false" or "no" ? "falso" : null;
            return Falla(t.Linea, t.Columna, $"\"{t.Lexema}\" no es un valor: los textos van entre comillas.",
                booleano != null ? $"Si es un booleano escribe {booleano}; si es un texto, \"{sugerido}\"." : $"Escribe \"{sugerido}\"");
        }

        /// <summary>El ";" final es opcional; después no puede quedar nada.</summary>
        private void Fin()
        {
            if (Actual?.Tipo == TipoToken.FinSentencia) Avanzar();
            if (Actual == null) return;
            Token t = Actual;
            if (t.Tipo == TipoToken.Coma)
                throw Falla(t.Linea, t.Columna, "Sobra \",\" al final de la instrucción.", "Si son varios valores, escríbelos como lista: [1, 2, 3]");
            throw Falla(t.Linea, t.Columna, $"Sobra \"{t.Lexema}\" al final de la instrucción.", "Escribe una sola instrucción por línea.");
        }

        private ErrorDeLinea InstruccionDesconocida(Token t)
        {
            string mayus = t.Lexema.ToUpperInvariant();
            if (t.Tipo == TipoToken.Clave && AnalizadorLexico.EsPalabraClave(mayus))
                return Falla(t.Linea, t.Columna, $"\"{t.Lexema}\" no se reconoce como palabra clave.", $"Las palabras clave van en mayúsculas: {mayus}");
            if (t.Tipo == TipoToken.Clave && Siguiente?.Tipo is TipoToken.Asignacion or TipoToken.DosPuntos)
                return Falla(t.Linea, t.Columna, $"Falta CONFIG antes de \"{t.Lexema}\".", $"Escribe: CONFIG {t.Lexema} = …");
            return Falla(t.Linea, t.Columna, $"Una línea no puede empezar con \"{t.Lexema}\".",
                "Cada línea empieza con FORMATO, CONFIG, SECCION o FINSECCION.");
        }

        // ------------------------------------------------------------ Utilidades

        private List<Elemento> Contenedor(Archivo archivo) =>
            _abiertas.Count == 0 ? archivo.Elementos : _abiertas.Peek()?.Elementos ?? _descartados;

        private Token? Actual => _pos < _tokens.Count ? _tokens[_pos] : null;
        private Token? Siguiente => _pos + 1 < _tokens.Count ? _tokens[_pos + 1] : null;
        private Token Avanzar() => _tokens[_pos++];

        private static ErrorDeLinea Falla(int linea, int columna, string mensaje, string sugerencia) =>
            new(new ErrorCompilacion(Fase.Sintactico, linea, columna, mensaje, sugerencia));
    }
}
