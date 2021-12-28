using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EasyLog.WriterImplementations
{
    /// <summary>
    /// Provides an interface to log messages to a simple text file
    /// </summary>
    public class FileWriter : LogWriter
    {
        /// <summary>
        /// File name mask
        /// </summary>
        /// <remarks>See documentation and the constructor header comment for information</remarks>
        public string FileNameMask { get; }
        /// <summary>
        /// Gets or sets whether I/O failures should raise an exception.
        /// </summary>
        /// <remarks>
        /// If set to false, no exception is raised,
        /// and any failed log message is silently discarded.
        /// Because of this, it defaults to true.
        /// It's not recommended that you set this to false.
        /// </remarks>
        public bool RaiseExceptionOnLogFailure { get; set; } = true;

        private StreamWriter FS;
        private string CurrentFileName;
        private readonly Encoding textEncoding;
        private readonly string translatedMask;

        /// <summary>
        /// Instantiates a file based logger
        /// </summary>
        /// <param name="LogMask">Event mask to be logged</param>
        /// <param name="FileNameMask">
        /// File name mask. For a simple use case, this is just a regular file name and path.
        /// Directories are created as needed.
        /// DateTime format strings can be inserted via %{...}
        /// Severity can be inserted as #{sev} for the string or #{sevint} for the enum number
        /// Example for a daily log file with a monthly folder structure:
        /// C:\Path\To\Programm\Logs\%{yyyy}\%{MM}\%{dd}.txt
        /// Whether the supplied date value is UTC or local depends on the configuration of
        /// <see cref="EasyLogger.UseUTC"/>
        /// </param>
        /// <param name="TextEncoding">Encoding for the file</param>
        public FileWriter(LogSeverity LogMask, string FileNameMask, Encoding TextEncoding)
        {
            if (string.IsNullOrWhiteSpace(FileNameMask))
            {
                throw new ArgumentException($"'{nameof(FileNameMask)}' cannot be null or whitespace.", nameof(FileNameMask));
            }

            textEncoding = TextEncoding ?? throw new ArgumentNullException(nameof(TextEncoding));
            this.LogMask = LogMask;
            this.FileNameMask = Path.GetFullPath(FileNameMask);
            translatedMask = this.FileNameMask
                .Replace("%{", "{0:")
                .Replace("#{sev}", "{2}")
                .Replace("#{sevint}", "{1}");
            ThreadSafetyRequiresLock = true;
            //The lock is later bound to the open file stream but we currently do not have one
            Lock = this;

            try
            {
                var TempName = ConstructFileName(DateTime.UtcNow, LogSeverity.Info);
                var Segments = TempName.Split('/');
                for(var i = 1; i < Segments.Length - 2; i++)
                {
                    if (Segments[i].IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        throw new ArgumentException($"File name mask test with example date failed. Invalid path segment: {Segments[i]}");
                    }
                }
                if (Segments[Segments.Length - 1].IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    throw new ArgumentException($"File name mask test with example date failed. Invalid path segment: {Segments[Segments.Length - 1]}");
                }
            }
            catch (Exception ex)
            {
                ex.Data["FileNameMask"] = FileNameMask;
                throw new ArgumentException("The supplied file name mask results in an invalid file name when parsed with the current UTC date. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Instantiates a file based logger using UTF-8
        /// </summary>
        /// <param name="LogMask">Event mask to be logged</param>
        /// <param name="FileNameMask">
        /// File name mask. For a simple use case, this is just a regular file name and path.
        /// Directories are created as needed. Formatting can be done using <see cref="string.Format"/> style syntax.
        /// Possible arguments:
        /// 0: DateTime
        /// 1: Severity (as number)
        /// 2: Severity (as string)
        /// Example for a daily log file: C:\Path\To\Programm\Logs\{0:yyyy}\{0:MM}\{0:dd}.txt
        /// Whether the supplied date value is UTC or local depends on the configuration of
        /// <see cref="EasyLogger.UseUTC"/>
        /// </param>
        public FileWriter(LogSeverity LogMask, string FileNameMask) :
            this(LogMask, FileNameMask, new UTF8Encoding(false))
        {
            //Default to UTF-8
        }

        /// <summary>
        /// Does nothing for this type of logger
        /// </summary>
        /// <returns>true</returns>
        public override bool Open()
        {
            return true;
        }

        /// <summary>
        /// Closes any open file stream
        /// </summary>
        /// <returns>true</returns>
        public override bool Close()
        {
            if (FS != null)
            {
                FS.Close();
                FS.Dispose();
                FS = null;
                Lock = this;
            }
            return true;
        }

        /// <summary>
        /// Simply calls <see cref="Close"/>
        /// </summary>
        public override void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Flushes any pending data to the file
        /// </summary>
        public override void Flush()
        {
            if (FS != null)
            {
                FS.Flush();
            }
        }

        public override void WriteEntry(string Component, DateTime LogTime, double Runtime, LogSeverity Severity, string Line, Exception Ex = null)
        {
            var Writer = GetStream(LogTime, Severity);
            try
            {
                Writer.WriteLine(Line);
            }
            catch
            {
                if (RaiseExceptionOnLogFailure)
                {
                    throw;
                }
            }
            if (Ex != null)
            {
                try
                {
                    Writer.WriteLine(EasyLogger.FormatException(Ex));
                }
                catch
                {
                    if (RaiseExceptionOnLogFailure)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the file stream that's applicable for the given date and severity.
        /// Creates directories and files as necessary.
        /// </summary>
        /// <param name="LogTime">Date for the log message</param>
        /// <param name="Severity">Log severity</param>
        /// <returns>File writer</returns>
        private StreamWriter GetStream(DateTime LogTime, LogSeverity Severity)
        {
            //Get the matching file name for the given log parameters
            string ExpectedName = ConstructFileName(LogTime, Severity);
            FileStream TempStream = null;

            //If CurrentFileName differs from ExpectedName we need to open a new stream
            if (ExpectedName != CurrentFileName)
            {
                //Create directories if the new log name differs from the old one.
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ExpectedName));
                    TempStream = File.Open(ExpectedName, FileMode.Append, FileAccess.Write, FileShare.Read);
                }
                catch (Exception ex)
                {
                    if (RaiseExceptionOnLogFailure)
                    {
                        Environment.FailFast($"{nameof(FileWriter)} cannot create log file at {ExpectedName}", ex);
                        throw;
                    }
                    else
                    {
                        //Continue writing to the old stream if possible.
                        //If no stream is open, use a null stream
                        return FS ?? StreamWriter.Null;
                    }
                }
                //Properly close an already open stream
                if (FS != null)
                {
                    FS.Close();
                    FS.Dispose();
                }
                FS = new StreamWriter(TempStream, textEncoding);
                Lock = FS;
                CurrentFileName = ExpectedName;
            }

            return FS;
        }

        /// <summary>
        /// Constructs a file name according to the file name mask
        /// </summary>
        /// <param name="LogTime">Date for the log message</param>
        /// <param name="Severity">Log severity</param>
        /// <returns>File name</returns>
        private string ConstructFileName(DateTime DT, LogSeverity Severity)
        {
            return string.Format(translatedMask, DT, (int)Severity, Severity);
        }
    }
}
