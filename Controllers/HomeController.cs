using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace MyWebApp.Controllers;

[Controller]
public class HomeController : Controller
{
    private readonly ILogger<ConsoleLoggerProvider> logger;
    private readonly IConfiguration conf;
    public HomeController(ILogger<ConsoleLoggerProvider> logger, IConfiguration conf)
    {
        this.conf = conf;
        this.logger = logger;
    }

    [Route("/")]
    [HttpGet]
    public async Task<ActionResult> Index()
    {
        logger.LogInformation($"{conf.GetConnectionString("db")}");
        using (var db = new Database.DbService($"{conf.GetConnectionString("db")}"))
        {
            logger.LogInformation($"{await db.GetData()}");
        }

        return View("Index", new Models.User { Name = "Shit" });
    }

    [Route("pre")]
    [HttpGet]
    public ActionResult Pre()
    {
        int x = 0;
        int y = 10 / x;
        return View("Index", new Models.User { Name = "Not Mike" });
    }

    [HttpPost]
    public ActionResult IndexPost()
    {
        foreach(var i in HttpContext.Request.Form)
        {
            logger.Log(LogLevel.Information, $"{i.Value}");
        }
       
        return View("Index", new Models.User { Name = "Mike"});
    }
}

