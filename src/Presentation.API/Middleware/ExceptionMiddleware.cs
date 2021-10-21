using Domain.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Presentation.API.ViewModels;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Presentation.API.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> logger;
        private readonly IWebHostEnvironment env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            this.logger = logger;
            this.env = env;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await this._next(httpContext);
            }
            catch (Exception ex)
            {
                await this.HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            ErrorViewModel result;

            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Strict-Transport-Security", $"max-age={TimeSpan.FromDays(60)}");

            if (ex is ArgumentException
                 or AmountViolationException
                 or CreditCardDataValidationException
                 or PaymentStatusValidationException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                logger.LogWarning(ex.Message);

                result = new ErrorViewModel()
                {
                    Error = ex.Message
                };
            }
            else if (ex is NotFoundException) {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

                logger.LogWarning(ex.Message);

                result = new ErrorViewModel()
                {
                    Error = ex.Message
                };
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                logger.LogError(ex.Message);

                // For security reasons we might filter out 500 error messages since they can reveal flaws on our system
                result = new ErrorViewModel()
                {
                    Error = this.env.IsDevelopment() ? ex.Message : "An unexpected error occurred."
                };
            }

            return context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }));
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
