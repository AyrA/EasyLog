using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog
{
    /// <summary>
    /// Abstraction for a log writer
    /// </summary>
    public abstract class LogWriter : IDisposable
    {
        /// <summary>
        /// Gets or sets a bitmask of all events logged in this instance.
        /// See <see cref="LogSeverityMask"/> for a list of predefined constants.
        /// </summary>
        /// <remarks>
        /// The instance doesn't needs to filter for these.
        /// <see cref="EasyLogger"/> will already do this.
        /// </remarks>
        public LogSeverity LogMask { get; set; } = LogSeverity.CombinedDefaults;

        /// <summary>
        /// If set to true, it means calls to WriteEntry must be inside of a <see cref="lock"/> statement
        /// to be thread safe. The object that is locked in this case is <see cref="Lock"/>
        /// </summary>
        public bool ThreadSafetyRequiresLock { get; protected set; }

        /// <summary>
        /// Object that can be used to lock this instance for multi thread safety.
        /// This may be null if <see cref="ThreadSafetyRequiresLock"/> is false.
        /// </summary>
        /// <remarks>
        /// In the simplest case, it can be initialized with "Lock = new object()".
        /// For a file or network logger you may want to assign it the file/network stream instance.
        /// You can share the same object between multiple instances of your writer
        /// if they log to the same base resource to get thread safety across instances.
        /// This value is accessed by EasyLogger exclusively for locking purposes.
        /// </remarks>
        /// <example>Lock = this;</example>
        public object Lock { get; protected set; }

        /// <summary>
        /// When overridden in a derived class,
        /// opens the logger and gets it ready to log messages
        /// </summary>
        /// <remarks>
        /// This should also return true if opening is not necessary,
        /// or if <see cref="Open"/> has already been called.
        /// <see cref="Open"/> should also work as "re-open"
        /// after <see cref="Close"/> has been called.
        /// </remarks>
        /// <returns>true, if sucessfully opened</returns>
        public virtual bool Open() => true;

        /// <summary>
        /// When overridden in a derived class, closes the logger
        /// </summary>
        /// <remarks>
        /// This should also return true if the log is already closed,
        /// or if this implementation doesn't needs closing
        /// </remarks>
        /// <returns>true, if closed successfully</returns>
        public virtual bool Close() => true;

        /// <summary>
        /// Writes the given entry to the log
        /// </summary>
        /// <param name="Component">
        /// the component that generated the message.
        /// Note: <paramref name="Line"/> is already formatted to include this.
        /// </param>
        /// <param name="LogTime">
        /// Time of log.
        /// Note: <paramref name="Line"/> is already formatted to include this.
        /// </param>
        /// <param name="Runtime">
        /// How long the logger has been running.
        /// Note: <paramref name="Line"/> is already formatted to include this.
        /// </param>
        /// <param name="Severity">
        /// Log message severity.
        /// Note: <paramref name="Line"/> is already formatted to include this.
        /// </param>
        /// <param name="Line">Fully formatted log line</param>
        /// <param name="Ex">
        /// Optional exception that is associated with this log line
        /// </param>
        public abstract void WriteEntry(string Component, DateTime LogTime, double Runtime, LogSeverity Severity, string Line, Exception Ex = null);

        /// <summary>
        /// When overridden in a derived class,
        /// causes all pending data to be written immediately
        /// if flushing is appropriate for the chosen logging method.
        /// Otherwise it does nothing
        /// </summary>
        /// <remarks>
        /// This method should only return after data has been completely flushed.
        /// This method may do nothing if flushing data is not appropriate or the backend automatically flushes.
        /// This method is not guaranteed to be called during an application crash.
        /// </remarks>
        public virtual void Flush() { }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
