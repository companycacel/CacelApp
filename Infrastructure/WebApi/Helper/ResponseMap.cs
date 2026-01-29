using Core.Exceptions;
using Core.Shared.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace WebApi.Helper;

public class ResponseMap
{
    public static async Task<ApiResponse<T>> Mapping<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        if (response.IsSuccessStatusCode)
        {
            var successResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, options);
            return successResponse;
        }
        else
        {
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, options);
            throw new WebApiException(errorResponse.message, errorResponse.statusCode, errorResponse.error);
        }
    }

    public static async Task<ApiResponse<T>> MappingDirect<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        if (response.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<T>(content, options);
            return new ApiResponse<T> { status = 1, Data = data };
        }
        else
        {
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, options);
            throw new WebApiException(errorResponse.message, errorResponse.statusCode, errorResponse.error);
        }
    }

    public static async Task<byte[]> GetFile(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return content;
        }
        else
        {
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content);
            throw new WebApiException(errorResponse.message, errorResponse.statusCode, errorResponse.error);
        }
    }
}
