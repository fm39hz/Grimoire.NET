namespace Grimoire.Api.Handler;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler {
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken) {
		var rootException = exception.GetBaseException();

		var problemDetails = new ProblemDetails { Instance = httpContext.Request.Path };

		if (exception is DbUpdateException) {
			// Lỗi do dữ liệu (trùng key, thiếu trường, sai ràng buộc...)
			problemDetails.Status = StatusCodes.Status409Conflict;
			problemDetails.Title = "Database Conflict";
			problemDetails.Detail = rootException.Message;

			LogDatabaseErrorErrormessage(rootException.Message);
		}
		else {
			// Lỗi crash code (NullReference, Argument...)
			problemDetails.Status = StatusCodes.Status500InternalServerError;
			problemDetails.Title = "Internal Server Error";
			problemDetails.Detail = rootException.Message;

			// Với lỗi 500 thì NÊN log stack trace để dev còn fix
			LogSystemCrashErrormessage(exception, rootException.Message);
		}

		httpContext.Response.StatusCode = problemDetails.Status.Value;
		await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

		return true;
	}

	[LoggerMessage(LogLevel.Error, "Database Error: {errorMessage}")]
	partial void LogDatabaseErrorErrormessage(string errorMessage);

	[LoggerMessage(LogLevel.Error, "System Crash: {errorMessage}")]
	partial void LogSystemCrashErrormessage(Exception ex, string errorMessage);
}
