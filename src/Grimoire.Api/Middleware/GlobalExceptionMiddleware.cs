namespace Grimoire.Api.Middleware;

using System.Text.Json;
using Domain.Exception;

public class GlobalExceptionMiddleware {
	private readonly RequestDelegate _next;
	private readonly ILogger<GlobalExceptionMiddleware> _logger;
	private static readonly JsonSerializerOptions JsonOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger) {
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context) {
		try {
			await _next(context);

			// Handle 401/403 responses with empty body
			if (!context.Response.HasStarted) {
				if (context.Response.StatusCode == 401) {
					await HandleErrorAsync(context, 401, "Unauthorized. Please login or refresh your token.");
				}
				else if (context.Response.StatusCode == 403) {
					await HandleErrorAsync(context, 403, "Forbidden. You don't have permission to access this resource.");
				}
			}
		}
		catch (System.Exception ex) {
			_logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
			await HandleExceptionAsync(context, ex);
		}
	}

	private static async Task HandleExceptionAsync(HttpContext context, System.Exception exception) {
		var statusCode = exception switch {
			EntityNotFoundException => StatusCodes.Status404NotFound,
			ArgumentException => StatusCodes.Status400BadRequest,
			UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
			KeyNotFoundException => StatusCodes.Status404NotFound,
			InvalidOperationException => StatusCodes.Status409Conflict,
			_ => StatusCodes.Status500InternalServerError
		};

		var message = exception.Message;

		// Hide internal error details in production
		if (statusCode == 500 && !context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) {
			message = "An internal server error occurred. Please try again later.";
		}

		await HandleErrorAsync(context, statusCode, message);
	}

	private static async Task HandleErrorAsync(HttpContext context, int statusCode, string message) {
		context.Response.ContentType = "application/json";
		context.Response.StatusCode = statusCode;

		var response = new {
			succeeded = false,
			statusCode = statusCode,
			message = message,
			timestamp = DateTime.UtcNow
		};

		var json = JsonSerializer.Serialize(response, JsonOptions);
		
		await context.Response.WriteAsync(json);
	}
}
