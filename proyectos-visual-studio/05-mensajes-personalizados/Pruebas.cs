using MensajesPersonalizados.Compilador;

namespace MensajesPersonalizados;

/// <summary>Pruebas automáticas sin dependencias externas: dotnet run -- --probar</summary>
public static class Pruebas
{
    private sealed record Caso(string Nombre, string Fuente, FaseError? ErrorEsperado = null, string? CodigoEsperado = null);

    private static readonly Caso[] Casos =
    {
        new("Mensaje simple", "MOSTRAR \"Hola\"", CodigoEsperado: "Console.WriteLine(\"Hola\");"),
        new("Concatenación texto + variable",
            "TEXTO nombre = \"Ana\"\nMOSTRAR \"Bienvenido, \" + nombre",
            CodigoEsperado: "Console.WriteLine(\"Bienvenido, \" + nombre);"),
        new("Plantilla con marcador",
            "TEXTO nombre = \"Ana\"\nMOSTRAR \"Hola {nombre}\"",
            CodigoEsperado: "Console.WriteLine($\"Hola {nombre}\");"),
        new("Llaves literales en plantilla",
            "TEXTO x = \"a\"\nMOSTRAR \"{{x}} vale {x}\"",
            CodigoEsperado: "$\"{{x}} vale {x}\""),
        new("Número dentro del mensaje",
            "ENTERO edad = 20\nMOSTRAR \"Edad: \" + edad",
            CodigoEsperado: "int edad = 20;"),
        new("Operación numérica en mensaje",
            "DECIMAL precio = 2.5\nENTERO cantidad = 4\nMOSTRAR \"Total: \" + (precio * cantidad)",
            CodigoEsperado: "\"Total: \" + (precio * cantidad)"),
        new("Funciones de texto",
            "TEXTO n = \"ana\"\nMOSTRAR MAYUSCULAS(n) + \" tiene \" + LONGITUD(n) + \" letras\"",
            CodigoEsperado: "n.ToUpperInvariant() + \" tiene \" + n.Length"),
        new("Entrada del usuario",
            "TEXTO nombre\nPEDIR nombre \"¿Nombre?\"\nMOSTRAR \"Hola {nombre}\"",
            CodigoEsperado: "nombre = Console.ReadLine() ?? \"\";"),
        new("Entrada numérica genera ayudante",
            "ENTERO edad\nPEDIR edad\nMOSTRAR \"{edad}\"",
            CodigoEsperado: "static int LeerEntero"),
        new("Escapes de cadena", "MOSTRAR \"Dijo \\\"hola\\\"\\n\"", CodigoEsperado: "\"Dijo \\\"hola\\\"\\n\""),
        new("Nombre que es palabra clave de C#", "TEXTO class = \"x\"\nMOSTRAR class", CodigoEsperado: "string @class"),
        new("Comentarios y líneas vacías", "# saludo\n\nMOSTRAR \"ok\" // fin\n", CodigoEsperado: "Console.WriteLine(\"ok\");"),

        new("Léxico: cadena sin cerrar", "MOSTRAR \"Hola", FaseError.Lexico),
        new("Léxico: símbolo inválido", "MOSTRAR \"a\" & \"b\"", FaseError.Lexico),
        new("Léxico: escape inválido", "MOSTRAR \"a\\q\"", FaseError.Lexico),
        new("Sintáctico: falta mensaje", "MOSTRAR", FaseError.Sintactico),
        new("Sintáctico: operador sin operando", "MOSTRAR \"Hola \" +", FaseError.Sintactico),
        new("Sintáctico: minúsculas", "mostrar \"Hola\"", FaseError.Sintactico),
        new("Sintáctico: marcador sin cerrar", "TEXTO n = \"a\"\nMOSTRAR \"Hola {n\"", FaseError.Sintactico),
        new("Sintáctico: paréntesis sin cerrar", "TEXTO n = \"a\"\nMOSTRAR MAYUSCULAS(n", FaseError.Sintactico),
        new("Semántico: variable no declarada", "MOSTRAR \"Hola \" + nombre", FaseError.Semantico),
        new("Semántico: marcador no declarado", "MOSTRAR \"Hola {nombre}\"", FaseError.Semantico),
        new("Semántico: redeclaración", "TEXTO a = \"x\"\nTEXTO a = \"y\"\nMOSTRAR a", FaseError.Semantico),
        new("Semántico: tipo incompatible", "ENTERO edad = \"veinte\"\nMOSTRAR edad", FaseError.Semantico),
        new("Semántico: resta con texto", "TEXTO a = \"x\"\nMOSTRAR a - 1", FaseError.Semantico),
        new("Semántico: función con número", "MOSTRAR MAYUSCULAS(5)", FaseError.Semantico),
        new("Semántico: división entre cero", "ENTERO a = 4 / 0\nMOSTRAR a", FaseError.Semantico),
    };

    public static int Ejecutar()
    {
        int fallos = 0;
        foreach (Caso caso in Casos)
        {
            ResultadoCompilacion r = CompiladorMensajes.Compilar(caso.Fuente);
            string? problema = null;

            if (caso.ErrorEsperado is FaseError fase)
            {
                if (r.Exitoso) problema = "se esperaba un error y compiló.";
                else if (r.Errores[0].Fase != fase) problema = $"se esperaba error {fase} y se obtuvo: {r.Errores[0]}";
            }
            else if (!r.Exitoso)
            {
                problema = "no compiló: " + string.Join(" | ", r.Errores);
            }
            else if (caso.CodigoEsperado is string esperado && !r.CodigoCSharp!.Contains(esperado, StringComparison.Ordinal))
            {
                problema = $"el C# generado no contiene: {esperado}";
            }

            Console.WriteLine($"{(problema is null ? "OK  " : "FALLA")} {caso.Nombre}{(problema is null ? "" : " → " + problema)}");
            if (problema is not null) fallos++;
        }

        Console.WriteLine();
        Console.WriteLine($"{Casos.Length - fallos}/{Casos.Length} pruebas correctas.");
        return fallos == 0 ? 0 : 1;
    }
}
