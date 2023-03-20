using AutoMapper;
using FluentMigrator.Runner;
using LazyCache;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using RestApiNDxApiV6.Api;
using RestApiNDxApiV6.Api.Middlewares;
using RestApiNDxApiV6.Api.Utilities;
using RestApiNDxApiV6.Domain.Mapping;
using RestApiNDxApiV6.Domain.Service;
using RestApiNDxApiV6.Entity.Context;
using RestApiNDxApiV6.Entity.Migrations;
using RestApiNDxApiV6.Entity.Queries;
using RestApiNDxApiV6.Entity.Repository;
using RestApiNDxApiV6.Entity.UnitofWork;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ExceptionHandlerMiddleware = RestApiNDxApiV6.Api.Middlewares.ExceptionHandlerMiddleware;

/// <summary>
/// Designed by AnaSoft Inc. 2022
/// http://www.anasoft.net/apincore 
///
/// NOTE:
/// Must update database connection in appsettings.json - "RestApiNDxApiV6.ApiDB"
/// </summary>

namespace RestApiNDxApiV6.Api
{
    public partial class Startup
    {

        public static IConfiguration Configuration { get; set; }
        public IWebHostEnvironment HostingEnvironment { get; private set; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {

            Log.Information("Startup::ConfigureServices");

            try
            {
                //db service
                //provides connection string for RestApiNDxApiV6Context with Dapper implementation
                services.AddTransient<RestApiNDxApiV6Context, RestApiNDxApiV6Context>(sp =>
                {
                    return new RestApiNDxApiV6Context(Configuration["ConnectionStrings:RestApiNDxApiV6DB"]);
                });

                //fluent migration services
                services.AddFluentMigratorCore()
                    .ConfigureRunner(rb => rb
                    // Set the connection string
                    .WithGlobalConnectionString(Configuration["ConnectionStrings:RestApiNDxApiV6DBMigration"])
                    //.AddSqlServer()
                    .AddSqlServer2012()
                    //.AddSqlServer2016()
                    // Define the assemblies containing the migrations
                    .ScanIn(typeof(Migration_Initial).Assembly).For.Migrations()
                    ).AddLogging(lb => lb
                        .AddFluentMigratorConsole().AddSerilog());



                services.AddControllers(
                opt =>
                {
                    //Custom filters can be added here 
                    //opt.Filters.Add(typeof(CustomFilterAttribute));
                    //opt.Filters.Add(new ProducesAttribute("application/json"));
                }
                ).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders =
                        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                });

                #region "API versioning"
                //API versioning service
                services.AddApiVersioning(
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
                services.AddVersionedApiExplorer(
                options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    //versioning by url segment
                    options.SubstituteApiVersionInUrl = true;
                });
                #endregion

                #region "Authentication"
                if (Configuration["Authentication:UseIdentityServer4"] == "False")
                {
                    //JWT API authentication service
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ClockSkew = TimeSpan.Zero,
                            ValidIssuer = Configuration["Jwt:Issuer"],
                            ValidAudience = Configuration["Jwt:Issuer"],
                            RequireExpirationTime = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                        };
                    }
                    );
                }
                else
                {
                    //Identity Server 4 API authentication service
                    services.AddAuthorization();
                    //.AddJsonFormatters();
                    services.AddAuthentication("Bearer")
                    .AddIdentityServerAuthentication(option =>
                    {
                        option.Authority = Configuration["Authentication:IdentityServer4IP"];
                        option.RequireHttpsMetadata = false;
                        //option.ApiSecret = "secret";
                        option.ApiName = "RestApiNDxApiV6";  //This is the resourceAPI that we defined in the Config.cs in the AuthServ project (apiresouces.json and clients.json). They have to be named equal.
                    });

                }
                #endregion

                #region "CORS"
                // include support for CORS
                // More often than not, we will want to specify that our API accepts requests coming from other origins (other domains). When issuing AJAX requests, browsers make preflights to check if a server accepts requests from the domain hosting the web app. If the response for these preflights don't contain at least the Access-Control-Allow-Origin header specifying that accepts requests from the original domain, browsers won't proceed with the real requests (to improve security).
                services.AddCors(options =>
                    {
                        options.AddPolicy("CorsPolicy-public",
                            builder => builder.AllowAnyOrigin()   //WithOrigins and define a specific origin to be allowed (e.g. https://mydomain.com)
                                .AllowAnyMethod()
                                .AllowAnyHeader()
                        //.AllowCredentials()
                        .Build());
                    });
                #endregion

                if (Configuration["DDosAttackProtection:Enabled"] == "True")
                    services.AddSingleton(typeof(DDosAttackMonitoringService));

                #region "MVC and JSON options"
                services.AddMvc(option => option.EnableEndpointRouting = false)
                         .AddNewtonsoftJson();
                #endregion

                #region "DI code"
                //general unitofwork injections
                services.AddScoped<IUnitOfWork, UnitOfWork>();

                //services injections
                services.AddScoped(typeof(AccountService<,>), typeof(AccountService<,>));
                services.AddScoped(typeof(UserService<,>), typeof(UserService<,>));
                services.AddScoped(typeof(AccountServiceAsync<,>), typeof(AccountServiceAsync<,>));
                services.AddScoped(typeof(UserServiceAsync<,>), typeof(UserServiceAsync<,>));
                //
                SetAdditionalDIServices(services);
                //...add other services
                //
                services.AddScoped(typeof(IService<,>), typeof(GenericService<,>));
                services.AddScoped(typeof(IServiceAsync<,>), typeof(GenericServiceAsync<,>));
                #endregion

                #region "Add services lazy cache 3s by default"
                //NOTE that unit tests can be affected by updating cache timeout
                int serviceCacheSeconds;
                int.TryParse(Configuration["ServiceCacheSeconds"], out serviceCacheSeconds);
                services.AddLazyCache(serviceProvider => new CachingService(CachingService.DefaultCacheProvider)
                {
                    DefaultCachePolicy = { DefaultCacheDurationSeconds = serviceCacheSeconds }
                });
                #endregion

                //data mapper services configuration
                services.AddAutoMapper(typeof(MappingProfile));

                #region "Swagger API"
                //Swagger API documentation
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RestApiNDxApiV6 API", Version = "v1" });
                    c.SwaggerDoc("v2", new OpenApiInfo { Title = "RestApiNDxApiV6 API", Version = "v2" });

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
        }


        //call scaffolded class method to add DIs
        partial void SetAdditionalDIServices(IServiceCollection services);


        // This method gets called by the runtime
        // This method can be used to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            Log.Information("Startup::Configure");

            try
            {

                //Forwarded Headers Middleware should run before other middleware. This ordering ensures that the middleware relying on forwarded headers information can consume the header values for processing.
                app.UseForwardedHeaders();

                if (env.EnvironmentName == "Development")
                    app.UseDeveloperExceptionPage();
                else
                {
                    app.UseMiddleware<ExceptionHandlerMiddleware>();
                    //app.UseHsts();
                }

                //app.UseHttpsRedirection();
                //app.UseStaticFiles();
                //app.UseCookiePolicy();
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
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RestApiNDxApiV6 API V1");
                    c.SwaggerEndpoint("/swagger/v2/swagger.json", "RestApiNDxApiV6 API V2");
                    c.DisplayOperationId();
                    c.DisplayRequestDuration();
                });

                //Middlewares (orders of all middlewares(including custom) is very important)
                //1
                //NOTE:  this pipeline (1) is only used when integration tests run to populate empty
                //       requests RemoteIp address required for DDoS attacks prevention test
                if (Configuration["IntegrationTests"] == "True")
                    app.UseFakeRemoteIpAddressMiddlewareExtensions();
                //2
                if (Configuration["DDosAttackProtection:Enabled"] == "True")
                    app.UseDDosAttackStopMiddlewareExtensions();


                app.UseMvc();


                //migrations and seeds from json files
                using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    if (Configuration["ConnectionStrings:UseMigrationService"] == "True")
                    {
                        var runner = serviceScope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                        DatabaseHelper.CreateIfNotExists(Configuration["ConnectionStrings:RestApiNDxApiV6DB"]);  //Create database process must be outside migration transaction
                        runner.MigrateUp();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
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







