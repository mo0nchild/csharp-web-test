using System;
using System.Threading.Tasks;

using System.Text.Json.Serialization;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MyWebApp;

public class Program
{
    public static async Task Main(string[] args)
    { 
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });
        WebApplication app = builder.Build();

        app.Run(MyComponent);
        await app.RunAsync();
    }

    public record Person(string Name, int Age);

    public async static Task MyComponent(HttpContext context)
    {
        var (response, request) = (context.Response, context.Request);
        if(request.Path == "/api/user")
        {
            var text = "Data Error";
            if (request.HasJsonContentType())
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new MyConverter());

                var person = await request.ReadFromJsonAsync<Person>(options);
                if (person != null) text = $"Name: {person.Name}\tAge: {person.Age}";
            }

            await response.WriteAsJsonAsync(new {Text = text});
        }
        else
        {
            response.ContentType = "text/html; charset=utf-8";
            await response.SendFileAsync("html/index.html");
        }

    }

}

public class MyConverter : JsonConverter<Program.Person>
{
    public override Program.Person? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        (string personName, int personAge) = ("None", 0);

        while (reader.Read())
        {
            if(reader.TokenType == JsonTokenType.PropertyName)
            {
                var property = reader.GetString();
                reader.Read();

                switch (property)
                {
                    case "Age" or "age" when reader.TokenType == JsonTokenType.Number:

                        personAge = reader.GetInt32();

                        break;

                    case "Age" or "age" when reader.TokenType == JsonTokenType.String:

                        string? stringValue = reader.GetString();
                        // пытаемся конвертировать строку в число
                        if (int.TryParse(stringValue, out int value))
                        {
                            personAge = value;
                        }

                        break;

                    case "Name" or "name":

                        personName = reader.GetString() ?? personName;

                        break;

                    default: break;
                }

            };
        }

        return new Program.Person(Name: personName, Age: personAge);
    }

    public override void Write(Utf8JsonWriter writer, Program.Person value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WriteNumber("age", value.Age);
        writer.WriteEndObject();
    }
}