using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MyWebApp;

public record Person(string Email, string Password, string City, string Company);
public class AuthOptions
{
    public const string ISSUER = "MyAuthServer"; // издатель токена
    public const string AUDIENCE = "MyAuthClient"; // потребитель токена
    const string KEY = "mysupersecret_secretkey!123";   // ключ для шифрации
    public static SymmetricSecurityKey GetSymmetricSecurityKey() => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));

    public static List<Person> people = new List<Person>
    {
        new Person("tom@gmail.com", "12345", "Voronez", "Microsoft"),
        new Person("bob@gmail.com", "55555", "Moscow", "Apple"),
        new Person("sam@gmail.com", "11111", "Berlin", "Microsoft")
    };
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMvc();
        builder.Services.AddServerSideBlazor();

        builder.Services.AddAuthorization(opt =>
        {
            opt.AddPolicy("microsoft_policy", policy =>
            {
                policy.RequireClaim("company", "Microsoft");
            });
            opt.AddPolicy("apple_policy", policy =>
            {
                policy.RequireClaim("company", "Apple");
            });
        });
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(option =>
            {
                option.LoginPath = "/login";
                option.AccessDeniedPath = "/login";
            });

        WebApplication app = builder.Build();

        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCors(builder => builder.AllowAnyOrigin());

        app.Map("/", [Authorize](HttpContext context) =>
        {
            var login = context.User.FindFirst(ClaimTypes.Name);
            var city = context.User.FindFirst(ClaimTypes.Locality);
            var company = context.User.FindFirst("company");
            return $"Name: {login?.Value}\nCity: {city?.Value}\nCompany: {company?.Value}";


        });

        app.Map("/microsoft", [Authorize(Policy = "microsoft_policy")]() => "You are living in Microsoft");
        app.Map("/apple", [Authorize(Policy = "apple_policy")]() => "You are working in Apple");

        app.MapGet("/login", async (IWebHostEnvironment env, HttpContext context) =>
        {
            var fileProvider = new PhysicalFileProvider(env.WebRootPath);
            var fileinfo = fileProvider.GetFileInfo("index.html");

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(fileinfo);
        });

        app.MapPost("/login", async (HttpContext context) =>
        {
            var data = context.Request.Form;
            if(data.ContainsKey("email") && data.ContainsKey("password"))
            {
                var (email, password) = (data["email"], data["password"]);


                Person? person = AuthOptions.people.FirstOrDefault(p => p.Email == email && p.Password == password);
                if (person == null) return Results.Unauthorized();

                var claim = new List<Claim> {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Locality, person.City),
                    new Claim("company", person.Company)

                };
                ClaimsIdentity identity = new ClaimsIdentity(claim, "Cookies");
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            }
            return Results.Redirect("/");
        });

        app.Map("/data", [Authorize(Policy = "OnlyForMicrosoft")] () => new { message = "Hello World!" });
        app.Map("/logout", async (HttpContext context) => await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme));

        //app.UseEndpoints(endpoints =>
        //{
        //    endpoints.MapControllerRoute
        //    (
        //        name: "default",
        //        pattern: "{controller=Home}/{action=Index}/{id?}"
        //    );
        //    endpoints.MapBlazorHub();
        //});

        await app.RunAsync();
    }

}