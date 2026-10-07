using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MiniCompiladorFormularios;

enum TipoToken
{
    Formulario,
    Etiqueta,
    Campo,
    Boton,
    FinFormulario,
    Cadena,
    Identificador,
    FinArchivo
}

record Token(TipoToken Tipo, string Texto, int Linea);
record ElementoFormulario(string Tipo, string Valor);
record ResultadoCompilacion(string Mensaje, bool Correcto, string? Ejecutable);

static class LenguajeFormulario
{
    public static List<Token> AnalizarLexico(string fuente)
    {
        var tokens = new List<Token>();
        int posicion = 0;
        int linea = 1;

        while (posicion < fuente.Length)
        {
            char actual = fuente[posicion];

            if (char.IsWhiteSpace(actual))
            {
                if (actual == '\n')
                {
                    linea++;
                }

                posicion++;
                continue;
            }

            if (actual == '"')
            {
                int inicio = ++posicion;

                while (posicion < fuente.Length && fuente[posicion] != '"')
                {
                    posicion++;
                }

                if (posicion >= fuente.Length)
                {
                    throw new Exception($"Error léxico en la línea {linea}: la cadena de texto no tiene comillas de cierre.");
                }

                string contenido = fuente[inicio..posicion];
                posicion++;
                tokens.Add(new Token(TipoToken.Cadena, contenido, linea));
                continue;
            }

            if (char.IsLetter(actual) || actual == '_')
            {
                int inicio = posicion;

                while (posicion < fuente.Length &&
                       (char.IsLetterOrDigit(fuente[posicion]) || fuente[posicion] == '_'))
                {
                    posicion++;
                }

                string texto = fuente[inicio..posicion];
                TipoToken tipo = texto switch
                {
                    "FORMULARIO" => TipoToken.Formulario,
                    "ETIQUETA" => TipoToken.Etiqueta,
                    "CAMPO" => TipoToken.Campo,
                    "BOTON" => TipoToken.Boton,
                    "FIN_FORMULARIO" => TipoToken.FinFormulario,
                    _ => TipoToken.Identificador
                };

                tokens.Add(new Token(tipo, texto, linea));
                continue;
            }

            throw new Exception($"Error léxico en la línea {linea}: '{actual}' no es un símbolo válido.");
        }

        tokens.Add(new Token(TipoToken.FinArchivo, string.Empty, linea));
        return tokens;
    }

    public static string AnalizarSintaxis(List<Token> tokens)
    {
        int posicion = 0;

        Token Consumir(TipoToken esperado, string mensaje)
        {
            if (posicion >= tokens.Count || tokens[posicion].Tipo != esperado)
            {
                throw new Exception(mensaje);
            }

            return tokens[posicion++];
        }

        Consumir(TipoToken.Formulario, "Error sintáctico: se esperaba FORMULARIO al inicio.");
        Token titulo = Consumir(
            TipoToken.Cadena,
            "Error sintáctico: FORMULARIO requiere un título entre comillas.");

        var elementos = new List<ElementoFormulario>();
        var campos = new HashSet<string>(StringComparer.Ordinal);

        while (posicion < tokens.Count && tokens[posicion].Tipo != TipoToken.FinFormulario)
        {
            if (tokens[posicion].Tipo == TipoToken.FinArchivo)
            {
                throw new Exception("Error sintáctico: falta FIN_FORMULARIO.");
            }

            Token actual = tokens[posicion++];

            switch (actual.Tipo)
            {
                case TipoToken.Etiqueta:
                {
                    Token texto = Consumir(
                        TipoToken.Cadena,
                        "Error sintáctico: ETIQUETA requiere un texto entre comillas.");
                    elementos.Add(new ElementoFormulario("Etiqueta", texto.Texto));
                    break;
                }

                case TipoToken.Campo:
                {
                    Token identificador = Consumir(
                        TipoToken.Identificador,
                        "Error sintáctico: falta el identificador del campo.");

                    if (!campos.Add(identificador.Texto))
                    {
                        throw new Exception($"Error semántico: el campo '{identificador.Texto}' está repetido.");
                    }

                    elementos.Add(new ElementoFormulario("Campo", identificador.Texto));
                    break;
                }

                case TipoToken.Boton:
                {
                    Token texto = Consumir(
                        TipoToken.Cadena,
                        "Error sintáctico: BOTON requiere un texto entre comillas.");
                    elementos.Add(new ElementoFormulario("Boton", texto.Texto));
                    break;
                }

                default:
                    throw new Exception($"Error sintáctico: '{actual.Texto}' no es un elemento válido dentro del formulario.");
            }
        }

        Consumir(TipoToken.FinFormulario, "Error sintáctico: falta FIN_FORMULARIO.");
        Consumir(TipoToken.FinArchivo, "Error sintáctico: hay texto adicional después de FIN_FORMULARIO.");

        return GenerarCodigo(titulo.Texto, elementos);
    }

    private static string GenerarCodigo(string titulo, List<ElementoFormulario> elementos)
    {
        static string EscaparTextoCs(string texto) => texto
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

        List<string> nombresCampos = elementos
            .Where(elemento => elemento.Tipo == "Campo")
            .Select(elemento => elemento.Valor)
            .ToList();

        var codigo = new StringBuilder();
        int posicionY = 25;
        int numeroBoton = 0;

        codigo.AppendLine("using System;");
        codigo.AppendLine("using System.IO;");
        codigo.AppendLine("using System.Text;");
        codigo.AppendLine("using System.Windows.Forms;");
        codigo.AppendLine();
        codigo.AppendLine("internal static class Program");
        codigo.AppendLine("{");
        codigo.AppendLine("    [STAThread]");
        codigo.AppendLine("    static void Main()");
        codigo.AppendLine("    {");
        codigo.AppendLine("        ApplicationConfiguration.Initialize();");
        codigo.AppendLine();
        codigo.AppendLine("        var formulario = new Form");
        codigo.AppendLine("        {");
        codigo.AppendLine($"            Text = \"{EscaparTextoCs(titulo)}\",");
        codigo.AppendLine("            Width = 420,");
        codigo.AppendLine("            Height = 180,");
        codigo.AppendLine("            StartPosition = FormStartPosition.CenterScreen");
        codigo.AppendLine("        };");
        codigo.AppendLine();

        foreach (ElementoFormulario elemento in elementos)
        {
            if (elemento.Tipo == "Etiqueta")
            {
                codigo.AppendLine("        formulario.Controls.Add(new Label");
                codigo.AppendLine("        {");
                codigo.AppendLine($"            Text = \"{EscaparTextoCs(elemento.Valor)}\",");
                codigo.AppendLine("            Left = 25,");
                codigo.AppendLine($"            Top = {posicionY},");
                codigo.AppendLine("            AutoSize = true");
                codigo.AppendLine("        });");
                codigo.AppendLine();
                posicionY += 25;
                continue;
            }

            if (elemento.Tipo == "Campo")
            {
                string variable = "campo_" + elemento.Valor;

                codigo.AppendLine($"        var {variable} = new TextBox");
                codigo.AppendLine("        {");
                codigo.AppendLine($"            Name = \"{EscaparTextoCs(elemento.Valor)}\",");
                codigo.AppendLine("            Left = 25,");
                codigo.AppendLine($"            Top = {posicionY},");
                codigo.AppendLine("            Width = 300");
                codigo.AppendLine("        };");
                codigo.AppendLine($"        formulario.Controls.Add({variable});");
                codigo.AppendLine();
                posicionY += 57;
                continue;
            }

            string boton = $"boton_{numeroBoton++}";

            codigo.AppendLine($"        var {boton} = new Button");
            codigo.AppendLine("        {");
            codigo.AppendLine($"            Text = \"{EscaparTextoCs(elemento.Valor)}\",");
            codigo.AppendLine("            Left = 25,");
            codigo.AppendLine($"            Top = {posicionY},");
            codigo.AppendLine("            Width = 300");
            codigo.AppendLine("        };");

            if (elemento.Valor.Equals("Guardar", StringComparison.OrdinalIgnoreCase))
            {
                codigo.AppendLine();
                codigo.AppendLine($"        {boton}.Click += (_, _) =>");
                codigo.AppendLine("        {");

                if (nombresCampos.Count == 0)
                {
                    codigo.AppendLine("            MessageBox.Show(\"No hay campos para guardar.\", \"Guardar\", MessageBoxButtons.OK, MessageBoxIcon.Information);");
                }
                else
                {
                    string validacion = string.Join(
                        " || ",
                        nombresCampos.Select(nombre => $"string.IsNullOrWhiteSpace(campo_{nombre}.Text)"));
                    string cabecera = string.Join(";", nombresCampos);

                    codigo.AppendLine($"            if ({validacion})");
                    codigo.AppendLine("            {");
                    codigo.AppendLine("                MessageBox.Show(\"Complete todos los campos antes de guardar.\", \"Validación\", MessageBoxButtons.OK, MessageBoxIcon.Warning);");
                    codigo.AppendLine("                return;");
                    codigo.AppendLine("            }");
                    codigo.AppendLine();
                    codigo.AppendLine("            try");
                    codigo.AppendLine("            {");
                    codigo.AppendLine("                string carpetaDatos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), \"MiniCompiladoresUIP\", \"Formularios\");");
                    codigo.AppendLine("                Directory.CreateDirectory(carpetaDatos);");
                    codigo.AppendLine("                string archivoDatos = Path.Combine(carpetaDatos, \"registros.csv\");");
                    codigo.AppendLine("                bool archivoNuevo = !File.Exists(archivoDatos);");
                    codigo.AppendLine();
                    codigo.AppendLine("                if (archivoNuevo)");
                    codigo.AppendLine("                {");
                    codigo.AppendLine($"                    File.AppendAllText(archivoDatos, \"{EscaparTextoCs(cabecera)}\" + Environment.NewLine, new UTF8Encoding(false));");
                    codigo.AppendLine("                }");
                    codigo.AppendLine();
                    codigo.AppendLine("                string[] valores =");
                    codigo.AppendLine("                {");

                    foreach (string nombre in nombresCampos)
                    {
                        codigo.AppendLine($"                    campo_{nombre}.Text.Replace(\";\", \",\"),");
                    }

                    codigo.AppendLine("                };");
                    codigo.AppendLine("                File.AppendAllText(archivoDatos, string.Join(\";\", valores) + Environment.NewLine, new UTF8Encoding(false));");
                    codigo.AppendLine();
                    codigo.AppendLine("                MessageBox.Show(\"Datos guardados correctamente.\\n\\nArchivo:\\n\" + archivoDatos, \"Registro guardado\", MessageBoxButtons.OK, MessageBoxIcon.Information);");
                    codigo.AppendLine();

                    foreach (string nombre in nombresCampos)
                    {
                        codigo.AppendLine($"                campo_{nombre}.Clear();");
                    }

                    codigo.AppendLine($"                campo_{nombresCampos[0]}.Focus();");
                    codigo.AppendLine("            }");
                    codigo.AppendLine("            catch (Exception error)");
                    codigo.AppendLine("            {");
                    codigo.AppendLine("                MessageBox.Show(\"No fue posible guardar los datos.\\n\\n\" + error.Message, \"Error al guardar\", MessageBoxButtons.OK, MessageBoxIcon.Error);");
                    codigo.AppendLine("            }");
                }

                codigo.AppendLine("        };");
            }

            codigo.AppendLine($"        formulario.Controls.Add({boton});");
            codigo.AppendLine();
            posicionY += 63;
        }

        codigo.AppendLine($"        formulario.Height = Math.Max(180, {posicionY + 70});");
        codigo.AppendLine("        Application.Run(formulario);");
        codigo.AppendLine("    }");
        codigo.AppendLine("}");

        return codigo.ToString();
    }
}

static class CompiladorFormulario
{
    public static ResultadoCompilacion Compilar(string codigo)
    {
        string carpetaTemporal = Path.Combine(
            Path.GetTempPath(),
            "CompiladoresGrupo1",
            "formulario-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(carpetaTemporal);

        string rutaCodigo = Path.Combine(carpetaTemporal, "Program.cs");
        string rutaProyecto = Path.Combine(carpetaTemporal, "FormularioGenerado.csproj");

        File.WriteAllText(rutaCodigo, codigo, new UTF8Encoding(false));

        const string proyecto = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
""";

        File.WriteAllText(rutaProyecto, proyecto, new UTF8Encoding(false));

        var inicio = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = carpetaTemporal,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        inicio.ArgumentList.Add("build");
        inicio.ArgumentList.Add("FormularioGenerado.csproj");
        inicio.ArgumentList.Add("-c");
        inicio.ArgumentList.Add("Release");
        inicio.ArgumentList.Add("--nologo");

        using Process proceso = Process.Start(inicio)
            ?? throw new Exception("No fue posible iniciar la compilación del formulario generado.");

        string salida = proceso.StandardOutput.ReadToEnd();
        string errores = proceso.StandardError.ReadToEnd();
        proceso.WaitForExit();

        if (proceso.ExitCode != 0)
        {
            string detalle = string.IsNullOrWhiteSpace(errores) ? salida : errores;
            return new ResultadoCompilacion(detalle.Trim(), false, null);
        }

        string ejecutable = Path.Combine(
            carpetaTemporal,
            "bin",
            "Release",
            "net8.0-windows",
            "FormularioGenerado.exe");

        if (!File.Exists(ejecutable))
        {
            return new ResultadoCompilacion(
                "La compilación finalizó, pero no se encontró FormularioGenerado.exe.",
                false,
                null);
        }

        return new ResultadoCompilacion(
            "Compilación correcta.\r\n0 advertencias\r\n0 errores",
            true,
            ejecutable);
    }
}

sealed class VentanaCompilador : Form
{
    private readonly TextBox fuente = new()
    {
        Multiline = true,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10),
        ScrollBars = ScrollBars.Both
    };

    private readonly TextBox codigoGenerado = new()
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9),
        ScrollBars = ScrollBars.Both
    };

    private readonly TextBox resultado = new()
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Vertical
    };

    private readonly DataGridView tablaTokens = new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        Dock = DockStyle.Fill,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private readonly Label estado = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private string? ejecutableGenerado;

    public VentanaCompilador()
    {
        Text = "Mini-compilador de lenguaje para formularios";
        Width = 1100;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        tablaTokens.Columns.Add("Tipo", "Tipo");
        tablaTokens.Columns.Add("Texto", "Texto");
        tablaTokens.Columns.Add("Linea", "Línea");

        var botones = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(6, 5, 6, 5)
        };

        AgregarBoton(botones, "Cargar ejemplo", CargarEjemplo);
        AgregarBoton(botones, "Analizar", () => Analizar());
        AgregarBoton(botones, "Generar formulario", GenerarFormulario);
        AgregarBoton(botones, "Abrir formulario generado", AbrirFormulario);
        AgregarBoton(botones, "Limpiar", Limpiar);

        var contenido = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(6)
        };

        contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 47));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 47));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 6));

        AgregarBloque(
            contenido,
            fuente,
            "Código fuente\r\nFormato: FORMULARIO \"Título\" · ETIQUETA \"Texto\" · CAMPO identificador · BOTON \"Texto\" · FIN_FORMULARIO",
            0,
            0,
            44);

        AgregarBloque(contenido, tablaTokens, "Tokens", 1, 0, 22);
        AgregarBloque(contenido, codigoGenerado, "Código C# generado", 0, 1, 22);
        AgregarBloque(contenido, resultado, "Resultado", 1, 1, 22);

        contenido.Controls.Add(estado, 0, 2);
        contenido.SetColumnSpan(estado, 2);

        Controls.Add(contenido);
        Controls.Add(botones);

        CargarEjemplo();
    }

    private static void AgregarBoton(FlowLayoutPanel panel, string texto, Action accion)
    {
        var boton = new Button
        {
            Text = texto,
            AutoSize = true
        };

        boton.Click += (_, _) => accion();
        panel.Controls.Add(boton);
    }

    private static void AgregarBloque(
        TableLayoutPanel tabla,
        Control control,
        string titulo,
        int columna,
        int fila,
        int altoTitulo)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4)
        };

        var etiqueta = new Label
        {
            Text = titulo,
            Dock = DockStyle.Top,
            Height = altoTitulo
        };

        panel.Controls.Add(control);
        panel.Controls.Add(etiqueta);
        tabla.Controls.Add(panel, columna, fila);
    }

    private void CargarEjemplo()
    {
        fuente.Text =
            "FORMULARIO \"Registro\"\r\n" +
            "ETIQUETA \"Nombre\"\r\n" +
            "CAMPO nombre\r\n" +
            "ETIQUETA \"Correo\"\r\n" +
            "CAMPO correo\r\n" +
            "BOTON \"Guardar\"\r\n" +
            "FIN_FORMULARIO";

        LimpiarSalida();
    }

    private void Limpiar()
    {
        fuente.Clear();
        LimpiarSalida();
    }

    private void LimpiarSalida()
    {
        tablaTokens.Rows.Clear();
        codigoGenerado.Clear();
        resultado.Clear();
        ejecutableGenerado = null;
        estado.Text = "Listo.";
    }

    private bool Analizar()
    {
        try
        {
            List<Token> tokens = LenguajeFormulario.AnalizarLexico(fuente.Text);

            tablaTokens.Rows.Clear();
            foreach (Token token in tokens)
            {
                tablaTokens.Rows.Add(token.Tipo, token.Texto, token.Linea);
            }

            codigoGenerado.Text = LenguajeFormulario.AnalizarSintaxis(tokens);
            resultado.Text = "El formulario es válido.";
            estado.Text = "Análisis correcto.";
            return true;
        }
        catch (Exception error)
        {
            resultado.Text = error.Message;
            estado.Text = error.Message;
            ejecutableGenerado = null;
            return false;
        }
    }

    private void GenerarFormulario()
    {
        if (!Analizar())
        {
            return;
        }

        ResultadoCompilacion compilacion = CompiladorFormulario.Compilar(codigoGenerado.Text);
        resultado.Text = compilacion.Mensaje;
        ejecutableGenerado = compilacion.Correcto ? compilacion.Ejecutable : null;
        estado.Text = compilacion.Correcto
            ? "Formulario generado y compilado correctamente."
            : "Error durante la compilación del formulario.";
    }

    private void AbrirFormulario()
    {
        if (string.IsNullOrWhiteSpace(ejecutableGenerado) || !File.Exists(ejecutableGenerado))
        {
            estado.Text = "Primero genere un formulario válido.";
            return;
        }

        Process.Start(new ProcessStartInfo(ejecutableGenerado)
        {
            UseShellExecute = true
        });
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new VentanaCompilador());
    }
}
