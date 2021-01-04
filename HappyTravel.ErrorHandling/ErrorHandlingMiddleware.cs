using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using HappyTravel.ErrorHandling.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.ErrorHandling
{
    public class ErrorHandlingMiddleware
    {
        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }


        public async Task Invoke(HttpContext httpContext, PublicProblemDetailsFactory factory)
        {
            var originalBody = httpContext.Response.Body;
            try
            {
                await using var proxyStream = new MemoryStream();
                httpContext.Response.Body = proxyStream;
                await _next(httpContext);
                
                proxyStream.Position = 0;

                if (httpContext.Response.StatusCode < (int) HttpStatusCode.BadRequest ||
                    httpContext.Response.StatusCode == (int) HttpStatusCode.InternalServerError)
                {
                    await proxyStream.CopyToAsync(originalBody);
                    return;
                }

                var problemDetails = httpContext.Response.Body switch
                {
                    null => factory.CreateProblemDetails(httpContext, new ProblemDetails
                    {
                        Detail = "Problem details wasn't specified.",
                        Status = httpContext.Response.StatusCode
                    }),
                    _ => await JsonSerializer.DeserializeAsync<ProblemDetails>(httpContext.Response.Body)
                };

                var enrichedDetails = factory.CreateProblemDetailsWithContext(problemDetails);
                await StreamHelper.CopyDetailsTo(originalBody, enrichedDetails);
            }
            catch (Exception ex)
            {
                var problemDetails = factory.CreateProblemDetails(httpContext, new ProblemDetails
                {
                    Detail = ex.Message,
                    Status = (int) HttpStatusCode.InternalServerError
                });
                await StreamHelper.CopyDetailsTo(originalBody, problemDetails);
            }
            finally 
            {
                httpContext.Response.Body = originalBody;
            }
        }


        private readonly RequestDelegate _next;
    }
}
