using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using HappyTravel.ErrorHandling.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace HappyTravel.ErrorHandling
{
    /// <inheritdoc/>
    public class PublicProblemDetailsFactory : ProblemDetailsFactory
    {
        public PublicProblemDetailsFactory(IOptions<ApiBehaviorOptions>? options, IHttpContextAccessor? httpContextAccessor = null)
        {
            _httpContextAccessor = httpContextAccessor; //This isn't safe but I assume the factory is for web projects only, and the accessor is always in place
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }


        /// <summary>
        /// Creates a <see cref="ProblemDetails" /> instance that configures defaults based on values specified in <see cref="ApiBehaviorOptions" />.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext" />.</param>
        /// <param name="statusCode">The value for <see cref="ProblemDetails.Status"/>.</param>
        /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
        /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
        /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
        /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
        /// <returns>The <see cref="ProblemDetails"/> instance.</returns>
        public ProblemDetails CreateProblemDetails(HttpContext httpContext, HttpStatusCode statusCode, string title, string detail, string? type = null, string? instance = null)
        {
            if (title is null)
                throw new ArgumentNullException(nameof(title));

            if (detail is null)
                throw new ArgumentNullException(nameof(detail));

            return CreateProblemDetails(httpContext, (int) statusCode, title, type, detail, instance);
        }


        /// <summary>
        /// Creates a <see cref="ProblemDetails" /> instance that configures defaults based on values specified in <see cref="ApiBehaviorOptions" />.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext" />.</param>
        /// <param name="statusCode">The value for <see cref="ProblemDetails.Status"/>.</param>
        /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
        /// <param name="problemDetails">The existing instance of <see cref="ProblemDetails" />.</param>
        /// <returns>The <see cref="ProblemDetails"/> instance.</returns>
        public ProblemDetails CreateProblemDetails(HttpContext httpContext, ProblemDetails problemDetails, int? statusCode = null, string? title = null)
        {
            statusCode ??= problemDetails.Status;
            title ??= problemDetails.Title;

            var newProblemDetails = CreateProblemDetails(httpContext, statusCode, title, problemDetails.Type, problemDetails.Detail);
            
            return AddExtensions(newProblemDetails, problemDetails.Extensions.Select(kvp => (kvp.Key, kvp.Value)));
        }


        /// <inheritdoc/>
        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null,
            string? detail = null, string? instance = null)
        {
            statusCode ??= (int) HttpStatusCode.InternalServerError;
            title ??= ReasonPhrases.GetReasonPhrase(statusCode.Value);

            var context = httpContext.Features.Get<IExceptionHandlerFeature>();
            if (context?.Error != null)
                detail ??= context.Error?.Message;

            var problemDetails = new ProblemDetails
            {
                Detail = detail,
                Instance = instance,
                Status = statusCode,
                Title = title,
                Type = type
            };

            return ApplyProblemDetailsDefaults(httpContext, problemDetails);
        }


        /// <inheritdoc/>
        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null,
            string? title = null, string? type = null, string? detail = null, string? instance = null)
        {
            if (modelStateDictionary is null)
                throw new ArgumentNullException(nameof(modelStateDictionary));

            statusCode ??= (int) HttpStatusCode.BadRequest;

            var problemDetails = new ValidationProblemDetails(modelStateDictionary)
            {
                Detail = detail,
                Instance = instance,
                Status = statusCode,
                Title = title,
                Type = type
            };

            if (title is not null)
                problemDetails.Title = title;

            return (ValidationProblemDetails) ApplyProblemDetailsDefaults(httpContext, problemDetails);
        }


        /// <summary>
        /// Creates a <see cref="ProblemDetails" /> instance that configures defaults based on values specified in <see cref="ApiBehaviorOptions" />.
        /// </summary>
        /// <param name="problemDetails">The existing instance of <see cref="ProblemDetails" />.</param>
        /// <param name="statusCode">The value for <see cref="ProblemDetails.Status" />.</param>
        /// <param name="title">The value for <see cref="ProblemDetails.Title"/>.</param>
        /// <returns>The <see cref="ProblemDetails"/> instance.</returns>
        public ProblemDetails CreateProblemDetailsWithContext(ProblemDetails problemDetails, int? statusCode = null, string? title = null)
        {
            if (_httpContextAccessor is null)
                throw new NullReferenceException("Can't get Http Context or its value is null");

            statusCode ??= problemDetails.Status;
            title ??= problemDetails.Title;

            var newProblemDetails = CreateProblemDetails(_httpContextAccessor.HttpContext, statusCode, title, problemDetails.Type, problemDetails.Detail);
            
            return AddExtensions(newProblemDetails, problemDetails.Extensions.Select(kvp => (kvp.Key, kvp.Value)));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="statusCode">The value for <see cref="ProblemDetails.Status" />.</param>
        /// <param name="title">The existing instance of <see cref="ProblemDetails.Title" />.</param>
        /// <param name="type">The value for <see cref="ProblemDetails.Type"/>.</param>
        /// <param name="detail">The value for <see cref="ProblemDetails.Detail"/>.</param>
        /// <param name="instance">The value for <see cref="ProblemDetails.Instance"/>.</param>
        /// <returns></returns>
        public ProblemDetails CreateProblemDetailsWithContext(int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
        {
            if (_httpContextAccessor is null)
                throw new NullReferenceException("Can't get Http Context or its value is null");

            return CreateProblemDetails(_httpContextAccessor.HttpContext, statusCode, title, type, detail, instance);
        }


        private ProblemDetails ApplyProblemDetailsDefaults(HttpContext httpContext, ProblemDetails problemDetails)
        {
            if (problemDetails.Status is not null && _options.ClientErrorMapping.TryGetValue(problemDetails.Status.Value, out var clientErrorData))
            {
                problemDetails.Title ??= clientErrorData.Title;
                problemDetails.Type ??= clientErrorData.Link;
            }

            problemDetails.Instance ??= httpContext.Request.Path;
            problemDetails.Type ??= "about:blank";

            return AddTracingInfo(problemDetails, httpContext);
        }


        private static ProblemDetails AddTracingInfo(ProblemDetails problemDetails, HttpContext httpContext)
        {
            // ids collect in a straightforward manner because we haven't real world data 
            var spanId = Activity.Current.SpanId.ToString();
            if (!string.IsNullOrEmpty(spanId))
                problemDetails.AddSpanId(spanId);

            var parentId = Activity.Current.ParentId;
            if (!string.IsNullOrEmpty(parentId))
                problemDetails.AddParentId(parentId);

            var traceId = Activity.Current.RootId;
            if (!string.IsNullOrEmpty(traceId))
                problemDetails.AddTraceId(traceId);

            var requestId = httpContext.TraceIdentifier;
            if (!string.IsNullOrEmpty(requestId))
                problemDetails.AddRequestId(requestId);

            problemDetails = AddExtensions(problemDetails, Activity.Current.Baggage.Select(kvp => (kvp.Key, (object) kvp.Value)));
            
            return problemDetails;
        }
        
        
        private static ProblemDetails AddExtensions(ProblemDetails problemDetails, IEnumerable<(string, object)> extensions)
        {
            foreach (var (key, value) in extensions)
                problemDetails.Extensions.TryAdd(key, value);
            
            return problemDetails;
        }

        
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly ApiBehaviorOptions _options;
    }
}
