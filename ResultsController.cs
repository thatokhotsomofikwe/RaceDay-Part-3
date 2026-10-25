using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RaceDay.MVC.Controllers
{
    public class ResultsController : Controller
    {
        private readonly RaceDayApiService _api;

        public ResultsController(RaceDayApiService api)
        {
            _api = api;
        }

        // Participant: view personal results
        public async Task<IActionResult> MyResults()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var results = await _api.GetAsync<List<ResultViewModel>>(
                    "api/results/me");

                return View(results ?? new List<ResultViewModel>());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Unable to load your results. Please check that the API is running.";

                return View(new List<ResultViewModel>());
            }
        }

        // Organiser: view results and eligible enrolments for an event
        public async Task<IActionResult> Manage(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (eventId <= 0)
            {
                return RedirectToAction("ManageEvents", "Organiser");
            }

            try
            {
                var results = await _api.GetAsync<List<ResultViewModel>>(
                    $"api/events/{eventId}/results");

                var enrolments = await _api.GetAsync<List<EventEnrolmentViewModel>>(
                    $"api/events/{eventId}/enrolments");

                ViewBag.EventID = eventId;
                ViewBag.Enrolments = enrolments ?? new List<EventEnrolmentViewModel>();

                return View(results ?? new List<ResultViewModel>());
            }
            catch (HttpRequestException)
            {
                ViewBag.EventID = eventId;
                ViewBag.Enrolments = new List<EventEnrolmentViewModel>();
                ViewBag.Error =
                    "Unable to load results or enrolments. Check the API connection and your access to this event.";

                return View(new List<ResultViewModel>());
            }
        }

        // Organiser: record a result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int eventId,
            int enrolmentId,
            string? finishTime,
            int? position,
            string status)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            TimeSpan? parsedTime = null;

            if (!string.IsNullOrWhiteSpace(finishTime))
            {
                if (!TimeSpan.TryParse(finishTime, out var time) ||
                    time < TimeSpan.Zero)
                {
                    TempData["Error"] = "Please enter a valid finishing time.";
                    return RedirectToAction(nameof(Manage), new { eventId });
                }

                parsedTime = time;
            }

            var request = new
            {
                EnrolmentID = enrolmentId,
                FinishTime = parsedTime,
                Position = position,
                Status = status
            };

            try
            {
                using var response = await _api.PostAsync(
                    $"api/events/{eventId}/results", request,
                    HttpContext.Session.GetString("ApiSessionCookie"));

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] =
                        "Could not record the result. The enrolment may be invalid or already have a result.";
                }
                else
                {
                    TempData["Success"] = "Result recorded successfully.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the RaceDay API.";
            }

            return RedirectToAction(nameof(Manage), new { eventId });
        }

        // Organiser: update an existing result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            int eventId,
            int resultId,
            string? finishTime,
            int? position,
            string status)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            TimeSpan? parsedTime = null;

            if (!string.IsNullOrWhiteSpace(finishTime))
            {
                if (!TimeSpan.TryParse(finishTime, out var time) ||
                    time < TimeSpan.Zero)
                {
                    TempData["Error"] = "Please enter a valid finishing time.";
                    return RedirectToAction(nameof(Manage), new { eventId });
                }

                parsedTime = time;
            }

            var request = new
            {
                FinishTime = parsedTime,
                Position = position,
                Status = status
            };

            try
            {
                using var response = await _api.PutAsync(
                    $"api/results/{resultId}", request,
                    HttpContext.Session.GetString("ApiSessionCookie"));

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Could not update the result.";
                }
                else
                {
                    TempData["Success"] = "Result updated successfully.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the RaceDay API.";
            }

            return RedirectToAction(nameof(Manage), new { eventId });
        }

        // Organiser: delete a result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int eventId, int resultId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                using var response = await _api.DeleteAsync(
                    $"api/results/{resultId}",
                    HttpContext.Session.GetString("ApiSessionCookie"));

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Could not delete the result.";
                }
                else
                {
                    TempData["Success"] = "Result deleted successfully.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Unable to connect to the RaceDay API.";
            }

            return RedirectToAction(nameof(Manage), new { eventId });
        }
    }
}