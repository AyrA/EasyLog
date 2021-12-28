using EasyLog.WriterImplementations;
using System;
using System.IO;

namespace EasyLog
{
    /// <summary>
    /// Provides pre-made logger implementations for quick and easy usage
    /// </summary>
    public static class DefaultLogger
    {
        /// <summary>
        /// Creates a logger that writes messages to the console.
        /// This logger will automatically lock up on Critical messages
        /// </summary>
        /// <param name="Filter">Log message filter</param>
        /// <param name="UseStdErr">Use <see cref="Console.Error"/> instead of <see cref="Console.Out"/></param>
        /// <param name="UseColor">Colorize messages</param>
        /// <returns>Console logger</returns>
        public static EasyLogger CreateConsoleLogger(LogSeverity Filter = LogSeverity.CombinedDefaults, bool UseStdErr = true, bool UseColor = true)
        {
            var CW = new ConsoleWriter(LogSeverity.CombinedAll, UseStdErr, UseColor, UseColor);
            var Log = new EasyLogger(CW, Filter)
            {
                //Automatically lock the console logger if no debugger is attached
                AutoLockOnCritical = !System.Diagnostics.Debugger.IsAttached,
                BeThreadSafe = true
            };
            return Log;
        }

        /// <summary>
        /// Creates a logger that writes messages to file.
        /// This logger uses an additional "crash.log" file in the log base directory for critical messages
        /// </summary>
        /// <param name="Filter">Log message filter</param>
        /// <param name="BaseLogDirectory">
        /// Base directory for log files.
        /// Relative paths resolve with <see cref="Environment.CurrentDirectory"/>.
        /// If you want to guarantee your logs use the executable directory,
        /// supply a full file path instead.
        /// </param>
        /// <param name="RotateDaily">Automatically start new log file after midnight</param>
        /// <param name="PerMonthDirectory">Create a new directory for every month</param>
        /// <returns>File logger</returns>
        public static EasyLogger CreateFileLogger(LogSeverity Filter = LogSeverity.CombinedDefaults, string BaseLogDirectory = "Logs", bool RotateDaily = false, bool PerMonthDirectory = false)
        {
            var Dir = Path.GetFullPath(BaseLogDirectory);
            if (RotateDaily)
            {
                if (PerMonthDirectory)
                {
                    Dir = Path.Combine(Dir, "%{yyyy}", "%{MM}", "%{dd}.log");
                }
                else
                {
                    Dir = Path.Combine(Dir, "%{yyyy}-%{MM}-%{dd}.log");
                }
            }
            else
            {
                Dir = Path.Combine(Dir, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
            }
            var Logger = new LogWriter[]
            {
                new FileWriter(LogSeverity.CombinedAll, Dir),
                new FileWriter(LogSeverity.Critical, Path.Combine(BaseLogDirectory, "crash.log"))
            };
            var Log = new EasyLogger(Logger, Filter)
            {
                BeThreadSafe = true
            };
            return Log;
        }

        /// <summary>
        /// Creates a logger with no backing store
        /// </summary>
        /// <param name="Filter">Log message filter</param>
        /// <returns>Null logger</returns>
        public static EasyLogger CreateNullLogger(LogSeverity Filter = LogSeverity.CombinedDefaults)
        {
            return new EasyLogger(new NullWriter(Filter), Filter)
            {
                BeThreadSafe = false
            };
        }
    }
}
