using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;
using RouteModel = RaceDay.API.Models.Route;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class RoutesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoutesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/events/{eventId}/route
        [HttpGet("events/{eventId}/route")]
        public async Task<IActionResult> GetEventRoute(int eventId)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null) return NotFound("Event not found.");

            var route = await _context.Routes
                .FirstOrDefaultAsync(r => r.EventID == eventId);

            if (route == null) return NotFound("No route has been added.");

            return Ok(route);
        }

        // POST: api/events/{eventId}/route
        [HttpPost("events/{eventId}/route")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> CreateRoute(
            int eventId, RouteModel route)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null) return NotFound("Event not found.");

            if (ev.OrganiserID != userId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (string.IsNullOrWhiteSpace(route.RouteName))
                return BadRequest("Route name is required.");

            if (route.DistanceKm.HasValue && route.DistanceKm.Value <= 0)
                return BadRequest("Distance must be greater than zero.");

            var routeExists = await _context.Routes
                .AnyAsync(r => r.EventID == eventId);

            if (routeExists)
                return Conflict("This event already has a route.");

            route.RouteID = 0;
            route.EventID = eventId;
            route.Event = null;

            _context.Routes.Add(route);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("The route could not be saved.");
            }

            return Ok(route);
        }

        // PUT: api/routes/{id}
        [HttpPut("routes/{id}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> UpdateRoute(
            int id, RouteModel updatedRoute)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return Unauthorized();

            var route = await _context.Routes
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RouteID == id);

            if (route == null) return NotFound();

            if (route.Event == null ||
                route.Event.OrganiserID != userId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (string.IsNullOrWhiteSpace(updatedRoute.RouteName))
                return BadRequest("Route name is required.");

            if (updatedRoute.DistanceKm.HasValue &&
                updatedRoute.DistanceKm.Value <= 0)
            {
                return BadRequest("Distance must be greater than zero.");
            }

            route.RouteName = updatedRoute.RouteName.Trim();
            route.DistanceKm = updatedRoute.DistanceKm;
            route.RouteUrl = updatedRoute.RouteUrl;
            route.Description = updatedRoute.Description;

            await _context.SaveChangesAsync();

            return Ok(route);
        }
    }
}