using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HappyTravel.ErrorHandling.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseProblemDetailsExceptionHandler(this IApplicationBuilder app, IWebHostEnvironment env, ILogger? logger = null)
        {
            app.UseExceptionHandler(builder =>
            {
                builder.Run(
                async context =>
                {
                    context.Response.StatusCode = (int) StatusCode;
                    context.Response.ContentType = ContentType;

                    var feature = context.Features.Get<IExceptionHandlerFeature>();
                    if (feature?.Error is null)
                        return;

                    var exception = feature.Error;
                    logger?.LogError(exception, exception.Message);

                    var accessor = (IHttpContextAccessor) builder.ApplicationServices.GetService(typeof(IHttpContextAccessor));
                    var httpContext = accessor?.HttpContext ?? new DefaultHttpContext();

                    var factory = (PublicProblemDetailsFactory) builder.ApplicationServices.GetService(typeof(ProblemDetailsFactory));
                    var details = factory.CreateProblemDetails(httpContext, StatusCode, StatusCodeTitle, detail: exception.Message);

                    httpContext?.Request.Headers.TryGetValue(CorrelationRequestIdHeader, out var requestId);
                    details.Extensions.Add(new KeyValuePair<string, object>(CorrelationRequestIdHeader, requestId));

                    if (!env.IsProduction())
                        details.Extensions.Add(nameof(exception.StackTrace), exception.StackTrace);

                    foreach (DictionaryEntry entry in exception.Data)
                        details.Extensions.Add(entry.Key.ToString(), entry.Value);

                    var json = JsonSerializer.Serialize(details);
                    await context.Response.WriteAsync(json);
                });
            });

            return app;
        }


        private const string ContentType = "application/json";
        private const string CorrelationRequestIdHeader = "x-request-id";
        private const HttpStatusCode StatusCode = HttpStatusCode.InternalServerError;
        private const string StatusCodeTitle = "Internal Server Error";
    }
}
