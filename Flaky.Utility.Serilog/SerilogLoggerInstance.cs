using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;

namespace Flaky.Utility.Serilog
{
    public static class SerilogLoggerInstance
    {
        public static ILogger ConsoleLogger() => ConsoleLoggerConfiguration().CreateLogger();

        public static ILogger ComplexFileLoggers(
            string logFilePath = null
            , LogEventLevel level = LogEventLevel.Verbose
            //, string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) [{SourceContext}] {Message}{NewLine}{Exception}"
            , int retainedFileCountLimit = 10
            ) 
            =>ComplexFileLoggersConfiguration(logFilePath, level, retainedFileCountLimit).CreateLogger();

        public static ILogger ClefFileLogger(
            string logFilePath = null
            , LogEventLevel level = LogEventLevel.Verbose
            //, string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({ThreadId}) [{SourceContext}] {Message}{NewLine}{Exception}"
            , int retainedFileCountLimit = 10
            )
            //=> FileLoggerConfiguration(logFilePath, level, template).CreateLogger();
            => ClefFileLoggerConfiguration(logFilePath, level, retainedFileCountLimit).CreateLogger();

        public static ILogger ConfiguredLogger(IConfiguration loggerConfiguration)
        {
            return loggerConfiguration == null
                ? throw new ArgumentNullException(nameof(loggerConfiguration))
                : new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(loggerConfiguration)
                    .CreateLogger();
        }

        public static LoggerConfiguration ConsoleLoggerConfiguration() =>
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                //.Enrich.WithCaller()
                .Enrich.WithThreadId()
            .WriteTo
            .Async(a => a.Console(
                    //formatter: new CompactJsonFormatter(),
                    LogEventLevel.Verbose,
                    outputTemplate: "{Timestamp:o}[{Level}][{ThreadId}][{SourceContext}][{Properties:j}] {Message:lj}{NewLine}{Exception}"));

        public static LoggerConfiguration ClefFileLoggerConfiguration(
            string logFilePath = null
            , LogEventLevel level = LogEventLevel.Verbose
            //, string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.ffffffz}[{Level}][{ThreadId}][{SourceContext}][{Properties:j}] {Message}{NewLine}{Exception}"
            , int retainedFileCountLimit = 10
            
            )
            => 
            //string.IsNullOrEmpty(logFilePath) || string.IsNullOrWhiteSpace(logFilePath)
            //    ? throw new ArgumentNullException(nameof(logFilePath))
            //    : 
            new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .Enrich.FromLogContext()
                    //.Enrich.WithCaller()
                    .Enrich.WithThreadId()
                    .WriteTo
                    .Async(a =>
                    {
                        a.File(
                            formatter: new CompactJsonFormatter(),
                            logFilePath ?? "Logger_.clef",
                            level,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: retainedFileCountLimit);
                    });


        public static LoggerConfiguration ComplexFileLoggersConfiguration(
            string logFilePath = null
            , LogEventLevel level = LogEventLevel.Verbose
            , int retainedFileCountLimit = 10

        )
            => ClefFileLoggerConfiguration(logFilePath,level,retainedFileCountLimit)
                    //.Enrich.WithCaller()
                    .WriteTo
                    .Async(a =>
                    {
                        a.File(
                            logFilePath ?? "Logger_.log",
                            level,
                            rollingInterval: RollingInterval.Day,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.ffffffz}[{Level}][{ThreadId}][{SourceContext}][{Properties:j}] {Message}{NewLine}{Exception}");
                    })
                ;

    }
}
