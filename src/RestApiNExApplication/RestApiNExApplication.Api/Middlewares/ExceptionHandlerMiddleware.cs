using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RestApiNExApplication.Api.Middlewares
{
    /// <summary>
    /// Middleware - error handling
    /// </summary>
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
                if (Startup.Configuration["Exception:ThrowExceptionAfterLog"] == "True")
                    throw;
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            //get inner if exists
            var innerExceptionMsg = string.Empty;
            if (exception.InnerException != null)
            {
                innerExceptionMsg = exception.InnerException.Message;
                if (exception.InnerException.InnerException != null)
                {
                    innerExceptionMsg = innerExceptionMsg + Environment.NewLine + exception.InnerException.InnerException.Message;
                }
            }
            var result = JsonConvert.SerializeObject(new
            {
                // customize as you need
                error = new
                {
                    message = exception.Message + Environment.NewLine + innerExceptionMsg,
                    exception = exception.GetType().Name
                }
            });
            await response.WriteAsync(result);
            //serilog
            Log.Error("ERROR HAPPENED", result);

        }

    }

}
