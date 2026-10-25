using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers
{
    public class ProfileController : Controller
    {
        private readonly RaceDayApiService _api;

        public ProfileController(RaceDayApiService api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Auth");

            var apiCookie = HttpContext.Session.GetString("ApiSessionCookie");

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                TempData["Error"] =
                    "Your API session has expired. Please log in again.";

                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var profile = await _api.GetAsync<ProfileViewModel>(
                    "api/users/me", apiCookie);

                if (profile == null)
                {
                    TempData["Error"] = "Your profile could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(profile);
            }
            catch
            {
                TempData["Error"] =
                    "Unable to load your profile. Please log in again.";

                return RedirectToAction("Login", "Auth");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Auth");

            var apiCookie = HttpContext.Session.GetString("ApiSessionCookie");

            if (string.IsNullOrWhiteSpace(apiCookie))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _api.PutAsync(
                    "api/users/me",
                    new
                    {
                        model.FullName,
                        model.PhoneNumber
                    },
                    apiCookie);

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "Unable to update your profile. Please try again.");

                        return View(model);
                    }
                }

                HttpContext.Session.SetString("FullName", model.FullName);

                TempData["Success"] = "Your profile was updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to contact the API. Please try again.");

                return View(model);
            }
        }
    }
}