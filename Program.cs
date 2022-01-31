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
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Console;

namespace MyWebApp;

public class Program
{
    public static async Task Main(string[] args)
    { 
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = ".MyApp.Session";
            options.IdleTimeout = TimeSpan.FromSeconds(1000);
            options.Cookie.IsEssential = true;
        });

        WebApplication app = builder.Build();

        app.UseSession();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async (context) =>
            {
                var env = context.RequestServices.GetService<IWebHostEnvironment>();
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.SendFileAsync(Path.Combine(env?.WebRootPath ?? "", "index.html"));
            });
            endpoints.MapPost("/login", async (context) =>
            {
                if(context.Request.Form.TryGetValue("login", out var login) &&
                    context.Request.Form.TryGetValue("password", out var password))
                {
                    await context.Response.WriteAsync($"Ok");
                    context.Session.SetString("auth", $"{login}/{password}");
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Failed");
                }
            });
            endpoints.MapGet("/check", async (context) =>
            {
                if (context.Session.IsAvailable)
                {
                    var auth = context.Session.GetString("auth");
                    await context.Response.WriteAsync($"auth: {auth}\nlogin: {auth?.Split('/')[0]}\npassword: {auth?.Split('/')[1]}");
                }
                else
                {
                    await context.Response.WriteAsync("Session Closed");
                }
            });
        });

        await app.RunAsync();
    }

}