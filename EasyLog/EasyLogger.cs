using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace EasyLog
{
    /// <summary>
    /// Provides an easy to use logging interface that can support virtually any backend
    /// </summary>
    public class EasyLogger : IDisposable
    {
        /// <summary>
        /// The default date and time format.
        /// The default is ISO 8601 date and time without the useless "T"
        /// </summary>
        /// <remarks>
        /// This does not include timezone information.
        /// If you need to know real time, either set <see cref="UseUTC"/> or add the UTC offset specifier "K".
        /// </remarks>
        public const string DEFAULT_DATE_FORMAT = "yyyy-MM-dd HH':'mm':'ss";
        /// <summary>
        /// The default format for a log line. Supported placeholders:
        /// 0: Date and time formatted using <see cref="DateFormat"/>
        /// 1: Number of seconds since this logging instance has been started (including fractions)
        /// 2: The component that generated the message
        /// 3: The log message severity
        /// 4: The log message
        /// </summary>
        /// <remarks>
        /// Placeholders can be left out if they're not needed.
        /// </remarks>
        public const string DEFAULT_LINE_FORMAT = "{0}\t{1:0.00}\t{2}\t[{3}]\t{4}";

        /// <summary>
        /// This value can be optionally set to grant every component you use
        /// easy access to an EasyLogger that you instantiated and configured.
        /// If this is left as null (or manually set to null),
        /// the first EasyLogger you instantiate will assign itself to this value.
        /// </summary>
        public static EasyLogger DefaultLogger = null;

        /// <summary>
        /// Allows to find out if a message is worse than a certain level very quickly.
        /// </summary>
        private static readonly Dictionary<LogSeverity, LogSeverity> logMaskMatch = new Dictionary<LogSeverity, LogSeverity>
        {
            { LogSeverity.Critical, LogSeverityMask.CriticalOrHigher },
            { LogSeverity.Error   , LogSeverityMask.ErrorOrHigher    },
            { LogSeverity.Warning , LogSeverityMask.WarningOrHigher  },
            { LogSeverity.Info    , LogSeverityMask.InfoOrHigher     },
            { LogSeverity.Debug   , LogSeverityMask.DebugOrHigher    },
            { LogSeverity.Trace   , LogSeverityMask.TraceOrHigher    }
        };

        /// <summary>
        /// Counts time since this instance has been started
        /// </summary>
        private readonly Stopwatch logStart;
        /// <summary>
        /// Loaded log backends
        /// </summary>
        private readonly List<LogWriter> backends;
        /// <summary>
        /// Log message filter
        /// </summary>
        private LogSeverity severityMask;
        /// <summary>
        /// true, to indicate that <see cref="Dispose"/> has been called
        /// </summary>
        private bool disposed = false;
        /// <summary>
        /// 0: Log normally
        /// 1: Log this message but no message after this
        /// 2: Do not log anything (also applies if value is bigger)
        /// </summary>
        private int logTrip = 0;
        /// <summary>
        /// List of AppDomains we currently have a global error logger attached
        /// </summary>
        private readonly List<AppDomain> hookedDomains;

        /// <summary>
        /// Automatically locks up the application
        /// when a message of type <see cref="LogSeverity.Critical"/> is logged.
        /// If set to true, it automatically calls <see cref="LockUp"/> after the message has been logged.
        /// </summary>
        public bool AutoLockOnCritical { get; set; } = false;

        /// <summary>
        /// The final message displayed at the end of <see cref="LockUp"/>.
        /// You may tell the user here to send you a log file.
        /// </summary>
        public string LockUpMessage { get; set; }

        /// <summary>
        /// Format of a log line.
        /// The default is <see cref="DEFAULT_LINE_FORMAT"/>
        /// </summary>
        public string LineFormat { get; set; } = DEFAULT_LINE_FORMAT;

        /// <summary>
        /// Gets or sets the date format for log messages.
        /// This can be a predefined or custom DateTime format specifier.
        /// The default is <see cref="DEFAULT_DATE_FORMAT"/>
        /// </summary>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings"/>
        public string DateFormat { get; set; } = DEFAULT_DATE_FORMAT;

        /// <summary>
        /// Gets or sets whether UTC should be used for log messages
        /// </summary>
        public bool UseUTC { get; set; } = false;

        /// <summary>
        /// Gets or sets whether thread safety should be ensured.
        /// Has no effect if <see cref="LogWriter.ThreadSafetyRequiresLock"/> of the supplied backend
        /// is not also set to true.
        /// Thread safety is provided by using the "lock" statement.
        /// </summary>
        public bool BeThreadSafe { get; set; } = true;
        /// <summary>
        /// Gets or sets the minimum message severity required for a message to be logged
        /// </summary>
        public LogSeverity SeverityMask
        {
            get => severityMask;
            set
            {
                //Stripping unknown values
                severityMask = value & LogSeverity.CombinedAll;
            }
        }

        /// <summary>
        /// Gets if the safety mechanism has been tripped and logging stopped
        /// </summary>
        public bool IsTripped { get => logTrip > 1; }

        /// <summary>
        /// Creates a new instance with the given backends and log severity mask.
        /// Multiple backends can be supplied, but they all must be operable at time of calling this constructor.
        /// </summary>
        /// <param name="Backend">Log backend. At least one must be supplied</param>
        /// <param name="SeverityMask">
        /// Messages that will be logged.
        /// Usually one of the <see cref="LogSeverityMask"/> constants
        /// </param>
        public EasyLogger(IEnumerable<LogWriter> Backend, LogSeverity SeverityMask = LogSeverityMask.InfoOrHigher)
        {
            logStart = Stopwatch.StartNew();
            if (Backend == null)
            {
                throw new ArgumentNullException(nameof(Backend));
            }
            backends = Backend.ToList();
            if (backends.Count == 0)
            {
                throw new ArgumentException($"{nameof(Backend)} argument is empty");
            }
            if (backends.Contains(null))
            {
                throw new ArgumentNullException(nameof(Backend), "At least one entry is null");
            }
            if (backends.Select(m => m.Open()).ToArray().Contains(false))
            {
                throw new ArgumentNullException(nameof(Backend), "At least one backend refused to Open()");
            }
            if (backends.Distinct().Count() < backends.Count)
            {
                throw new ArgumentException($"{nameof(Backend)} argument contains duplicates");
            }
            hookedDomains = new List<AppDomain>();
            this.SeverityMask = SeverityMask;
        }

        /// <summary>
        /// Creates a new instance with the given backend and log severity mask.
        /// </summary>
        /// <param name="Backend">Log backend</param>
        /// <param name="SeverityMask">
        /// Messages that will be logged.
        /// Usually one of the <see cref="LogSeverityMask"/> constants
        /// </param>
        public EasyLogger(LogWriter Backend, LogSeverity SeverityMask = LogSeverityMask.InfoOrHigher) :
            this(new LogWriter[] { Backend }, SeverityMask)
        {

        }

        /// <summary>
        /// Resets the log trip into the untripped state.
        /// You don't want to do this unless you are absolutely sure,
        /// because if <see cref="IsTripped"/> is set,
        /// it usually means your application is already in a crashing state.
        /// </summary>
        /// <remarks>
        /// If <see cref="IsTripped"/> is set,
        /// it usually means that the global exception logger (attached via <see cref="AttachGlobalErrorLogger"/>)
        /// has been called
        /// </remarks>
        public void Untrip()
        {
            CheckDisposed();
            logTrip = 0;
        }

        /// <summary>
        /// Attaches this instance to the global error handler to report errors arriving there.
        /// </summary>
        /// <remarks>It is not dangerous to call this when the logger is already attached</remarks>
        public void AttachGlobalErrorLogger(AppDomain Domain)
        {
            CheckDisposed();
            lock (this)
            {
                DetachGlobalErrorLogger(Domain);
                hookedDomains.Add(Domain);
                Domain.UnhandledException += CurrentDomain_UnhandledException;
            }
        }

        /// <summary>
        /// Detaches this instance from the global error handler
        /// </summary>
        /// <remarks>
        /// It is not dangerous to call this when the logger is already detached.
        /// This is called automatically when this instance is disposed
        /// </remarks>
        public void DetachGlobalErrorLogger(AppDomain Domain)
        {
            CheckDisposed();
            lock (this)
            {
                if (hookedDomains.Remove(Domain))
                {
                    Domain.UnhandledException -= CurrentDomain_UnhandledException;
                }
            }
        }

        /// <summary>
        /// Gets a logger that uses the given component name in all messages
        /// </summary>
        /// <param name="ComponentName">Component name</param>
        /// <returns>Component specific logger</returns>
        public ComponentLogger GetLogger(string ComponentName)
        {
            CheckDisposed();
            return new ComponentLogger(ComponentName, this);
        }

        #region Logging functions

        /// <summary>
        /// Logs the given message
        /// </summary>
        /// <param name="Component">Component that generated the message</param>
        /// <param name="Severity">Log message severity</param>
        /// <param name="Message">Log message</param>
        /// <param name="Ex">Optional exception</param>
        /// <returns>Formatted message if logged. Returns null if not logged</returns>
        public string Log(string Component, LogSeverity Severity, string Message, Exception Ex = null)
        {
            //This avoids recursively logging critical errors if the AppDomain crashes.
            if (logTrip == 1)
            {
                ++logTrip;
            }
            else if (logTrip > 1)
            {
                return null;
            }
            if (!Enum.IsDefined(typeof(LogSeverity), Severity))
            {
                throw new ArgumentException("Unsupported value for log severity", nameof(Severity));
            }
            if (!severityMask.HasFlag(Severity))
            {
                return null;
            }
            var DT = UseUTC ? DateTime.UtcNow : DateTime.Now;
            var Time = logStart.Elapsed.TotalSeconds;
            var Formatted = string.Format(LineFormat, DT.ToString(DateFormat), Time, Severity, Component, Message);
            foreach (var BE in backends)
            {
                if (BE.LogMask.HasFlag(Severity))
                {
                    if (BeThreadSafe && BE.ThreadSafetyRequiresLock)
                    {
                        //Component requires thread safety and we've turned it on
                        lock (BE.Lock)
                        {
                            BE.WriteEntry(Component, DT, Time, Severity, Formatted, Ex);
                        }
                    }
                    else
                    {
                        //Component doesn't requires thread safety or we've turned it off
                        BE.WriteEntry(Component, DT, Time, Severity, Formatted, Ex);
                    }
                }
            }
            if (AutoLockOnCritical && Severity == LogSeverity.Critical)
            {
                LockUp(Message, Ex);
            }
            return Formatted;
        }

        public string LogCritical(string Component, string Message, Exception Ex)
        {
            return Log(Component, LogSeverity.Critical, Message, Ex);
        }

        public string LogError(string Component, string Message, Exception Ex = null)
        {
            return Log(Component, LogSeverity.Error, Message, Ex);
        }

        public string LogWarning(string Component, string Message, Exception Ex = null)
        {
            return Log(Component, LogSeverity.Warning, Message, Ex);
        }

        public string LogInfo(string Component, string Message, Exception Ex = null)
        {
            return Log(Component, LogSeverity.Info, Message, Ex);
        }

        public string LogDebug(string Component, string Message, Exception Ex = null)
        {
            return Log(Component, LogSeverity.Debug, Message, Ex);
        }

        public string LogTrace(string Component, string Message, Exception Ex = null)
        {
            return Log(Component, LogSeverity.Trace, Message, Ex);
        }

        #endregion

        /// <summary>
        /// Locks up the application and displays the given message and exception.
        /// Useful for calling after logging critical messages.
        /// <see cref="AutoLockOnCritical"/> will make critical logs call this automatically
        /// </summary>
        /// <param name="Message">Log message</param>
        /// <param name="Ex">Exception</param>
        /// <remarks>
        /// This will not actually lock up if a debugger is attached.
        /// This allows a debugger to catch the exception.
        /// </remarks>
        public void LockUp(string Message, Exception Ex)
        {
            CheckDisposed();
            foreach (var BE in backends)
            {
                BE.Flush();
            }
            Console.Error.WriteLine();
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Error.Write("UNCAUGHT ERROR".PadRight(Console.BufferWidth));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Error.WriteLine("Reason: {0}", Message);
            if (Ex != null)
            {
                Console.Error.WriteLine(FormatException(Ex));
            }
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Error.Write(string.Empty.PadRight(Console.BufferWidth, '#'));
            Console.ResetColor();
            if (!string.IsNullOrEmpty(LockUpMessage))
            {
                Console.Error.WriteLine(LockUpMessage);
            }
            if (Debugger.IsAttached)
            {
                Console.Error.WriteLine("DEBUGGER IS ATTACHED. WILL NOT HALT!");
            }
            else
            {
                Console.Error.WriteLine("Application halted");
                Thread.CurrentThread.Join();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var LogType = e.IsTerminating ? LogSeverity.Critical : LogSeverity.Error;
            var Component = !(sender is AppDomain Domain) ? "<GLOBAL>" : Domain.FriendlyName;
            if (e.IsTerminating)
            {
                ++logTrip;
            }
            Log(Component, LogType, "Unhandled Exception in AppDomain", e.ExceptionObject as Exception);
        }

        private void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EasyLogger));
            }
        }

        public void Dispose()
        {
            foreach (var BE in backends)
            {
                BE.Dispose();
            }
            backends.Clear();
            foreach (var domains in hookedDomains.ToArray())
            {
                DetachGlobalErrorLogger(domains);
            }
            disposed = true;
        }

        /// <summary>
        /// Formats an exception and all inner exceptions into lines for logging.
        /// Logged information is type, message, stack, and the data dictionary
        /// </summary>
        /// <param name="Ex">Exceptions</param>
        /// <returns>
        /// Exception lines,
        /// or <see cref="string.Empty"/> if <paramref name="Ex"/> is null
        /// </returns>
        public static string FormatException(Exception Ex)
        {
            const int PAD = 10;
            if (Ex == null)
            {
                return string.Empty;
            }
            using (var SW = new System.IO.StringWriter())
            {
                while (Ex != null)
                {

                    SW.WriteLine(string.Empty.PadRight(PAD, '='));
                    SW.WriteLine("[{0}]: {1}", Ex.GetType().Name, Ex.Message);
                    SW.WriteLine("Code:     0x{0:X8}", Ex.HResult);
                    SW.WriteLine("Location: {0}", Ex.Source);
                    SW.WriteLine("Stack: {0}", Ex.StackTrace);
                    if (Ex.Data != null && Ex.Data.Count > 0)
                    {
                        SW.WriteLine("Data: {0} entries", Ex.Data.Count);
                        foreach (var K in Ex.Data.Keys)
                        {
                            SW.WriteLine("{0}: {1}", K, Ex.Data[K]);
                        }
                    }
                    Ex = Ex.InnerException;
                }
                SW.Write(string.Empty.PadRight(PAD, '='));
                SW.Flush();
                return SW.ToString();
            }
        }
    }
}
