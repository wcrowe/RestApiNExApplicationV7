using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestApiNLxV7.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InfoController : ControllerBase
    {

        public IConfiguration Configuration { get; }
        public InfoController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        //get info
        [AllowAnonymous]
        [HttpGet]
        //[Produces("text/html")]
        [Produces("application/json")]
        //public ActionResult<string> ApiInfo()
        public IActionResult ApiInfo()
        {

            var connstring = Configuration["ConnectionStrings:RestApiNLxV7"];



            var page = "<html><head><link href='https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css' rel='stylesheet' integrity='sha384-Vkoo8x4CGsO3+Hhxv8T/Q5PaXtkKtu6ug5TOeNV6gBiFeWPGFN9MuhOf23Q9Ifjh' crossorigin='anonymous'></head><body>" +
                "<div class='jumbotron'>" +
                "<h1><i class='fab fa-centercode' fa-2x></i>  RestApiNLxV7 Api</h1>" +
                "<h4>Created with RestApiNLx v.7.0.0</h4>" +
                 "REST API Light service started!<br>" +
                 "appsettings.json configuration:<br>" +
                 "<ul><li>.NET 7.0</li>" +
                 "<li>Connection String: " + connstring + "</li></ul>" +
                 "<a class='btn btn-outline-primary' role='button' href='/swagger'><b>Swagger API specification</b></a><br>" +
                 "<a class='btn btn-outline-warning' role='button' href='http://www.anasoft.net/restapi'><b>More instructions and more features</b></a>" +
                 "<a class='btn btn-outline-warning' role='button' href='https://www.youtube.com/channel/UC5XyWfG0nGYp7Q9buusealA'><b>YouTube instructions</b></a>" +
                "</div>" +

                "<div class='row'>" +
                "<div class='col-md-3'>" +
                "<h3>API patterns and services</h3>" +
                "<p><ul><li>Dependency Injection (Net Core feature) </li></ul>" +
                "<ul><li>GetAccount</li>" +
                "<li>GetAccounts</li>" +
                "<li>AddAccount</li>" +
                "<li>UpdateAccounts</li>" +
                "<li>DeleteAccount</li></ul>" +
                "</div>" +
                "<div class='col-md-3'>" +
                "<h3>API projects</h3>" +
                "<ul><li>Api</li><li>Data</li></ul>" +
                "</div>" +

                "</div>" +
                "</body></html>";

            //return page;

            return Content(page, "text/html");


        }

    }
}
