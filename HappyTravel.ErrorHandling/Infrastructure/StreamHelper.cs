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
            ReadOnlyMemory<byte> encodedJson = JsonSerializer.SerializeToUtf8Bytes(problemDetails);

            await using var responseStream = new MemoryStream();
            await responseStream.WriteAsync(encodedJson);
            responseStream.Position = 0;
            
            await responseStream.CopyToAsync(stream);
        }
    }
}
