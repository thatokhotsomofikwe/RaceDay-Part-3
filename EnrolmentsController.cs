using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnrolmentsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/events/{eventId}/enrolments
        [HttpPost("events/{eventId}/enrolments")]
        [RoleAuthorize("Participant")]
        public async Task<IActionResult> CreateEnrolment(
            int eventId, CreateEnrolmentRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null) return NotFound("Event not found.");

            if (ev.Status != "Open")
                return BadRequest("This event is not open for enrolment.");

            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryID == request.CategoryID &&
                    c.EventID == eventId);

            if (category == null)
                return BadRequest("The category does not belong to this event.");

            var existing = await _context.Enrolments
                .AnyAsync(e => e.EventID == eventId &&
                               e.ParticipantID == userId.Value);

            if (existing)
                return Conflict("You are already enrolled in this event.");

            var count = await _context.Enrolments
                .CountAsync(e => e.CategoryID == category.CategoryID &&
                                 e.Status == "Confirmed");

            if (count >= category.MaxParticipants)
                return Conflict("This category is full.");

            var enrolment = new Enrolment
            {
                EventID = eventId,
                CategoryID = category.CategoryID,
                ParticipantID = userId.Value,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("The enrolment could not be created. Check whether you are already enrolled.");
            }

            return Ok(enrolment);
        }

        // GET: api/enrolments/me
        [HttpGet("enrolments/me")]
        [RoleAuthorize("Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantID == userId.Value)
                .Include(e => e.Event)
                .Include(e => e.Category)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.EventID,
                    EventName = e.Event!.EventName,
                    e.CategoryID,
                    CategoryName = e.Category!.CategoryName,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // DELETE: api/enrolments/{id}
        [HttpDelete("enrolments/{id}")]
        [RoleAuthorize("Participant")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e =>
                    e.EnrolmentID == id &&
                    e.ParticipantID == userId.Value);

            if (enrolment == null) return NotFound();

            if (enrolment.Status == "Cancelled")
                return BadRequest("This enrolment is already cancelled.");

            enrolment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/events/{eventId}/enrolments
        [HttpGet("events/{eventId}/enrolments")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null) return NotFound("Event not found.");

            if (ev.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            var enrolments = await _context.Enrolments
                .Where(e => e.EventID == eventId)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.ParticipantID,
                    ParticipantName = e.Participant!.FullName,
                    e.CategoryID,
                    CategoryName = e.Category!.CategoryName,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }
    }

    public class CreateEnrolmentRequest
    {
        public int CategoryID { get; set; }
    }
}
