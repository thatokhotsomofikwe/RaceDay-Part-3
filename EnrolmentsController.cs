using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class EnrolmentsController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public EnrolmentsController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                var eventItem = await _apiService.GetAsync<JsonElement>(
                    $"api/events/{eventId}", cookie);

                var categories = await _apiService.GetAsync<List<CategoryOptionViewModel>>(
                    $"api/events/{eventId}/categories", cookie);

                var model = new EnrolmentCreateViewModel
                {
                    EventID = eventId,
                    EventName = eventItem.TryGetProperty("eventName", out var name)
                        ? name.GetString() ?? "Selected event"
                        : "Selected event",
                    Categories = categories ?? new List<CategoryOptionViewModel>()
                };

                if (!model.Categories.Any())
                {
                    ViewBag.Error = "There are no categories available for this event.";
                }

                return View(model);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Unable to load this event. Please try again.";
                return View(new EnrolmentCreateViewModel
                {
                    EventID = eventId,
                    EventName = "Selected event"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EnrolmentCreateViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (model.CategoryID <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryID),
                    "Please select a category.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                using var response = await _apiService.PostAsync(
                    $"api/events/{model.EventID}/enrolments",
                    new { CategoryID = model.CategoryID },
                    cookie);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "You have successfully enrolled in this event.";
                    return RedirectToAction("MyEnrolments");
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Enrolment failed. You may already be enrolled, the category may be full, or the event may not be open.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to contact the RaceDay API. Please try again.");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MyEnrolments()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                var enrolments = await _apiService.GetAsync<List<EnrolmentViewModel>>(
                    "api/enrolments/me", cookie);

                return View(enrolments ?? new List<EnrolmentViewModel>());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Unable to load your enrolments.";
                return View(new List<EnrolmentViewModel>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                using var response = await _apiService.DeleteAsync(
                    $"api/enrolments/{id}", cookie);

                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                    response.IsSuccessStatusCode
                        ? "Your enrolment has been cancelled."
                        : "Unable to cancel this enrolment.";

                return RedirectToAction("MyEnrolments");
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to contact the RaceDay API.";
                return RedirectToAction("MyEnrolments");
            }
        }
    }
}
