using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.ErrorHandling.Extensions
{
    public static class ProblemDetailsExtensions
    {
        public static void AddParentId(this ProblemDetails details, string parentId)
            => details.Extensions[nameof(parentId)] = parentId;


        public static void AddRequestId(this ProblemDetails details, string requestId)
            => details.Extensions[nameof(requestId)] = requestId;


        public static void AddSpanId(this ProblemDetails details, string spanId)
            => details.Extensions[nameof(spanId)] = spanId;


        public static void AddTraceId(this ProblemDetails details, string traceId)
            => details.Extensions[nameof(traceId)] = traceId;
    }
}
