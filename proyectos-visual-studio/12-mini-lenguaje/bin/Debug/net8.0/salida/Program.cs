using System;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        int total = 0;
        for (int i = 1; (i <= 5); i = (i + 1)) {
            total = (total + i);
        }
        if ((total >= 15)) {
            Console.WriteLine(("Total: " + total));
        } else {
            Console.WriteLine("Revisa el cálculo");
        }
        ConsoleWindow.WaitIfOpenedSeparately();
    }
}
internal static class ConsoleWindow
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint GetConsoleProcessList([System.Runtime.InteropServices.Out] uint[] processes, uint count);

    public static void WaitIfOpenedSeparately()
    {
        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected || Console.IsOutputRedirected) return;
        // Una consola con un solo proceso se cierra cuando termina este programa.
        if (GetConsoleProcessList(new uint[2], 2) != 1) return;
        Console.WriteLine("\nPulsa una tecla para cerrar...");
        Console.ReadKey(true);
    }
}