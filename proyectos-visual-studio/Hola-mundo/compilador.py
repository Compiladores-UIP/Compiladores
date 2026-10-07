#!/usr/bin/env python3
"""Mini-compilador "Hola Mundo": IMPRIMIR "texto"  ->  C# (.cs)  ->  .exe
Fases: léxico -> sintáctico -> semántico -> generación de código -> compilación -> ejecución.
Uso:  python compilador.py ejemplos/correcto.txt
"""
import re, sys, os, shutil, subprocess

# ---------- 1. ANÁLISIS LÉXICO ----------
ESPEC = [
    ("IMPRIMIR",  r"IMPRIMIR\b"),
    ("CADENA",    r'"[^"\n]*"'),
    ("PUNTOCOMA", r";"),
    ("NL",        r"\n"),
    ("ESPACIO",   r"[ \t\r]+"),
    ("ERROR",     r"."),
]
PATRON = re.compile("|".join(f"(?P<{n}>{p})" for n, p in ESPEC))

def lexer(src):
    tokens, linea, ini = [], 1, 0
    for m in PATRON.finditer(src):
        tipo, val, col = m.lastgroup, m.group(), m.start() - ini + 1
        if tipo == "NL":
            linea += 1; ini = m.end()
        elif tipo == "ESPACIO":
            pass
        elif tipo == "ERROR":
            raise SyntaxError(f"[Léxico] Línea {linea}, col {col}: símbolo no reconocido {val!r}")
        else:
            tokens.append((tipo, val, linea))
    tokens.append(("EOF", "", linea))
    return tokens

# ---------- 2. ANÁLISIS SINTÁCTICO ----------
# programa  -> sentencia+ EOF
# sentencia -> IMPRIMIR CADENA [ ; ]
def parser(tokens):
    i, ast = 0, []
    while tokens[i][0] != "EOF":
        if tokens[i][0] != "IMPRIMIR":
            raise SyntaxError(f"[Sintáctico] Línea {tokens[i][2]}: se esperaba IMPRIMIR")
        if tokens[i + 1][0] != "CADENA":
            raise SyntaxError(f"[Sintáctico] Línea {tokens[i][2]}: se esperaba una cadena entre comillas después de IMPRIMIR")
        ast.append(("Imprimir", tokens[i + 1][1][1:-1], tokens[i][2]))
        i += 2
        if tokens[i][0] == "PUNTOCOMA":
            i += 1
    if not ast:
        raise SyntaxError("[Sintáctico] El programa está vacío")
    return ast

# ---------- 3. VALIDACIÓN SEMÁNTICA BÁSICA ----------
def semantico(ast):
    for _, texto, linea in ast:
        if texto.strip() == "":
            raise ValueError(f"[Semántico] Línea {linea}: IMPRIMIR no puede recibir una cadena vacía")

# ---------- 4. GENERACIÓN DE CÓDIGO C# ----------
def generar(ast):
    esc = lambda s: s.replace("\\", "\\\\").replace('"', '\\"')
    cuerpo = "\n".join(f'        Console.WriteLine("{esc(t)}");' for _, t, _ in ast)
    return f"using System;\n\nclass Programa\n{{\n    static void Main()\n    {{\n{cuerpo}\n    }}\n}}\n"

# ---------- 5. COMPILACIÓN (.exe) ----------
def buscar_compilador():
    for nombre in ("csc", "mcs"):
        if shutil.which(nombre):
            return nombre
    for ruta in (r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                 r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"):
        if os.path.exists(ruta):
            return ruta
    return None

def compilar(cs, exe):
    comp = buscar_compilador()
    if not comp:
        return False, "No se encontró csc/mcs. Compile manualmente: csc salida\\Hola.cs"
    opcion = f"-out:{exe}" if comp.endswith("mcs") else f"/out:{exe}"
    r = subprocess.run([comp, opcion, cs], capture_output=True, text=True)
    return r.returncode == 0, (r.stdout + r.stderr).strip()

def main():
    ruta = sys.argv[1] if len(sys.argv) > 1 else "ejemplos/correcto.txt"
    src = open(ruta, encoding="utf-8").read()
    print("== CÓDIGO FUENTE ==\n" + src)
    try:
        tokens = lexer(src)
        print("== 1. LÉXICO ==")
        for t in tokens: print(f"  {t[0]:<10} {t[1]!r}")
        ast = parser(tokens)
        print("== 2. SINTÁCTICO ==")
        for n, t, _ in ast: print(f"  {n}(texto={t!r})")
        semantico(ast)
        print("== 3. SEMÁNTICO ==\n  OK")
    except (SyntaxError, ValueError) as e:
        print("ERROR CONTROLADO:", e, "\nNo se genera código.")
        sys.exit(1)
    cs = generar(ast)
    os.makedirs("salida", exist_ok=True)
    open("salida/Hola.cs", "w", encoding="utf-8").write(cs)
    print("== 4. CÓDIGO C# (salida/Hola.cs) ==\n" + cs)
    ok, msg = compilar("salida/Hola.cs", "salida/Hola.exe")
    print("== 5. COMPILACIÓN ==")
    if ok:
        print("  OK -> salida/Hola.exe\n== 6. EJECUCIÓN ==")
        print(subprocess.run(["salida/Hola.exe"], capture_output=True, text=True).stdout)
    else:
        print("  " + (msg or "Error al compilar"))

if __name__ == "__main__":
    main()
