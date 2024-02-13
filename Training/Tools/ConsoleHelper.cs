namespace Tools
{
    public static class ConsoleHelper
    {
        public static void WriteLineColor(string text, ConsoleColor? color = null)
        {
            if (color != null)
                Console.ForegroundColor = (ConsoleColor)color;
            else
                Console.ResetColor();
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteColor(string text, ConsoleColor? color = null)
        {
            if (color != null)
                Console.ForegroundColor = (ConsoleColor)color;
            else
                Console.ResetColor();
            Console.Write(text);
            Console.ResetColor();
        }
    }
}
