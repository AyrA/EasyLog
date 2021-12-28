using System;
using System.Threading;

namespace EasyLog.WriterImplementations
{
    /// <summary>
    /// Provides a log writer with no backing storage.
    /// It discards all messages.
    /// </summary>
    /// <remarks>
    /// Allows for fake load testing in multi threaded environments
    /// by using the <see cref="Delay"/> value
    /// </remarks>
    public class NullWriter : LogWriter
    {
        private TimeSpan delay;

        /// <summary>
        /// Gets or sets the fake delay added on every log message.
        /// Disabled if zero.
        /// </summary>
        public TimeSpan Delay
        {
            get => delay;
            set
            {
                delay = value.Duration();
            }
        }

        public NullWriter(LogSeverity LogMask = LogSeverity.CombinedDefaults, bool PretendToNeedThreadSafety = false)
        {
            this.LogMask = LogMask;
            ThreadSafetyRequiresLock = PretendToNeedThreadSafety;
            Lock = this;
            Delay = new TimeSpan(0L);
        }

        /// <summary>
        /// Discards the given message
        /// and waits for <see cref="Delay"/> to pass
        /// </summary>
        /// <param name="Component">Ignored</param>
        /// <param name="LogTime">Ignored</param>
        /// <param name="Runtime">Ignored</param>
        /// <param name="Severity">Ignored</param>
        /// <param name="Line">Ignored</param>
        /// <param name="Ex">Ignored</param>
        public override void WriteEntry(string Component, DateTime LogTime, double Runtime, LogSeverity Severity, string Line, Exception Ex = null)
        {
            if (Delay.Ticks > 0)
            {
                Thread.Sleep(Delay);
            }
        }
    }
}
