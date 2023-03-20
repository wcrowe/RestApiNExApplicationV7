using AutoMapper.Configuration;
using LazyCache;
using Loggly;
using Loggly.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestApiNExApplication.Api;
using RestApiNExApplication.Api.Middlewares;
using RestApiNExApplication.Api.Settings;
using RestApiNExApplication.Api.Utilities;
using RestApiNExApplication.Domain.Mapping;
using RestApiNExApplication.Domain.Service;
using RestApiNExApplication.Entity.Context;
using RestApiNExApplication.Entity.UnitofWork;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

//var builder = WebApplication.CreateBuilder(args);
//var app = builder.Build();
//app.MapGet("/", () => "Hello World");
//app.Run();

var builder = WebApplication.CreateBuilder(args);
var startup = new Startup(builder.Configuration);


#region "Attach services"

try
{

    #region "Add basic services"

    builder.Services.AddControllers();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });
    #endregion

    #region "API versioning"
    //API versioning service
    builder.Services.AddApiVersioning(
        o =>
        {
            //o.Conventions.Controller<UserController>().HasApiVersion(1, 0);
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.ReportApiVersions = true;
            o.DefaultApiVersion = new ApiVersion(1, 0);
            o.ApiVersionReader = new UrlSegmentApiVersionReader();
        }
        );

    // format code as "'v'major[.minor][-status]"
    builder.Services.AddVersionedApiExplorer(
    options =>
    {
        options.GroupNameFormat = "'v'VVV";
        //versioning by url segment
        options.SubstituteApiVersionInUrl = true;
    });
    #endregion

    #region "DB service"
    DefaultDbContext.DotNetRunningInDockerContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    string connectionString = "ConnectionStrings:RestApiNExApplicationDB";
    if (DefaultDbContext.DotNetRunningInDockerContainer) connectionString += "Docker";

    if (builder.Configuration["ConnectionStrings:UseInMemoryDatabase"] == "True")
        builder.Services.AddDbContext<DefaultDbContext>(opt => opt.UseInMemoryDatabase("TestDB-" + Guid.NewGuid().ToString()));
    else
        builder.Services.AddDbContext<DefaultDbContext>(options => options.UseSqlServer(builder.Configuration[connectionString]));
    //read configs
    DefaultDbContext.RespositoryUseThreadSafeDictionary = builder.Configuration["RespositoryUseThreadSafeDictionary"] == "True";
    DefaultDbContext.UseLazyLoadingProxies = builder.Configuration["ConnectionStrings:UseLazyLoadingProxies"] == "True";
    //turn off lazy loading if used ThreadSafe dictionary
    if (DefaultDbContext.RespositoryUseThreadSafeDictionary == true)
        DefaultDbContext.UseLazyLoadingProxies = false;
    #endregion

    #region "Authentication"
    if (builder.Configuration["Authentication:UseIdentityServer4"] == "False")
    {
        //JWT API authentication service
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Issuer"],
                RequireExpirationTime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };
        }
        );
    }
    else
    {
        //Identity Server 4 API authentication service
        builder.Services.AddAuthorization();
        //.AddJsonFormatters();
        builder.Services.AddAuthentication("Bearer")
        .AddIdentityServerAuthentication(option =>
        {
            option.Authority = builder.Configuration["Authentication:IdentityServer4IP"];
            option.RequireHttpsMetadata = false;
            //option.ApiSecret = "secret";
            option.ApiName = "RestApiNExApplication";  //This is the resourceAPI that we defined in the Config.cs in the AuthServ project (apiresouces.json and clients.json). They have to be named equal.
        });

    }
    #endregion

    #region "CORS"
    // include support for CORS
    // More often than not, we will want to specify that our API accepts requests coming from other origins (other domains). When issuing AJAX requests, browsers make preflights to check if a server accepts requests from the domain hosting the web app. If the response for these preflights don't contain at least the Access-Control-Allow-Origin header specifying that accepts requests from the original domain, browsers won't proceed with the real requests (to improve security).
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy-public",
            builder => builder.AllowAnyOrigin()   //WithOrigins and define a specific origin to be allowed (e.g. https://mydomain.com)
                .AllowAnyMethod()
                .AllowAnyHeader()
        //.AllowCredentials()
        .Build());
    });
    #endregion

    #region "DDoS attack service"
    if (builder.Configuration["DDosAttackProtection:Enabled"] == "True")
        builder.Services.AddSingleton(typeof(DDosAttackMonitoringService));
    #endregion

    #region "MVC and JSON options"
    //mvc service (set to ignore ReferenceLoopHandling in json serialization like Users[0].Account.Users)
    //in case you need to serialize entity children use commented out option instead
    builder.Services.AddMvc(option => option.EnableEndpointRouting = false)
.AddNewtonsoftJson(options => { options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore; });  //NO entity classes' children serialization
                                                                                                                                      //.AddNewtonsoftJson(ops =>
                                                                                                                                      //{
                                                                                                                                      //    ops.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize;
                                                                                                                                      //    ops.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;                                                                                                                                     //}); //WITH entity classes' children serialization
    #endregion

    #region "DI code"
    //general unitofwork injections
    builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

    //services injections - AddTransient or AddScoped
    builder.Services.AddScoped(typeof(AccountService<,>), typeof(AccountService<,>));
    builder.Services.AddScoped(typeof(UserService<,>), typeof(UserService<,>));
    builder.Services.AddScoped(typeof(AccountServiceAsync<,>), typeof(AccountServiceAsync<,>));
    builder.Services.AddScoped(typeof(UserServiceAsync<,>), typeof(UserServiceAsync<,>));
    //
    startup.DIServicesBuilt(builder.Services);
    //...add other services
    //
    builder.Services.AddScoped(typeof(IService<,>), typeof(GenericService<,>));
    builder.Services.AddScoped(typeof(IServiceAsync<,>), typeof(GenericServiceAsync<,>));
    #endregion

    #region "Add services lazy cache 3s by default"
    //NOTE that unit tests can be affected by updating cache timeout
    int serviceCacheSeconds;
    int.TryParse(builder.Configuration["ServiceCacheSeconds"], out serviceCacheSeconds);
    builder.Services.AddLazyCache(serviceProvider => new CachingService(CachingService.DefaultCacheProvider)
    {
        DefaultCachePolicy = { DefaultCacheDurationSeconds = serviceCacheSeconds }
    });
    #endregion

    #region"Data mapper services configuration"
    builder.Services.AddAutoMapper(typeof(MappingProfile));
    #endregion

    #region "Swagger API"
    //Swagger API documentation
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "RestApiNExApplication API", Version = "v1" });
        c.SwaggerDoc("v2", new OpenApiInfo { Title = "RestApiNExApplication API", Version = "v2" });

        //In Test project find attached swagger.auth.pdf file with instructions how to run Swagger authentication 
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Description = "Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        });


        c.AddSecurityRequirement(new OpenApiSecurityRequirement{
                        {
                            new OpenApiSecurityScheme{
                                Reference = new OpenApiReference{
                                    Id = "Bearer", //The name of the previously defined security scheme.
                                    Type = ReferenceType.SecurityScheme
                                }
                            },new List<string>()
                        }
                    });

        //c.DocumentFilter<api.infrastructure.filters.SwaggerSecurityRequirementsDocumentFilter>();
    });
    #endregion
}
catch (Exception ex)
{
    Log.Error(ex.Message);
}

#endregion

#region "Build web app"

var app = builder.Build();

#region "Read configurationa and setup logger"

string env = app.Environment.EnvironmentName;
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
            .Build();

//Must have Loggly account and setup correct info in appsettings
if (configuration["Serilog:UseLoggly"] == "true")
{
    var logglySettings = new LogglySettings();
    configuration.GetSection("Serilog:Loggly").Bind(logglySettings);
    Startup.SetupLogglyConfiguration(logglySettings);
}

Log.Logger = new LoggerConfiguration()
.ReadFrom.Configuration(configuration)
.CreateLogger();

#endregion



//Forwarded Headers Middleware should run before other middleware. This ordering ensures that the middleware relying on forwarded headers information can consume the header values for processing.
app.UseForwardedHeaders();

if (app.Environment.EnvironmentName == "Development")
    app.UseDeveloperExceptionPage();
else
{
    app.UseMiddleware<ExceptionHandlerMiddleware>();
    //app.UseHsts();
}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseCookiePolicy();
//app.UseRouting();
//app.UseRequestLocalization();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/", async http =>
    {
        http.Response.Redirect("api/info", true);
    });
    endpoints.MapGet("/api", async http =>
    {
        http.Response.Redirect("api/info", true);
    });
});

app.UseCors("CorsPolicy-public");  //apply to every request
app.UseAuthentication(); //needs to be up in the pipeline, before MVC
app.UseAuthorization();

// app.UseSession();
// app.UseResponseCompression();
// app.UseResponseCaching()


//Swagger API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RestApiNExApplication API V1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "RestApiNExApplication API V2");
    c.DisplayOperationId();
    c.DisplayRequestDuration();
});

//Middlewares (orders of all middlewares(including custom) is very important)
//1
//NOTE:  this pipeline (1) is only used when integration tests run to populate empty
//       requests RemoteIp address required for DDoS attacks prevention test
if (builder.Configuration["IntegrationTests"] == "True")
    app.UseFakeRemoteIpAddressMiddlewareExtensions();
//2
if (builder.Configuration["DDosAttackProtection:Enabled"] == "True")
    app.UseDDosAttackStopMiddlewareExtensions();

app.UseMvc();

#endregion

#region "Migrations and seeds from json files"
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    if (builder.Configuration["ConnectionStrings:UseInMemoryDatabase"] == "False" && !serviceScope.ServiceProvider.GetService<DefaultDbContext>().AllMigrationsApplied())
    {
        if (builder.Configuration["ConnectionStrings:UseMigrationService"] == "True")
            serviceScope.ServiceProvider.GetService<DefaultDbContext>().Database.Migrate();
    }
    //it will seed tables on aservice run from json files if tables empty
    if (builder.Configuration["ConnectionStrings:UseSeedService"] == "True")
        serviceScope.ServiceProvider.GetService<DefaultDbContext>().EnsureSeeded();
}
#endregion

#region "Start app"
try
{
    Log.Information("Starting web REST API application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Web REST API application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

//.NET 6 compiler generates the Program class behind the scenes as the internal class, thus making it inaccessible in our integration testing project. 
//So to solve this, we can create a public partial Program class in the Program.cs file
public partial class Program { }
#endregion

namespace RestApiNExApplication.Api
{
    public partial class Startup
    {

        public static Microsoft.Extensions.Configuration.IConfiguration Configuration { get; set; }

        public Startup(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void DIServicesBuilt(IServiceCollection services)
        {
            SetAdditionalDIServices(services);
        }

        //call scaffolded class method to add DIs
        partial void SetAdditionalDIServices(IServiceCollection services);

        /// <summary>
        /// Configure Loggly provider
        /// </summary>
        /// <param name="logglySettings"></param>
        public static void SetupLogglyConfiguration(LogglySettings logglySettings)
        {
            //Configure Loggly
            var config = LogglyConfig.Instance;
            config.CustomerToken = logglySettings.CustomerToken;
            config.ApplicationName = logglySettings.ApplicationName;
            config.Transport = new TransportConfiguration()
            {
                EndpointHostname = logglySettings.EndpointHostname,
                EndpointPort = logglySettings.EndpointPort,
                LogTransport = logglySettings.LogTransport
            };
            config.ThrowExceptions = logglySettings.ThrowExceptions;

            //Define Tags sent to Loggly
            config.TagConfig.Tags.AddRange(new ITag[]{
                    new ApplicationNameTag {Formatter = "Application-{0}"},
                    new HostnameTag { Formatter = "Host-{0}" }
                });
        }

    }
}

namespace api.infrastructure.filters
{
    public class SwaggerSecurityRequirementsDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument document, DocumentFilterContext context)
        {
            document.SecurityRequirements = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement{
                    {
                        new OpenApiSecurityScheme{
                            Reference = new OpenApiReference{
                                Id = "Bearer", //The name of the previously defined security scheme.
                                Type = ReferenceType.SecurityScheme
                            }
                        },new List<string>()
                    }
                },
                new OpenApiSecurityRequirement{
                    {
                        new OpenApiSecurityScheme{
                            Reference = new OpenApiReference{
                                Id = "Basic", //The name of the previously defined security scheme.
                                Type = ReferenceType.SecurityScheme
                            }
                        },new List<string>()
                    }
                }
             };

        }
    }
}
