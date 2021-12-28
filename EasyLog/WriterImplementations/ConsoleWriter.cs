using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EasyLog.WriterImplementations
{
    public class ConsoleWriter : LogWriter
    {
        public bool UsingStdError { get; }
        public bool UseColor { get; set; }
        public bool ResetColorAfterWrite { get; }

        private readonly TextWriter ConsoleStream;

        /// <summary>
        /// Creates a new instance of this logger
        /// </summary>
        /// <param name="LogMask">Log level mask</param>
        /// <param name="UseStdError">Use <see cref="Console.Error"/> instead of <see cref="Console.Out"/></param>
        /// <param name="UseColor">
        /// Colorize output.
        /// Colorization can eat a significant portion of logging if you generate excessive messages.
        /// You may want to disable it especially if you include the "Debug" or "Trace" levels in your
        /// <paramref name="LogMask"/>
        /// </param>
        /// <param name="ResetColorAfterWrite">
        /// Reset colors back to previous values after output.
        /// Has no effect if <paramref name="UseColor"/> is set to false.
        /// </param>
        public ConsoleWriter(LogSeverity LogMask, bool UseStdError, bool UseColor, bool ResetColorAfterWrite)
        {
            this.LogMask = LogMask;
            UsingStdError = UseStdError;
            this.UseColor = UseColor;
            this.ResetColorAfterWrite = UseColor && ResetColorAfterWrite;
            Lock = ConsoleStream = UseStdError ? Console.Error : Console.Out;
            ThreadSafetyRequiresLock = true;
        }

        /// <summary>
        /// Open has no effect on the console
        /// </summary>
        /// <returns>true</returns>
        public override bool Open() => true;
        /// <summary>
        /// Close has no effect on the console
        /// </summary>
        /// <returns></returns>
        public override bool Close() => true;
        
        /// <summary>
        /// Has no effect on this instance. It doesn't requires disposing
        /// </summary>
        public override void Dispose()
        {
            //Note: Do not dispose "ConsoleStream".
            //We did not instantiate it, so we should also not dispose it.
        }

        /// <summary>
        /// Flush has no effect on the console
        /// </summary>
        public override void Flush()
        {
        }

        public override void WriteEntry(string Component, DateTime LogTime, double Runtime, LogSeverity Severity, string Line, Exception Ex = null)
        {
            ConsoleColor C = ConsoleColor.Black;
            if (UseColor)
            {
                if (ResetColorAfterWrite)
                {
                    C = Console.ForegroundColor;
                }
                switch (Severity)
                {
                    case LogSeverity.Critical:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LogSeverity.Trace:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }
            }
            ConsoleStream.WriteLine(Line);
            if (Ex != null)
            {
                ConsoleStream.WriteLine(EasyLogger.FormatException(Ex));
            }
            if (ResetColorAfterWrite && UseColor)
            {
                Console.ForegroundColor = C;
            }
        }
    }
}
