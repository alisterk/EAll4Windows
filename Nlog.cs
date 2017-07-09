using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace EAll4Windows
{
    public static class Nlog
    {

        private static Logger log;

        static Nlog()
        {
            log = LogManager.GetCurrentClassLogger();

        }

        public static void Error(string error, params object[] args)
        {
            log.Error(error, args);
        }

        public static void Error(Exception ex)
        {
            log.Error(ex);
        }

        public static void Error(string message, Exception exception)
        {
            log.Error(exception, message);
        }

        public static void Info(string info, params object[] args)
        {
            log.Info(info, args);
        }

        public static void Trace(string info, params object[] args)
        {
            log.Trace(info, args);
        }

        public static void Info(string message, Exception exception)
        {
            log.Info(exception, message);
        }

        public static void Debug(string debug, params object[] args)
        {
            log.Debug(debug, args);
        }

        public static void Debug(string message, Exception exception)
        {
            log.Debug(exception, message);

        }


        public static void Warn(string warn, params object[] args)
        {
            log.Warn(warn, args);
        }

        public static void Warn(string message, Exception exception)
        {
            log.Warn(exception, message);
        }

        public static void Fatal(string message, params object[] args)
        {
            log.Fatal(message, args);
        }

        public static void Fatal(string message, Exception exception)
        {
            log.Fatal(exception, message);
        }
    }
}
