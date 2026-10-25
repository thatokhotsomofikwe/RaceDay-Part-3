using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/events/{eventId}/categories
        [HttpGet("events/{eventId}/categories")]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventID == eventId);

            if (!eventExists)
                return NotFound("Event not found.");

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/events/{eventId}/categories
        [HttpPost("events/{eventId}/categories")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> CreateCategory(
            int eventId, Category category)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized();

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (existingEvent == null)
                return NotFound("Event not found.");

            if (existingEvent.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (category.DistanceKm <= 0 ||
                category.EntryFee < 0 ||
                category.MaxParticipants <= 0)
            {
                return BadRequest(
                    "Distance must be positive, entry fee cannot be negative, " +
                    "and maximum participants must be positive.");
            }

            category.CategoryID = 0;
            category.EventID = eventId;
            category.Event = null;

            _context.Categories.Add(category);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(
                    "A category with this name may already exist for this event.");
            }

            return CreatedAtAction(
                nameof(GetCategories),
                new { eventId = eventId },
                category);
        }

        // PUT: api/categories/{id}
        [HttpPut("categories/{id}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> UpdateCategory(
            int id, Category updatedCategory)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            if (category.Event == null ||
                category.Event.OrganiserID != userId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (updatedCategory.DistanceKm <= 0 ||
                updatedCategory.EntryFee < 0 ||
                updatedCategory.MaxParticipants <= 0)
            {
                return BadRequest(
                    "Distance must be positive, entry fee cannot be negative, " +
                    "and maximum participants must be positive.");
            }

            category.CategoryName = updatedCategory.CategoryName;
            category.DistanceKm = updatedCategory.DistanceKm;
            category.EntryFee = updatedCategory.EntryFee;
            category.MaxParticipants = updatedCategory.MaxParticipants;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(
                    "A category with this name may already exist for this event.");
            }

            return Ok(category);
        }

        // DELETE: api/categories/{id}
        [HttpDelete("categories/{id}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            if (category.Event == null ||
                category.Event.OrganiserID != userId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            _context.Categories.Remove(category);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(
                    "This category cannot be deleted because it may have enrolments or other related records.");
            }

            return NoContent();
        }
    }
}