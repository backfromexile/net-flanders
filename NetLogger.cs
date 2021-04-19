using System;

namespace NetFlanders
{
    internal sealed class NetLogger
    {
        private readonly string? _prefix;

        public NetLogger(string? prefix)
        {
            if (prefix is string)
            {
                _prefix = prefix + ' ';
            }
        }

        public NetLogger() : this(null)
        {
        }

        public void Log(string message)
        {
            Console.WriteLine(_prefix + message);
        }

        public void LogWarning(string message)
        {
            LogColored(message, ConsoleColor.Yellow);
        }

        public void LogError(string message)
        {
            LogColored(message, ConsoleColor.Red);
        }

        private void LogColored(string message, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.WriteLine(_prefix + message);
            Console.ForegroundColor = oldColor;
        }
    }
}
