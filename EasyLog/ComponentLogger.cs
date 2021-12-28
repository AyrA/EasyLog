using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog
{
    /// <summary>
    /// Provides a simpler interface to the <see cref="EasyLogger"/> log functions
    /// </summary>
    public class ComponentLogger
    {
        private readonly EasyLogger logger;

        /// <summary>
        /// Gets the component name used for log messages
        /// </summary>
        public string Component { get; }

        /// <summary>
        /// Creates a component logger instance that logs messages using the supplied component.
        /// </summary>
        /// <param name="Component"></param>
        /// <param name="Logger"></param>
        /// <remarks>
        /// It's usually easier to use <see cref="EasyLogger.GetLogger(string)"/>
        /// </remarks>
        public ComponentLogger(string Component, EasyLogger Logger)
        {
            if (string.IsNullOrEmpty(Component))
            {
                throw new ArgumentException($"'{nameof(Component)}' cannot be null or empty.", nameof(Component));
            }

            if (Logger is null)
            {
                throw new ArgumentNullException(nameof(Logger));
            }

            this.Component = Component;
            logger = Logger;
        }

        #region Logging functions

        public string Log(LogSeverity Severity, string Message, Exception Ex = null)
        {
            return logger.Log(Component,Severity,Message,Ex);
        }

        public string LogCritical(string Message, Exception Ex)
        {
            return logger.Log(Component, LogSeverity.Critical, Message, Ex);
        }

        public string LogError(string Message, Exception Ex = null)
        {
            return logger.Log(Component, LogSeverity.Error, Message, Ex);
        }

        public string LogWarning(string Message, Exception Ex = null)
        {
            return logger.Log(Component, LogSeverity.Warning, Message, Ex);
        }

        public string LogInfo(string Message, Exception Ex = null)
        {
            return logger.Log(Component, LogSeverity.Info, Message, Ex);
        }

        public string LogDebug(string Message, Exception Ex = null)
        {
            return logger.Log(Component, LogSeverity.Debug, Message, Ex);
        }

        public string LogTrace(string Message, Exception Ex = null)
        {
            return logger.Log(Component, LogSeverity.Trace, Message, Ex);
        }

        #endregion
    }
}
