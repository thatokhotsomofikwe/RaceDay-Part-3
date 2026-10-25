using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.Models;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public OrganiserController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");
            var dashboard = new OrganiserDashboardViewModel();

            try
            {
                var events = await _apiService.GetAsync<List<EventViewModel>>(
                    "api/events/mine", cookie)
                    ?? new List<EventViewModel>();

                dashboard.TotalEvents = events.Count;

                dashboard.UpcomingEvents = events.Count(e =>
                    e.EventDate.Date >= DateTime.Today);

                foreach (var ev in events)
                {
                    var enrolments =
                        await _apiService.GetAsync<
                            List<System.Text.Json.JsonElement>>(
                            $"api/events/{ev.EventID}/enrolments", cookie);

                    if (enrolments != null)
                    {
                        dashboard.TotalEnrolments += enrolments.Count(
                            enrolment =>
                                !enrolment.TryGetProperty(
                                    "status", out var status) ||
                                status.GetString() != "Cancelled");
                    }
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Some dashboard information could not be loaded. Please try again.";
            }

            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> ManageEvents()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                var events = await _apiService.GetAsync<List<EventViewModel>>(
                    "api/events/mine", cookie);

                return View(events ?? new List<EventViewModel>());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Unable to load events. Please try again.";
                return View(new List<EventViewModel>());
            }
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(new CreateEventViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(CreateEventViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            var newEvent = new
            {
                model.EventName,
                model.Description,
                model.EventDate,
                model.StartTime,
                model.Location,
                model.EventType
            };

            try
            {
                using var response = await _apiService.PostAsync(
                    "api/events", newEvent, cookie);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Event created successfully!";
                    return RedirectToAction(nameof(ManageEvents));
                }

                ModelState.AddModelError(
                    string.Empty,
                    $"The API could not create the event. Status code: {(int)response.StatusCode}.");

                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to contact the RaceDay API. Please try again.");

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                var ev = await _apiService.GetAsync<System.Text.Json.JsonElement>(
                    $"api/events/{id}", cookie);

                if (ev.ValueKind == System.Text.Json.JsonValueKind.Undefined ||
                    ev.ValueKind == System.Text.Json.JsonValueKind.Null)
                {
                    return NotFound();
                }

                var model = new EditEventViewModel
                {
                    EventID = ev.GetProperty("eventID").GetInt32(),
                    EventName = ev.GetProperty("eventName").GetString() ?? "",
                    Description = ev.TryGetProperty("description", out var description)
                        ? description.GetString()
                        : null,
                    EventDate = ev.GetProperty("eventDate").GetDateTime(),
                    StartTime = TimeSpan.Parse(
                        ev.GetProperty("startTime").GetString() ?? "08:00:00"),
                    Location = ev.GetProperty("location").GetString() ?? "",
                    EventType = ev.GetProperty("eventType").GetString() ?? ""
                };

                return View(model);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to load the event. Please try again.";
                return RedirectToAction(nameof(ManageEvents));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EditEventViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            var updatedEvent = new
            {
                model.EventID,
                model.EventName,
                model.Description,
                model.EventDate,
                model.StartTime,
                model.Location,
                model.EventType
            };

            try
            {
                using var response = await _apiService.PutAsync(
                    $"api/events/{model.EventID}", updatedEvent, cookie);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Event updated successfully!";
                    return RedirectToAction(nameof(ManageEvents));
                }

                ModelState.AddModelError(
                    string.Empty,
                    $"The event could not be updated. Status code: {(int)response.StatusCode}.");

                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to contact the RaceDay API. Please try again.");

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                using var response = await _apiService.DeleteAsync(
                    $"api/events/{id}", cookie);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Event deleted successfully!";
                }
                else if (response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["Error"] =
                        "You cannot delete an event belonging to another organiser.";
                }
                else if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "The event could not be found.";
                }
                else
                {
                    TempData["Error"] =
                        $"The event could not be deleted. Status code: {(int)response.StatusCode}.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to contact the RaceDay API. Please try again.";
            }

            return RedirectToAction(nameof(ManageEvents));
        }

        [HttpGet]
        public async Task<IActionResult> EventParticipants(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                var participants =
                    await _apiService.GetAsync<List<EventParticipantViewModel>>(
                        $"api/events/{id}/enrolments", cookie);

                ViewBag.EventID = id;

                return View(participants ?? new List<EventParticipantViewModel>());
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load participants. Please try again.";

                return RedirectToAction(nameof(ManageEvents));
            }
        }
    }
}