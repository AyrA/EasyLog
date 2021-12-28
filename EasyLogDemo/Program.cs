using System;

namespace EasyLogDemo
{
    class Program
    {
        static void Main()
        {
            //Using the default console logger
            using (var CL = EasyLog.DefaultLogger.CreateConsoleLogger())
            {
                //Set or change a few defaults to match this demo:

                //This is a single threading application. No need for thread safety overhead.
                CL.BeThreadSafe = false;

                //Show that this is just a demo upon a crash
                CL.LockUpMessage = "Logger demo application. Press CTRL+C to terminate";

                //For this demo, log everything.
                //Normally you would use EasyLog.LogSeverity.CombinedDefaults
                CL.SeverityMask = EasyLog.LogSeverity.CombinedAll;
                CL.AttachGlobalErrorLogger(AppDomain.CurrentDomain);

                //Create a component logger.
                //Using these are optional but it means we don't have to supply
                //the component name manually for every log line.
                //The name can be freely chosen but should ideally represent the source
                var Logger = CL.GetLogger(nameof(Main));

                //Log every type of message except critical.
                //We could also use the generic "Logger.Log" instead.
                //This is especially usefull if the message type may depend upon dynamic conditions
                Logger.LogTrace("A trace message");
                Logger.LogDebug("A debug message");
                Logger.LogInfo("An info message");
                Logger.LogWarning("A warning message");
                Logger.LogError("An error message");

                //Disable default locker for the console logger
                //to show off critical message logging
                CL.AutoLockOnCritical = false;
                try
                {
                    //Produce a real exception to log it as critical message
                    Console.WriteLine(42 / int.Parse("0"));
                }
                catch (Exception ex)
                {
                    Logger.LogCritical("A critical message with a real exception", ex);
                }

                Logger.LogInfo("===========================================================");
                Logger.LogInfo("The next message is the result of a real uncaught exception");
                Logger.LogInfo("===========================================================");

                //Enable auto locker again
                CL.AutoLockOnCritical = true;

                //Produce a real exception but do not catch it
                //If you use "CL.AutoLockOnCritical = true", it will lock up now
                //unless we detect a debugger, because then we want the debugger to catch the exception.
                //This means the outcome of this line depends on whether you run this in visual studio or not.
                //If you are debugging, VS will likely jump in front of the console when this line is run.
                //You can just bring the console back to front from the task bar if you want to see the message,
                //or press CTRL+F5 to run without the debugger.
                Console.WriteLine(int.Parse("This exception will be logged by AttachGlobalErrorLogger"));
            }
            Console.Error.WriteLine("#END# Press [ESC] to exit");
            Console.ReadKey(true);
        }
    }
}
