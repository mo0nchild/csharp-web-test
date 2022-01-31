using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Text.Json.Serialization;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

namespace MyWebApp;

public static class FileLoggerExtension
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string filepath)
    {
        builder.AddProvider(new FileLoggerProvider(filepath));
        return builder;
    }
}

class FileLoggerProvider : ILoggerProvider
{
    string file;

    public FileLoggerProvider(string file)
    {
        this.file = file;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(file);
    }

    public void Dispose() { }
}

class FileLogger : ILogger, IDisposable
{
    string filepath;
    object _lock = new();
    public FileLogger(string filepath)
    {
        this.filepath = filepath;
    }

    public IDisposable BeginScope<TState>(TState state) => this;
    public void Dispose() { }
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_lock)
        {
            File.AppendAllText(filepath, formatter(state, exception) + Environment.NewLine);
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    { 
        var builder = WebApplication.CreateBuilder(args);
        builder?.Logging.AddFileLogger(Path.Combine(Directory.GetCurrentDirectory(), "logging.txt"));

        WebApplication? app = builder?.Build();
        if (app == null) throw new Exception("Can't build application");

        app.Map("/", async (HttpContext req, ILogger<FileLoggerProvider> log) => 
        {
            log.LogInformation("Hello");
            await req.Response.WriteAsync("Home");
        });

        await app.RunAsync();
    }

}