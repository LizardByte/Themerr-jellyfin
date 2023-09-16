namespace Jellyfin.Plugin.Themerr.Tests
{
    /// <summary>
    /// A simple logger for tests
    /// </summary>
    public static class TestLogger
    {
        // log a message to console
        private static ITestOutputHelper? _output;

        public static void Initialize(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        /// <summary>
        /// Logs a message to the test output
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public static void Log(string message, string type = "INFO")
        {
            _output?.WriteLine($"[{type}] {message}");
        }

        /// <summary>
        /// Logs a critical message to the test output
        /// </summary>
        /// <param name="message"></param>
        public static void Critical(string message)
        {
            Log(message, "CRITICAL");
        }

        /// <summary>
        /// Logs a debug message to the test output
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            Log(message, "DEBUG");
        }

        /// <summary>
        /// Logs an error message to the test output
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            Log(message, "ERROR");
        }

        /// <summary>
        /// Logs an info message to the test output
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            Log(message, "INFO");
        }

        /// <summary>
        /// Logs a warning message to the test output
        /// </summary>
        /// <param name="message"></param>
        public static void Warn(string message)
        {
            Log(message, "WARN");
        }
    }
}
