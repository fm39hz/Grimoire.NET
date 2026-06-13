namespace Grimoire.Api.Middleware;

using System.Diagnostics;
using System.Text.Json;
using Constant;
using Domain.Exception;
using EntityFramework.Exceptions.Common;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;

public partial class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger) {
	public async Task InvokeAsync(HttpContext context) {
		try {
			await next(context);
			if (!context.Response.HasStarted) {
				switch (context.Response.StatusCode) {
					case StatusCodes.Status401Unauthorized:
						await HandleErrorAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized",
							"Unauthorized. Please login or refresh your token.");
						break;
					case StatusCodes.Status403Forbidden:
						await HandleErrorAsync(context, StatusCodes.Status403Forbidden, "Forbidden",
							"Forbidden. You don't have permission to access this resource.");
						break;
					case StatusCodes.Status404NotFound:
						await HandleErrorAsync(context, StatusCodes.Status404NotFound, "Not Found", "Resource not found.");
						break;
					default:
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
			UnsupportedOperationException => (StatusCodes.Status501NotImplemented, "Not Implemented"),
			UniqueConstraintException => (StatusCodes.Status409Conflict, "Duplicate Key Error"),
			ReferenceConstraintException => (StatusCodes.Status409Conflict, "Foreign Key Violation"),
			CannotInsertNullException => (StatusCodes.Status400BadRequest, "Null Value Violation"),
			MaxLengthExceededException => (StatusCodes.Status400BadRequest, "String Too Long"),
			NumericOverflowException => (StatusCodes.Status400BadRequest, "Numeric Overflow Error"),
			_ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
		};

		var message = exception.Message;

		if (statusCode == StatusCodes.Status500InternalServerError && !context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
			message = "An internal server error occurred. Please try again later.";
		}

		await HandleErrorAsync(context, statusCode, title, message);
	}

	private static async Task HandleErrorAsync(HttpContext context, int statusCode, string title, string message) {
		context.Response.ContentType = ContentTypes.ProblemJson;
		context.Response.StatusCode = statusCode;

		var problemDetails = new ProblemDetails {
			Type = $"https://tools.ietf.org/html/rfc9110#section-{GetRfcSection(statusCode)}",
			Title = title,
			Status = statusCode,
			Extensions = {
				[ProblemDetailsKeys.Errors] = new Dictionary<string, string[]> { { "message", [message] } },
				[ProblemDetailsKeys.TraceId] = Activity.Current?.Id ?? context.TraceIdentifier
			}
		};

		var json = JsonSerializer.Serialize(problemDetails, JsonConfiguration.JsonOptions);

		await context.Response.WriteAsync(json);
	}

	private static string GetRfcSection(int statusCode) {
		const string rfcPrefix = "15";
		return statusCode switch {
			StatusCodes.Status426UpgradeRequired => $"{rfcPrefix}.5.22",
			>= StatusCodes.Status400BadRequest and < StatusCodes.Status500InternalServerError =>
				$"{rfcPrefix}.5.{statusCode - 399}",
			>= StatusCodes.Status500InternalServerError and < 600 =>
				$"{rfcPrefix}.6.{statusCode - 499}",
			_ => rfcPrefix
		};
	}

	[LoggerMessage(LogLevel.Error, "Unhandled exception: {message}")]
	static partial void LogUnhandledExceptionMessage(ILogger<GlobalExceptionMiddleware> logger, Exception ex,
		string message);
}
