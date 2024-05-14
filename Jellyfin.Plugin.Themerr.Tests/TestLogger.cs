namespace Jellyfin.Plugin.Themerr.Tests
{
    /// <summary>
    /// A simple logger for tests.
    /// </summary>
    public static class TestLogger
    {
        // log a message to console
        private static ITestOutputHelper? _output;

        /// <summary>
        /// Initializes the logger with the given output.
        /// </summary>
        /// <param name="output">The output to log to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the output is null.</exception>
        public static void Initialize(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        /// <summary>
        /// Logs a critical message to the test output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Critical(string message)
        {
            Log(message, "CRITICAL");
        }

        /// <summary>
        /// Logs a debug message to the test output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Debug(string message)
        {
            Log(message, "DEBUG");
        }

        /// <summary>
        /// Logs an error message to the test output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Error(string message)
        {
            Log(message, "ERROR");
        }

        /// <summary>
        /// Logs an info message to the test output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Info(string message)
        {
            Log(message);
        }

        /// <summary>
        /// Logs a warning message to the test output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Warn(string message)
        {
            Log(message, "WARN");
        }

        /// <summary>
        /// Logs a message to the test output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="type">The type of message to log.</param>
        private static void Log(string message, string type = "INFO")
        {
            _output?.WriteLine($"[{type}] {message}");
        }
    }
}
