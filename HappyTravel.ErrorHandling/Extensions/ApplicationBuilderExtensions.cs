using System.Collections;
using System.Collections.Generic;
using System.Net;
using HappyTravel.ErrorHandling.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HappyTravel.ErrorHandling.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseProblemDetailsErrorHandler(this IApplicationBuilder app, IWebHostEnvironment env, ILogger? logger = null,
            string correlationRequestIdHeader = DefaultRequestHeader)
        {
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseProblemDetailsExceptionHandler(env, logger, correlationRequestIdHeader);

            return app;
        }


        public static IApplicationBuilder UseProblemDetailsExceptionHandler(this IApplicationBuilder app, IWebHostEnvironment env, ILogger? logger = null,
            string correlationRequestIdHeader = DefaultRequestHeader)
        {
            app.UseExceptionHandler(builder =>
            {
                builder.Run(
                    async context =>
                    {
                        context.Response.StatusCode = (int) ExceptionStatusCode;
                        context.Response.ContentType = ContentType;

                        var feature = context.Features.Get<IExceptionHandlerFeature>();
                        if (feature?.Error is null)
                            return;

                        var exception = feature.Error;
                        logger?.LogError(exception, exception.Message);

                        var accessor = (IHttpContextAccessor) builder.ApplicationServices.GetService(typeof(IHttpContextAccessor));
                        var httpContext = accessor.HttpContext ?? new DefaultHttpContext();

                        var title = ReasonPhrases.GetReasonPhrase((int) ExceptionStatusCode);
                        var factory = (PublicProblemDetailsFactory) builder.ApplicationServices.GetService(typeof(ProblemDetailsFactory));
                        var details = factory.CreateProblemDetails(httpContext, ExceptionStatusCode, title, exception.Message);
                        httpContext.Request.Headers.TryGetValue(correlationRequestIdHeader, out var requestId);
                        details.Extensions.Add(new KeyValuePair<string, object>(correlationRequestIdHeader, requestId));

                        if (!env.IsProduction())
                            details.Extensions.Add(nameof(exception.StackTrace), exception.StackTrace);

                        foreach (DictionaryEntry entry in exception.Data)
                            details.Extensions.Add(entry.Key.ToString()!, entry.Value);

                        await StreamHelper.CopyDetailsTo(context.Response.Body, details);
                    });
            });

            return app;
        }


        private const string ContentType = "application/json";
        private const string DefaultRequestHeader = "x-request-id";
        private const HttpStatusCode ExceptionStatusCode = HttpStatusCode.InternalServerError;
    }
}
