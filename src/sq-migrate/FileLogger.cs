namespace sq_migrate
{
    internal static class FileLogger
    {
        private static readonly object Sync = new();
        private static readonly string _logFilePath = Path.Combine(
            AppContext.BaseDirectory,
            "logs",
            $"sq-migrate-{DateTime.UtcNow:yyyyMMdd}.log");

        public static string LogFilePath => _logFilePath;

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception? exception = null)
        {
            if (exception is null)
            {
                Write("ERROR", message);
                return;
            }

            Write("ERROR", $"{message}{Environment.NewLine}{exception}");
        }

        private static void Write(string level, string message)
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";

                lock (Sync)
                {
                    File.AppendAllText(_logFilePath, line);
                }
            }
            catch
            {
            }
        }
    }
}
