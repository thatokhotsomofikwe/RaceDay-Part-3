using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaceDay.MVC.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public CategoriesController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                var categories =
                    await _apiService.GetAsync<List<CategoryViewModel>>(
                        $"api/events/{eventId}/categories", cookie);

                ViewBag.EventID = eventId;

                return View(categories ?? new List<CategoryViewModel>());
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load categories. Please try again.";

                ViewBag.EventID = eventId;

                return View(new List<CategoryViewModel>());
            }
        }

        [HttpGet]
        public IActionResult Create(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(new CategoryViewModel
            {
                EventID = eventId,
                DistanceKm = 5,
                EntryFee = 0,
                MaxParticipants = 100
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
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

            var category = new
            {
                model.CategoryName,
                model.DistanceKm,
                model.EntryFee,
                model.MaxParticipants
            };

            try
            {
                using var response = await _apiService.PostAsync(
                    $"api/events/{model.EventID}/categories",
                    category,
                    cookie);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Category added successfully!";

                    return RedirectToAction(
                        nameof(Index),
                        new { eventId = model.EventID });
                }

                ModelState.AddModelError(
                    string.Empty,
                    $"The category could not be added. Status code: {(int)response.StatusCode}.");

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
    
// GET: Edit a category
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                var categories =
                    await _apiService.GetAsync<List<CategoryViewModel>>(
                        $"api/events/{eventId}/categories", cookie);

                var category = categories?.FirstOrDefault(
                    c => c.CategoryID == id);

                if (category == null)
                    return NotFound();

                return View(category);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to load the category. Please try again.";

                return RedirectToAction(nameof(Index), new { eventId });
            }
        }

        // POST: Save category changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                using var response = await _apiService.PutAsync(
                    $"api/categories/{model.CategoryID}",
                    model,
                    cookie);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Category updated successfully!";

                    return RedirectToAction(
                        nameof(Index),
                        new { eventId = model.EventID });
                }

                ModelState.AddModelError(
                    string.Empty,
                    $"Unable to update category. Status code: {(int)response.StatusCode}.");

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

        // POST: Delete a category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var cookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                using var response = await _apiService.DeleteAsync(
                    $"api/categories/{id}", cookie);

                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "Category deleted successfully!";
                else
                    TempData["Error"] =
                        $"Unable to delete category. Status code: {(int)response.StatusCode}. " +
                        "It may have related enrolments.";

            }
            catch (HttpRequestException)
            {
                TempData["Error"] =
                    "Unable to contact the RaceDay API. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { eventId });
        }
    }
}