using System;

namespace makecal
{
  public static class ConsoleHelper
  {
    public static int HeaderHeight { get; } = 10;
    public static int FooterHeight { get; } = 3;

    private static readonly int statusCol = 50;
    private static readonly int statusWidth = 30;
    private static readonly ConsoleColor defaultBackground = Console.BackgroundColor;
    private static readonly object consoleLock = new object();

    public static int MinConsoleWidth => statusCol + statusWidth;

    private static void Write(int line, int col, string text, ConsoleColor? colour = null)
    {
      lock (consoleLock)
      {
        if (colour == null)
        {
          colour = defaultBackground;
        }
        Console.SetCursorPosition(col, line);
        Console.BackgroundColor = defaultBackground;
        Console.Write(new string(' ', statusWidth));
        Console.SetCursorPosition(col, line);
        Console.BackgroundColor = colour.Value;
        Console.Write(text);
        Console.BackgroundColor = defaultBackground;
      }
    }

    public static void WriteDescription(int line, string text, ConsoleColor? colour = null)
    {
      Write(line, 0, text, colour);
    }

    public static void WriteStatus(int line, string text, ConsoleColor? colour = null)
    {
      Write(line, statusCol, text, colour);
    }

    public static void WriteError(string message)
    {
      lock (consoleLock)
      {
        Console.WriteLine();
        var backgroundColor = Console.BackgroundColor;
        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"Error: {message}");
        Console.BackgroundColor = backgroundColor;
        Console.WriteLine();
      }
    }

  }
}
