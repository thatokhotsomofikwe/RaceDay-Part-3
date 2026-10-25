using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RaceDay.MVC.Services
{
    public class RaceDayApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RaceDayApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient("RaceDayApi");
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string endpoint,
            object? data = null,
            string? apiSessionCookie = null)
        {
            var client = CreateClient();

            using var request = new HttpRequestMessage(method, endpoint);

            if (data != null)
            {
                request.Content = JsonContent.Create(data);
            }

            if (!string.IsNullOrWhiteSpace(apiSessionCookie))
            {
                request.Headers.TryAddWithoutValidation(
                    "Cookie", apiSessionCookie);
            }

            return await client.SendAsync(request);
        }

        public async Task<T?> GetAsync<T>(
            string endpoint,
            string? apiSessionCookie = null)
        {
            using var response = await SendAsync(
                HttpMethod.Get,
                endpoint,
                apiSessionCookie: apiSessionCookie);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<HttpResponseMessage> PostAsync<T>(
            string endpoint,
            T data,
            string? apiSessionCookie = null)
        {
            return await SendAsync(
                HttpMethod.Post, endpoint, data, apiSessionCookie);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(
            string endpoint,
            T data,
            string? apiSessionCookie = null)
        {
            return await SendAsync(
                HttpMethod.Put, endpoint, data, apiSessionCookie);
        }

        public async Task<HttpResponseMessage> DeleteAsync(
            string endpoint,
            string? apiSessionCookie = null)
        {
            return await SendAsync(
                HttpMethod.Delete,
                endpoint,
                apiSessionCookie: apiSessionCookie);
        }

        public async Task<string?> GetEventImageUrlAsync(int eventId)
        {
            using var response = await SendAsync(
                HttpMethod.Get,
                $"api/events/{eventId}/image");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content
                .ReadFromJsonAsync<EventImageResponse>();

            return result?.ImageUrl;
        }

        private class EventImageResponse
        {
            public string? ImageUrl { get; set; }
        }

        public string? GetApiSessionCookie(
            HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(
                "Set-Cookie", out var cookieHeaders))
            {
                return null;
            }

            foreach (var cookieHeader in cookieHeaders)
            {
                const string cookieName = ".AspNetCore.Session=";

                if (cookieHeader.StartsWith(
                    cookieName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    var semicolonIndex = cookieHeader.IndexOf(';');

                    return semicolonIndex >= 0
                        ? cookieHeader[..semicolonIndex]
                        : cookieHeader;
                }
            }

            return null;
        }
    }
}