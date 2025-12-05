namespace Grimoire.Api.Middleware;

using System.Text.Json;
using Domain.Exception;
using Infrastructure.Configuration;

public partial class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger) {
	public async Task InvokeAsync(HttpContext context) {
		try {
			await next(context);
			if (!context.Response.HasStarted) {
				switch (context.Response.StatusCode) {
					case 401:
						await HandleErrorAsync(context, 401, "Unauthorized. Please login or refresh your token.");
						break;
					case 403:
						await HandleErrorAsync(context, 403,
							"Forbidden. You don't have permission to access this resource.");
						break;
					case 404:
						await HandleErrorAsync(context, 404, "Resource not found.");
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
		var statusCode = exception switch {
			EntityNotFoundException => StatusCodes.Status404NotFound,
			ArgumentException => StatusCodes.Status400BadRequest,
			UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
			KeyNotFoundException => StatusCodes.Status404NotFound,
			InvalidOperationException => StatusCodes.Status409Conflict,
			_ => StatusCodes.Status500InternalServerError
		};

		var message = exception.Message;

		if (statusCode == 500 && !context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
			message = "An internal server error occurred. Please try again later.";
		}

		await HandleErrorAsync(context, statusCode, message);
	}

	private static async Task HandleErrorAsync(HttpContext context, int statusCode, string message) {
		context.Response.ContentType = "application/json";
		context.Response.StatusCode = statusCode;

		var response = new { succeed = false, statusCode, message, timestamp = DateTime.UtcNow };

		var json = JsonSerializer.Serialize(response, JsonConfiguration.JsonOptions);

		await context.Response.WriteAsync(json);
	}

	[LoggerMessage(LogLevel.Error, "Unhandled exception: {message}")]
	static partial void LogUnhandledExceptionMessage(ILogger<GlobalExceptionMiddleware> logger, Exception ex,
		string message);
}
