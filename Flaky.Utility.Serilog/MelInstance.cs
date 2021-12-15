using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flaky.Utility.Serilog
{
    public class MelSerilogInstance
    {
        public static ILoggerFactory FileLoggerFactory(
            string logFilePath = null
            , LogEventLevel level = LogEventLevel.Verbose
            , int retainedFileCountLimit = 10)
        {
            var fileLogger = SerilogLoggerInstance.ComplexFileLoggers(logFilePath: logFilePath, level: level, retainedFileCountLimit: retainedFileCountLimit);
            var loggerFactory = new LoggerFactory();
            return loggerFactory.AddSerilog(fileLogger);
        }

        public static ILoggerFactory ClefFileLoggerFactory(
            string logFilePath = null
            , LogEventLevel level = LogEventLevel.Verbose
            , int retainedFileCountLimit = 10)
        {
            var fileLogger = SerilogLoggerInstance.ClefFileLogger(logFilePath:logFilePath,level:level,retainedFileCountLimit:retainedFileCountLimit);
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(fileLogger);
            return loggerFactory;
        }

        public static ILoggerFactory ConsoleLoggerFactory()
        {
            var consoleLogger = SerilogLoggerInstance.ConsoleLogger();
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(consoleLogger);
            return loggerFactory;
        }
    }
}
