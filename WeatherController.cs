using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/events/{eventId}/weather")]
    public class WeatherController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherController(
            AppDbContext context,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: api/events/{eventId}/weather
        [HttpGet]
        public async Task<IActionResult> GetEventWeather(int eventId)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null)
                return NotFound("Event not found.");

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Find the event location's coordinates.
                var locationUrl =
                    "https://geocoding-api.open-meteo.com/v1/search?name=" +
                    Uri.EscapeDataString(ev.Location) +
                    "&count=1&language=en&format=json";

                var locationData =
                    await client.GetFromJsonAsync<JsonElement>(locationUrl);

                if (locationData.ValueKind != JsonValueKind.Object ||
                    !locationData.TryGetProperty("results", out var locations) ||
                    locations.GetArrayLength() == 0)
                {
                    return NotFound(
                        "Could not find weather coordinates for this event location.");
                }

                var location = locations[0];
                var latitude = location.GetProperty("latitude").GetDouble();
                var longitude = location.GetProperty("longitude").GetDouble();

                // Retrieve current weather for the coordinates.
                var weatherUrl =
                    "https://api.open-meteo.com/v1/forecast" +
                    $"?latitude={latitude}&longitude={longitude}" +
                    "&current=temperature_2m,relative_humidity_2m," +
                    "apparent_temperature,precipitation,weather_code," +
                    "wind_speed_10m&timezone=auto";

                var weatherData =
                    await client.GetFromJsonAsync<JsonElement>(weatherUrl);

                if (weatherData.ValueKind != JsonValueKind.Object ||
                    !weatherData.TryGetProperty("current", out var current))
                {
                    return StatusCode(
                        StatusCodes.Status502BadGateway,
                        "The weather service returned an unexpected response.");
                }

                return Ok(new
                {
                    ev.EventID,
                    ev.EventName,
                    ev.EventDate,
                    ev.Location,
                    Coordinates = new { latitude, longitude },
                    CurrentWeather = current
                });
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "The weather service could not be reached.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    "The weather service took too long to respond.");
            }
            catch (JsonException)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "The weather service returned invalid data.");
            }
        }
    }
}
