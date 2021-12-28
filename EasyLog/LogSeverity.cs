using System;

namespace EasyLog
{
    /// <summary>
    /// Possible log levels.
    /// A logger may use any combination of them,
    /// but a log message must only consist of one enumeration value from the list (excluding combined values)
    /// </summary>
    [Flags]
    public enum LogSeverity : int
    {
        /// <summary>
        /// An event that usually causes abnormal program termination.
        /// Events logged by <see cref="EasyLogger.AttachGlobalErrorLogger"/> are always logged this way.
        /// Example: corruption of an important file, missing libraries, out of memory
        /// </summary>
        /// <remarks>
        /// You should only ever log this error in combination with a thrown exception.
        /// If you do not log an exception it's more difficult to fix the error.
        /// If you do not throw the exception, the stack trace will be missing, incomplete, or wrong.
        /// </remarks>
        Critical = 1,
        /// <summary>
        /// An unexpected error.
        /// The application usually doesn't terminates
        /// but pending actions may not complete or information may have been lost.
        /// Example: Unexpected end of network connections, unexpected content in user supplied files
        /// </summary>
        Error = Critical << 1,
        /// <summary>
        /// An event that generally requires user intervention or causes
        /// </summary>
        /// <remarks>
        /// Example: Overwriting of a file, major reset of settings
        /// </remarks>
        Warning = Error << 1,
        /// <summary>
        /// Regular events created by the application.
        /// </summary>
        /// <remarks>
        /// Example: Accessing files, calling important functions, establishing network connections
        /// </remarks>
        Info = Warning << 1,
        /// <summary>
        /// Messages for debugging purposes.
        /// Normally not needed and they may cause the log file to grow very fast
        /// </summary>
        /// <remarks>
        /// Example: Data written to or read from a file or network socket
        /// </remarks>
        Debug = Info << 1,
        /// <summary>
        /// Messages for function calls.
        /// Enabling these will cause the log file size to explode (provided you actually do trace logging)
        /// </summary>
        /// <remarks>
        /// Example: Calling any function
        /// </remarks>
        Trace = Debug << 1,
        /// <summary>
        /// Default log level
        /// </summary>
        CombinedDefaults = Critical | Error | Warning | Info,
        /// <summary>
        /// All possible log levels
        /// </summary>
        CombinedAll = CombinedDefaults | Debug | Trace
    }

    /// <summary>
    /// Easy to use flag masks for log messages
    /// </summary>
    public struct LogSeverityMask
    {
        public const LogSeverity CriticalOrHigher = LogSeverity.Critical;
        public const LogSeverity ErrorOrHigher = CriticalOrHigher | LogSeverity.Error;
        public const LogSeverity WarningOrHigher = ErrorOrHigher | LogSeverity.Warning;
        public const LogSeverity InfoOrHigher = WarningOrHigher | LogSeverity.Info;
        public const LogSeverity DebugOrHigher = InfoOrHigher | LogSeverity.Debug;
        public const LogSeverity TraceOrHigher = DebugOrHigher | LogSeverity.Trace;
        public const LogSeverity Any = LogSeverity.CombinedAll;
    }
}
