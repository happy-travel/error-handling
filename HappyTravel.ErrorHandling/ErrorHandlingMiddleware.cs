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
            await using var proxyStream = new MemoryStream();
            var originalBody = SwapBodyStream(httpContext.Response, proxyStream);

            try
            {
                await _next(httpContext);
                
                proxyStream.Seek(0, SeekOrigin.Begin);

                if (httpContext.Response.StatusCode < (int) HttpStatusCode.BadRequest ||
                    httpContext.Response.StatusCode == (int) HttpStatusCode.InternalServerError)
                    return;

                ProblemDetails? problemDetails;
                if (proxyStream.Length == 0)
                    problemDetails = factory.CreateProblemDetails(httpContext, new ProblemDetails
                    {
                        Detail = "Details weren't specified",
                        Status = httpContext.Response.StatusCode
                    });
                else
                    problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(proxyStream);

                var enrichedDetails = factory.CreateProblemDetailsWithContext(problemDetails!);
                
                await StreamHelper.Rewrite(proxyStream, enrichedDetails);
            }
            catch (Exception ex)
            {
                var problemDetails = factory.CreateProblemDetails(httpContext, new ProblemDetails
                {
                    Detail = ex.Message,
                    Status = (int) HttpStatusCode.InternalServerError
                });
                
                await StreamHelper.Rewrite(proxyStream, problemDetails);
            }
            finally
            {
                await PutBodyStreamBack(httpContext.Response, originalBody);
            }
        }


        private Stream SwapBodyStream(HttpResponse response, Stream stream)
        {
            var body = response.Body;
            response.Body = stream;
            
            return body;
        }


        private async Task PutBodyStreamBack(HttpResponse response, Stream bodyStream)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            response.Headers.ContentLength = response.Body.Length;
            await response.Body.CopyToAsync(bodyStream);
            response.Body = bodyStream;
        }

        
        private readonly RequestDelegate _next;
    }
}
