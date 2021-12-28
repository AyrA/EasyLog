# EasyLog

EasyLog is a simple to use yet very versatile logging system.
It supports about any backend possible.

It comes with a console and file logger built into it.

The console logger can output to stdout as well as stderr, and optionally can colorize the output.
The file logger can output to a text file and supports log rotation based on log message type and date.

Additionally there is a "Null" logger that discards all output,
but allows to delay for arbitrary amounts of time to simulate a slow logging backend.

## Installation

1. Download [EasyLog.dll](https://gitload.net/AyrA/EasyLog)
2. Add a reference to it in your project settings
3. Done

## Usage

Simple usage with a pre-made logger below

```C#

using (var CL = EasyLog.DefaultLogger.CreateConsoleLogger())
{
	//Attach to the global error handler to log exceptions you did not catch
	CL.AttachGlobalErrorLogger(AppDomain.CurrentDomain);
	//Create a component logger for easier usage
	var Logger = CL.GetLogger("My first logger");
	//Log your first message
	Logger.LogInfo("An info message");
}

```

### Component logger

Component loggers are optional to use. The EasyLog class has methods to log all types of messages already.
The component logger makes it a bit more comfortable by not requiring that you supply the component name for every message.

You can use any number of component loggers and the EasyLog class itself simultaneously to log messages.

### Thread safety

For bigger projects you may experience multiple threads accessing the logging components simultaneously.
EasyLog is thread safe by default by using `lock(x){}` statements if the log backend requires locking for thread safety. This comes at a performance penalty.
If you don't need thread safety, or if it's guaranteed by other means,
you can set `LogWriter.BeThreadSafe = false;` to disable locking.

## Custom logger

You can create custom loggers fairly easily. All you need to do is derive from the `LogWriter` class.
Only the `WriteEntry` method needs to be implemented for simple interfaces.
If your implementation requires more complex operations to instantiate, you can override various methods to achieve this.

At the bare minimum you should:

- Create a constructor that sets the `LogMask` property
- Set `ThreadSafetyRequiresLock = true;` if you need a lock for thread safety
- If you need a lock, set `Lock = this;`

Check out the included file logger for a more complex scenario.
The file logger uses the file stream as a locking object. This means that if the stream changes, new log messages can be written to the new file without stalling due to ongoing locks of the old file.