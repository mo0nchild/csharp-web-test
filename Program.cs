using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MyWebApp;

public class Person
{
    public string Name { get; set; } = "none";
    public string Id { get; set; } = "none";
    public int Age { get; set; }
}

public class MyComponent<T> where T : Person 
{

    public List<T> Users { get; private set; }
    public static readonly string expressionGuid = @"^/api/users/\w{8}-\w{4}-\w{4}-\w{4}-\w{12}$";

    public MyComponent(List<T> list) => Users = list;

    public async Task GetAllUsers(HttpResponse response)
    {
        await response.WriteAsJsonAsync(Users);
    }

    public async Task GetUser(string? id, HttpContext context)
    {
        Person? person = Users.FirstOrDefault((x) => x.Id == id);
        if(person != null)
        {
            await context.Response.WriteAsJsonAsync(person);
        }
        else
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { message = "User not found"});
        }
    }

    public async Task UpdateUser(HttpContext context)
    {
        try
        {
            Person? person = await context.Request.ReadFromJsonAsync<Person>();
            if(person != null)
            {
                var user = Users.FirstOrDefault(u => u.Id == person.Id);
                if(user != null)
                {
                    user.Age = person.Age;
                    user.Name = person.Name;
                    await context.Response.WriteAsJsonAsync(user);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { message = "User not found" });
                }
            }
            else
            {
                throw new Exception("Uncorrect Data");
            }

        }
        catch (Exception)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new {message = "Uncorrect Data"});
        }
    }

    public async Task DeleteUser(string? id, HttpContext context)
    {
        Person? person = Users.Find(u => u.Id == id);
        if(person != null)
        {
            Users.Remove((T)person);
            await context.Response.WriteAsJsonAsync(person);
        }
        else
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { message = "User not found" });
        }
    }

    public async Task CreateUser(HttpContext context)
    {
        try
        {
            Person? person = await context.Request.ReadFromJsonAsync<Person>();
            if (person != null)
            {
                person.Id = Guid.NewGuid().ToString();
                Users.Add((T)person);

                await context.Response.WriteAsJsonAsync(person);
            }
            else throw new Exception("Uncorrect Data");
        }
        catch (Exception)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new {message = "Uncorrect Data" });
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    { 
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });
        WebApplication app = builder.Build();

        var component = new MyComponent<Person>(new List<Person> 
        {
            new Person { Name = "Mike", Age = 18, Id = Guid.NewGuid().ToString() },
            new Person { Name = "Jeremy", Age = 36, Id = Guid.NewGuid().ToString() },
            new Person { Name = "Kate", Age = 47, Id = Guid.NewGuid().ToString() },
        });

        app.Run(new RequestDelegate(Handler(component)));
        await app.RunAsync();
    }

    public static Func<HttpContext, Task> Handler<T> (MyComponent<T> component) where T : Person
    {
        return async delegate(HttpContext context)
        {
            var (request, response) = (context.Request, context.Response);
            string? id = null;

            foreach(var i in component.Users)
            {
                Console.WriteLine($"{i.Name}\t{ i.Age }\t{i.Id}");
            }
            Console.WriteLine();

            switch (request.Method)
            {

            case "GET" when request.Path == "/api/users":
                await component.GetAllUsers(response);
                break;

            case "GET" when Regex.IsMatch(request.Path, MyComponent<T>.expressionGuid):
                id = request.Path.Value?.Split('/')[3];
                await component.GetUser(id, context);
                break;

            case "POST" when request.Path == "/api/users":
                await component.CreateUser(context);
                break;

            case "PUT" when request.Path == "/api/users":
                await component.UpdateUser(context);
                break;

            case "DELETE" when Regex.IsMatch(request.Path, MyComponent<T>.expressionGuid):                
                id = request.Path.Value?.Split('/')[3];
                await component.DeleteUser(id, context);
                break;

            default:
                response.ContentType = "text/html; charset=utf-8";
                await response.SendFileAsync("html/index.html");
                break;

            }
        };
        
    }

}