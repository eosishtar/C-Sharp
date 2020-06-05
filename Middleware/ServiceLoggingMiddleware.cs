using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using WebApi.Core.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

// The following must be added to the begining of the Statup.cs file, Configure method
// app.Use(async (context, next) => {
//     context.Request.EnableBuffering();
//     await next();
// });

namespace WebApi.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ServiceLoggingMiddleware
    {
        private readonly RequestDelegate next;

        public ServiceLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context, DBContext db, ILogger<ServiceLoggingMiddleware> logger)
        {
            logger.LogDebug("Pipeline Initializing");

            Exception processingException = null;

            try
            {
                string requestBody = "";

                // Only inspect the request body for POST as GET from Oracle Fusion sends Transfer-Encoding: chunked (maybe also compressed) requests and the context.Request.ContentLength is null and context.Request.Body.Length is always 0
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(
                        context.Request.Body,
                        encoding: Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        bufferSize: 1024,
                        leaveOpen: true))
                    {
                        requestBody = await reader.ReadToEndAsync();

                        // Reset the request body stream position so the next middleware can read it
                        context.Request.Body.Position = 0;
                    }
                }

                var serviceLog = new ServiceLog
                {
                    Url = context.Request.GetDisplayUrl(),
                    StartDateTime = DateTime.Now,
                    ServerName = Environment.MachineName,
                    UserName = "Anonymous",
                    Method = context.Request.Method,
                    RequestBody = requestBody,
                    RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                };

                // Save request info to database
                await db.ServiceLog.AddAsync(serviceLog);
                await db.SaveChangesAsync();

                var originalBodyStream = context.Response.Body;

                using (var responseBodyStream = new MemoryStream())
                {
                    context.Response.Body = responseBodyStream;

                    try
                    {
                        await next(context);
                    }
                    catch (Exception ex)
                    {
                        processingException = ex;
                    }

                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    string responseBody = "";

                    using (var responseReader = new StreamReader(responseBodyStream))
                    {
                        responseBody = await responseReader.ReadToEndAsync();
                    }

                    using (var responseWriter = new StreamWriter(originalBodyStream))
                    {
                        await responseWriter.WriteAsync(responseBody);
                    }

                    if (processingException != null)
                    {
                        responseBody += $"{Environment.NewLine}Error occurred: {processingException.ToString()}";
                    }

                    if (!string.IsNullOrWhiteSpace(context.User.Identity.Name))
                    {
                        serviceLog.UserName = context.User.Identity.Name;
                    }

                    serviceLog.ResponseBody = responseBody;
                    serviceLog.HttpStatusCode = context.Response.StatusCode;
                    serviceLog.EndDateTime = DateTime.Now;

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ServiceLogging MiddleWare");
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            if (processingException != null)
            {
                ExceptionDispatchInfo.Capture(processingException).Throw();
            }

            logger.LogDebug("Pipeline Completing");
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ServiceLoggingMiddleWareExtensions
    {
        public static IApplicationBuilder UseServiceLoggingMiddleWare(this IApplicationBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.UseMiddleware<ServiceLoggingMiddleware>();
        }
    }
}
