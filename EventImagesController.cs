using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/events/{eventId:int}/image")]
    public class EventImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageService _blobStorage;

        public EventImagesController(
            AppDbContext context,
            BlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        // Upload an image (Organiser only)
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(
            int eventId,
            IFormFile image)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return Unauthorized(new
                {
                    message = "Organiser login is required."
                });
            }

            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (eventItem.OrganiserID != userId)
            {
                return Forbid();
            }

            if (image == null || image.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select an image."
                });
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    message = "Image size must be 5 MB or less."
                });
            }

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(
                image.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new
                {
                    message = "Only JPG, PNG, and WebP images are allowed."
                });
            }

            try
            {
                await using var stream = image.OpenReadStream();

                string imageUrl = await _blobStorage.UploadImageAsync(
                    stream,
                    image.FileName,
                    image.ContentType);

                eventItem.ImageUrl = imageUrl;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Event image uploaded successfully.",
                    imageUrl
                });
            }
            catch (Azure.RequestFailedException)
            {
                return StatusCode(502, new
                {
                    message = "Azure could not store the image. Check the storage configuration."
                });
            }
        }

        // Get a temporary viewing link for an event image
        [HttpGet]
        public async Task<IActionResult> GetImage(int eventId)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (string.IsNullOrWhiteSpace(eventItem.ImageUrl))
            {
                return NotFound(new
                {
                    message = "This event does not have an image."
                });
            }

            try
            {
                string temporaryImageUrl =
                    _blobStorage.GetReadImageUrl(eventItem.ImageUrl);

                return Ok(new
                {
                    imageUrl = temporaryImageUrl
                });
            }
            catch (ArgumentException)
            {
                return BadRequest(new
                {
                    message = "The stored image URL is invalid."
                });
            }
            catch (Azure.RequestFailedException)
            {
                return StatusCode(502, new
                {
                    message = "Unable to generate the image viewing link."
                });
            }
        }
    }
}