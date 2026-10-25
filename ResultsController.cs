using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResultsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/results/me
        [HttpGet("results/me")]
        [RoleAuthorize("Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var results = await _context.Results
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Event)
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Category)
                .Where(r => r.Enrolment!.ParticipantID == userId.Value)
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    EventName = r.Enrolment!.Event!.EventName,
                    CategoryName = r.Enrolment.Category!.CategoryName,
                    r.FinishTime,
                    r.Position,
                    r.Status,
                    r.RecordedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/events/{eventId}/results
        [HttpGet("events/{eventId}/results")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null) return NotFound("Event not found.");

            if (ev.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            var results = await _context.Results
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Participant)
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Category)
                .Where(r => r.Enrolment!.EventID == eventId)
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    ParticipantName = r.Enrolment!.Participant!.FullName,
                    CategoryName = r.Enrolment.Category!.CategoryName,
                    r.FinishTime,
                    r.Position,
                    r.Status,
                    r.RecordedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        // POST: api/events/{eventId}/results
        [HttpPost("events/{eventId}/results")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> CreateResult(
            int eventId, CreateResultRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null) return NotFound("Event not found.");

            if (ev.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e =>
                    e.EnrolmentID == request.EnrolmentID &&
                    e.EventID == eventId);

            if (enrolment == null)
                return BadRequest(
                    "The enrolment does not belong to this event.");

            var alreadyHasResult = await _context.Results
                .AnyAsync(r => r.EnrolmentID == request.EnrolmentID);

            if (alreadyHasResult)
                return Conflict("This enrolment already has a result.");

            if (!IsValidStatus(request.Status))
                return BadRequest(
                    "Status must be Finished, DidNotFinish or Disqualified.");

            if (request.Position.HasValue && request.Position.Value <= 0)
                return BadRequest("Position must be greater than zero.");

            var result = new Result
            {
                EnrolmentID = request.EnrolmentID,
                FinishTime = request.FinishTime,
                Position = request.Position,
                Status = request.Status,
                RecordedAt = DateTime.UtcNow
            };

            _context.Results.Add(result);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("The result could not be saved.");
            }

            return Ok(result);
        }

        // PUT: api/results/{id}
        [HttpPut("results/{id}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> UpdateResult(
            int id, UpdateResultRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var result = await _context.Results
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null) return NotFound();

            if (result.Enrolment?.Event?.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (!IsValidStatus(request.Status))
                return BadRequest(
                    "Status must be Finished, DidNotFinish or Disqualified.");

            if (request.Position.HasValue && request.Position.Value <= 0)
                return BadRequest("Position must be greater than zero.");

            result.FinishTime = request.FinishTime;
            result.Position = request.Position;
            result.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(result);
        }

        // DELETE: api/results/{id}
        [HttpDelete("results/{id}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var result = await _context.Results
                .Include(r => r.Enrolment!)
                    .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null) return NotFound();

            if (result.Enrolment?.Event?.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static bool IsValidStatus(string status)
        {
            return status == "Finished" ||
                   status == "DidNotFinish" ||
                   status == "Disqualified";
        }
    }

    public class CreateResultRequest
    {
        public int EnrolmentID { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = "Finished";
    }

    public class UpdateResultRequest
    {
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = "Finished";
    }
}