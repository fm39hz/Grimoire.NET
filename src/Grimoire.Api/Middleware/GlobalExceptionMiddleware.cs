namespace Grimoire.Api.Middleware;

using System.Text.Json;
using Domain.Exception;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;

public partial class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger) {
	public async Task InvokeAsync(HttpContext context) {
		try {
			await next(context);
			if (!context.Response.HasStarted) {
				switch (context.Response.StatusCode) {
					case 401:
						await HandleErrorAsync(context, 401, "Unauthorized",
							"Unauthorized. Please login or refresh your token.");
						break;
					case 403:
						await HandleErrorAsync(context, 403, "Forbidden",
							"Forbidden. You don't have permission to access this resource.");
						break;
					case 404:
						await HandleErrorAsync(context, 404, "Not Found", "Resource not found.");
						break;
				}
			}
		}
		catch (Exception ex) {
			LogUnhandledExceptionMessage(logger, ex, ex.Message);
			await HandleExceptionAsync(context, ex);
		}
	}

	private static async Task HandleExceptionAsync(HttpContext context, Exception exception) {
		var (statusCode, title) = exception switch {
			EntityNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
			ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"),
			UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
			KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
			InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
			_ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
		};

		var message = exception.Message;

		if (statusCode == 500 && !context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
			message = "An internal server error occurred. Please try again later.";
		}

		await HandleErrorAsync(context, statusCode, title, message);
	}

	private static async Task HandleErrorAsync(HttpContext context, int statusCode, string title, string message) {
		context.Response.ContentType = "application/problem+json";
		context.Response.StatusCode = statusCode;

		var problemDetails = new ProblemDetails {
			Type = $"https://tools.ietf.org/html/rfc9110#section-{GetRfcSection(statusCode)}",
			Title = title,
			Status = statusCode,
			Extensions = {
				["errors"] = new Dictionary<string, string[]> { { "message", [message] } },
				["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
			}
		};

		var json = JsonSerializer.Serialize(problemDetails, JsonConfiguration.JsonOptions);

		await context.Response.WriteAsync(json);
	}

	private static string GetRfcSection(int statusCode) {
		const string prefix = "15";
		return statusCode switch {
			426 => $"{prefix}.5.22",
			>= 400 and < 500 => $"{prefix}.5.{statusCode - 399}",
			>= 500 and < 600 => $"{prefix}.6.{statusCode - 499}",
			_ => prefix
		};
	}

	[LoggerMessage(LogLevel.Error, "Unhandled exception: {message}")]
	static partial void LogUnhandledExceptionMessage(ILogger<GlobalExceptionMiddleware> logger, Exception ex,
		string message);
}
