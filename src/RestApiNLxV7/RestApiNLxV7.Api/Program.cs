using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using RestApiNLxV7.Api.Api;

namespace RestApiNLxV7.Api.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
