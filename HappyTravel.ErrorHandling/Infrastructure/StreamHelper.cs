using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.ErrorHandling.Infrastructure
{
    internal class StreamHelper
    {
        internal static async Task CopyDetailsTo(Stream stream, ProblemDetails problemDetails)
        {
            var encodedJson = Serialize(problemDetails);

            await using var responseStream = new MemoryStream();
            await responseStream.WriteAsync(encodedJson);
            responseStream.Position = 0;
            
            await responseStream.CopyToAsync(stream);
        }
        
        
        internal static async Task WriteDetailsTo(Stream stream, ProblemDetails problemDetails)
        {
            var encodedJson = Serialize(problemDetails);
            await stream.WriteAsync(encodedJson);
        }

        
        internal static async Task RewriteDetailsTo(Stream stream, ProblemDetails problemDetails)
        {
            stream.SetLength(0);
            await WriteDetailsTo(stream, problemDetails);
        }


        private static ReadOnlyMemory<byte> Serialize(ProblemDetails problemDetails) 
            => JsonSerializer.SerializeToUtf8Bytes(problemDetails);
    }
}
