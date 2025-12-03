namespace Grimoire.Api.Middleware;

using System.Reflection;
using System.Text.Json;
using Application.Dto.Common;
using Microsoft.AspNetCore.Mvc;

public class PaginationMiddleware(RequestDelegate next) {
	private static readonly JsonSerializerOptions _jsonOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public async Task InvokeAsync(HttpContext context) {
		var originalBodyStream = context.Response.Body;

		using var responseBody = new MemoryStream();
		context.Response.Body = responseBody;

		var hasPaginationParams = HasPaginationRequestDtoParameter(context);

		if (hasPaginationParams) {
			context.Items["PaginationRequest"] = ExtractPaginationRequest(context.Request);
			context.Items["UsePagination"] = true;
		}

		await next(context);

		context.Response.Body = originalBodyStream;

		if (hasPaginationParams && context.Response.StatusCode == 200 && IsGetRequest(context.Request)) {
			await ProcessPaginatedResponse(context, responseBody, originalBodyStream);
		}
		else {
			await CopyResponseBody(responseBody, originalBodyStream);
		}
	}

	private static bool HasPaginationRequestDtoParameter(HttpContext context) {
		var endpoint = context.GetEndpoint();
		if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>() is not
			{ } actionDescriptor) {
			return false;
		}

		var methodInfo = actionDescriptor.MethodInfo;
		var parameters = methodInfo.GetParameters();

		return parameters.Any(static param => {
			var paramType = param.ParameterType;
			var underlyingType = Nullable.GetUnderlyingType(paramType);
			var actualType = underlyingType ?? paramType;
			return actualType == typeof(PaginationRequestDto) &&
					param.GetCustomAttributes<FromQueryAttribute>().Any();
		});
	}

	private static PaginationRequestDto ExtractPaginationRequest(HttpRequest request) {
		var paginationRequest = new PaginationRequestDto();

		if (int.TryParse(request.Query["page"], out var page)) {
			paginationRequest.Page = page;
		}

		if (int.TryParse(request.Query["pageSize"], out var pageSize)) {
			paginationRequest.PageSize = pageSize;
		}

		if (request.Query.TryGetValue("sortBy", out var sortBy)) {
			paginationRequest.SortBy = sortBy;
		}

		if (bool.TryParse(request.Query["sortDescending"], out var sortDescending)) {
			paginationRequest.SortDescending = sortDescending;
		}

		paginationRequest.Validate();
		return paginationRequest;
	}

	private static bool IsGetRequest(HttpRequest request) =>
		request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase);

	private static async Task ProcessPaginatedResponse(HttpContext context, MemoryStream responseBody,
		Stream originalBodyStream) {
		responseBody.Seek(0, SeekOrigin.Begin);
		var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

		try {
			using var document = JsonDocument.Parse(responseContent);
			var root = document.RootElement;

			if (TryExtractEnumerableData(root, out var arrayElement, out var itemType)) {
				var paginationRequest = (PaginationRequestDto)context.Items["PaginationRequest"]!;
				var items = arrayElement.EnumerateArray().ToList();
				var totalCount = items.Count;

				// Check if client wants envelope format for backward compatibility
				var useEnvelope = context.Request.Query.ContainsKey("envelope") ||
								(context.Request.Query.ContainsKey("include") && context.Request.Query["include"]
									.ToString().Contains("pagination"));

				if (useEnvelope) {
					var paginatedResponse = CreateEnvelopeResponse(items, paginationRequest, totalCount);
					var paginatedJson = JsonSerializer.Serialize(paginatedResponse, _jsonOptions);
					await WriteResponse(originalBodyStream, paginatedJson, context.Response);
				}
				else {
					// Industry standard: Use headers + clean data response
					AddPaginationHeaders(context, paginationRequest, totalCount);
					var paginatedItems = ApplyPagination(items, paginationRequest);
					var cleanJson = JsonSerializer.Serialize(paginatedItems, _jsonOptions);
					await WriteResponse(originalBodyStream, cleanJson, context.Response);
				}

				return;
			}
		}
		catch (JsonException) {
			// If JSON parsing fails, fall back to original response
		}

		await CopyResponseBody(responseBody, originalBodyStream);
	}

	private static bool TryExtractEnumerableData(JsonElement root, out JsonElement arrayElement, out string itemType) {
		arrayElement = default;
		itemType = "";

		if (root.ValueKind == JsonValueKind.Array) {
			arrayElement = root;
			itemType = "object";
			return true;
		}

		if (root.ValueKind == JsonValueKind.Object) {
			foreach (var property in root.EnumerateObject()) {
				if (property.Value.ValueKind == JsonValueKind.Array) {
					arrayElement = property.Value;
					itemType = "object";
					return true;
				}
			}
		}

		return false;
	}

	private static void AddPaginationHeaders(HttpContext context, PaginationRequestDto paginationRequest,
		int totalCount) {
		var totalPages = (int)Math.Ceiling((double)totalCount / paginationRequest.PageSize);
		var baseUrl = GetBaseUrl(context);
		var currentPath = context.Request.Path.Value ?? "";
		var queryParams = BuildQueryParams(context.Request.Query, paginationRequest);

		// Add total count header
		context.Response.Headers["X-Total-Count"] = totalCount.ToString();
		context.Response.Headers["X-Page"] = paginationRequest.Page.ToString();
		context.Response.Headers["X-Per-Page"] = paginationRequest.PageSize.ToString();
		context.Response.Headers["X-Total-Pages"] = totalPages.ToString();

		// Build Link header following RFC 8288
		var linkHeaders = new List<string>();

		// First page
		if (paginationRequest.Page > 1) {
			var firstUrl = $"{baseUrl}{currentPath}?{BuildPagedQuery(queryParams, 1)}";
			linkHeaders.Add($"<{firstUrl}>; rel=\"first\"");
		}

		// Previous page
		if (paginationRequest.Page > 1) {
			var prevUrl = $"{baseUrl}{currentPath}?{BuildPagedQuery(queryParams, paginationRequest.Page - 1)}";
			linkHeaders.Add($"<{prevUrl}>; rel=\"prev\"");
		}

		// Next page
		if (paginationRequest.Page < totalPages) {
			var nextUrl = $"{baseUrl}{currentPath}?{BuildPagedQuery(queryParams, paginationRequest.Page + 1)}";
			linkHeaders.Add($"<{nextUrl}>; rel=\"next\"");
		}

		// Last page
		if (paginationRequest.Page < totalPages && totalPages > 1) {
			var lastUrl = $"{baseUrl}{currentPath}?{BuildPagedQuery(queryParams, totalPages)}";
			linkHeaders.Add($"<{lastUrl}>; rel=\"last\"");
		}

		if (linkHeaders.Count > 0) {
			context.Response.Headers.Link = string.Join(", ", linkHeaders);
		}
	}

	private static object CreateEnvelopeResponse(List<JsonElement> items, PaginationRequestDto paginationRequest,
		int totalCount) {
		var paginatedItems = ApplyPagination(items, paginationRequest);
		var totalPages = (int)Math.Ceiling((double)totalCount / paginationRequest.PageSize);

		return new {
			Items = paginatedItems,
			TotalCount = totalCount,
			paginationRequest.Page,
			paginationRequest.PageSize,
			TotalPages = totalPages,
			HasPreviousPage = paginationRequest.Page > 1,
			HasNextPage = paginationRequest.Page < totalPages
		};
	}

	private static IEnumerable<JsonElement> ApplyPagination(List<JsonElement> items,
		PaginationRequestDto paginationRequest) {
		var sortedItems = ApplySorting(items, paginationRequest.SortBy, paginationRequest.SortDescending);
		return sortedItems.Skip(paginationRequest.Skip).Take(paginationRequest.PageSize);
	}

	private static IEnumerable<JsonElement> ApplySorting(IEnumerable<JsonElement> items, string? sortBy,
		bool sortDescending) {
		if (string.IsNullOrEmpty(sortBy)) {
			return items;
		}

		var sortedItems = items.OrderBy<JsonElement, object?>(item => item.TryGetProperty(sortBy, out var property)
			? property.ValueKind switch {
				JsonValueKind.String => property.GetString(),
				JsonValueKind.Number => property.GetDecimal(),
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Undefined => null,
				JsonValueKind.Null => null,
				JsonValueKind.Object => property.EnumerateObject(),
				JsonValueKind.Array => property.EnumerateArray(),
				_ => item.ToString()
			}
			: null);

		return sortDescending? sortedItems.Reverse() : sortedItems;
	}

	private static async Task WriteResponse(Stream originalBodyStream, string content, HttpResponse response) {
		response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(content);
		response.ContentType = "application/json; charset=utf-8";
		await originalBodyStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(content));
	}

	private static async Task CopyResponseBody(MemoryStream responseBody, Stream originalBodyStream) {
		responseBody.Seek(0, SeekOrigin.Begin);
		await responseBody.CopyToAsync(originalBodyStream);
	}

	private static string GetBaseUrl(HttpContext context) {
		var request = context.Request;
		var scheme = request.Scheme;
		var host = request.Host.Value;
		return $"{scheme}://{host}";
	}

	private static Dictionary<string, string> BuildQueryParams(IQueryCollection query,
		PaginationRequestDto paginationRequest) {
		var queryParams = new Dictionary<string, string>();

		foreach (var param in query) {
			// Skip pagination parameters as they'll be added separately
			if (param.Key is "page" or "pageSize" or "sortBy" or "sortDescending" or "envelope" or "include") {
				continue;
			}

			queryParams[param.Key] = param.Value.ToString();
		}

		// Add current sorting parameters
		if (!string.IsNullOrEmpty(paginationRequest.SortBy)) {
			queryParams["sortBy"] = paginationRequest.SortBy;
		}

		if (paginationRequest.SortDescending) {
			queryParams["sortDescending"] = "true";
		}

		queryParams["pageSize"] = paginationRequest.PageSize.ToString();

		return queryParams;
	}

	private static string BuildPagedQuery(Dictionary<string, string> baseParams, int page) {
		var allParams = new Dictionary<string, string>(baseParams) { ["page"] = page.ToString() };

		return string.Join("&", allParams.Select(static kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
	}
}
