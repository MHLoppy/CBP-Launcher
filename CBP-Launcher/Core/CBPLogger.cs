using NLog;
using System;

namespace CBPLauncher.Core
{
    public class CBPLogger : ILogger
    {
        //private static Lazy<CBPLogger> instance = new Lazy<CBPLogger>(() => new CBPLogger());
        private static CBPLogger instance;
        private static Logger logger;
        private static readonly object InstanceLock = new object();
        
        private CBPLogger()
        {
            //intentionally empty
        }

        /*public static CBPLogger GetInstance
        {
            get => instance.Value;
        }*/

        public static CBPLogger GetInstance()
        {
            if (instance == null)
            {
                lock (InstanceLock)
                {
                    if (instance == null)
                    {
                        instance = new CBPLogger();
                    }
                }
            }
            return instance;
        }

        // can't use lazy here because insufficient access to Logger rip
        private Logger GetLogger(string theLogger)
        {
            if (logger == null)
            {
                lock (InstanceLock)
                {
                    if (logger == null)
                    {
                        logger = LogManager.GetLogger(theLogger);
                    }
                }
            }
            return logger;
        }

        public void Debug(string message, string arg = null)
        {
            if (arg == null)
            {
                GetLogger("launcherLoggerRules").Debug(message);
            }
            else
            {
                GetLogger("launcherLoggerRules").Debug(message, arg);
            }
        }

        public void Error(string message, string arg = null)
        {
            if (arg == null)
            {
                GetLogger("launcherLoggerRules").Error(message);
            }
            else
            {
                GetLogger("launcherLoggerRules").Error(message, arg);
            }
        }

        public void Info(string message, string arg = null)
        {
            if (arg == null)
            {
                GetLogger("launcherLoggerRules").Info(message);
            }
            else
            {
                GetLogger("launcherLoggerRules").Info(message, arg);
            }
        }

        public void Warning(string message, string arg = null)
        {
            if (arg == null)
            {
                GetLogger("launcherLoggerRules").Warn(message);
            }
            else
            {
                GetLogger("launcherLoggerRules").Warn(message, arg);
            }
        }
    }
}
