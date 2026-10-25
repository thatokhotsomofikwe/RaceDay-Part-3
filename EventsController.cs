using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/events (Public)
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.Categories)
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/events/5 (Public)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
            {
                return NotFound();
            }

            return Ok(ev);
        }

        // POST: api/events (Organiser only)
        [HttpPost]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> CreateEvent(
            [FromBody] Event newEvent)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return Unauthorized();
            }

            newEvent.OrganiserID = userId.Value;
            newEvent.Status = "Upcoming";

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = newEvent.EventID },
                newEvent);
        }

        // PUT: api/events/5 (Organiser only)
        [HttpPut("{id}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] Event updatedEvent)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserID != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            ev.EventName = updatedEvent.EventName;
            ev.Description = updatedEvent.Description;
            ev.EventDate = updatedEvent.EventDate;
            ev.StartTime = updatedEvent.StartTime;
            ev.Location = updatedEvent.Location;
            ev.EventType = updatedEvent.EventType;

            await _context.SaveChangesAsync();

            return Ok(ev);
        }

        // DELETE: api/events/5 (Organiser only)
        [HttpDelete("{id}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            if (ev.OrganiserID != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("mine")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return Unauthorized();
            }

            var events = await _context.Events
                .Include(e => e.Categories)
                .Where(e => e.OrganiserID == userId.Value)
                .ToListAsync();

            return Ok(events);
        }
    }
}
