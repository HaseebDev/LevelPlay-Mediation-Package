using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Logging for the package. Diagnostic output is compiled OUT of release
    /// builds; errors always survive.
    ///
    /// <see cref="Info"/> and <see cref="Warn"/> carry
    /// <see cref="ConditionalAttribute"/> for UNITY_EDITOR and DEVELOPMENT_BUILD, so
    /// in a release build the compiler removes the call ENTIRELY, arguments
    /// included. That matters because most calls here interpolate a string: a
    /// runtime `if (enabled)` check would still pay to build every message before
    /// throwing it away, whereas this costs nothing at all.
    ///
    /// <see cref="Error"/> and <see cref="Exception"/> are deliberately NOT
    /// conditional. They fire only when something is genuinely broken (a missing
    /// prefab, a failed consent push), and a shipping game that fails silently is
    /// far worse to diagnose than one that leaves a line in the device log.
    /// </summary>
    public static class AdLog
    {
        private const string Prefix = "[Autech.LevelPlay] ";

        /// <summary>
        /// True when diagnostic output is compiled in: the Editor, or a build made
        /// with "Development Build" ticked. Useful for guarding work that only
        /// exists to produce a log message, such as building a debug snapshot.
        /// </summary>
        public static bool DiagnosticsEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Routine progress. Stripped from release builds.</summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        /// <summary>Something unexpected but recoverable. Stripped from release builds.</summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        /// <summary>Something is broken. Always logged, including in release builds.</summary>
        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }

        /// <summary>An exception worth the stack trace. Always logged.</summary>
        public static void Exception(System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
