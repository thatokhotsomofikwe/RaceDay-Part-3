using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly RaceDayApiService _api;

        public AuthController(RaceDayApiService api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var response = await _api.PostAsync(
                    "api/Auth/login",
                    new { model.Email, model.Password });

                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Login failed. Please check your email and password.");

                    return View(model);
                }

                var user = JsonSerializer.Deserialize<AuthResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (user == null ||
                    user.UserID <= 0 ||
                    (user.Role != "Organiser" &&
                     user.Role != "Participant"))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "The API returned an invalid login response.");

                    return View(model);
                }

                var apiCookie = _api.GetApiSessionCookie(response);

                if (string.IsNullOrWhiteSpace(apiCookie))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Login succeeded, but the API session cookie was not received. Check the API session configuration.");

                    return View(model);
                }

                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("ApiSessionCookie", apiCookie);

                if (user.Role == "Organiser")
                    return RedirectToAction("Dashboard", "Organiser");

                return RedirectToAction("Index", "Home");
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to contact the RaceDay API. Please try again.");

                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var response = await _api.PostAsync(
                    "api/Auth/register",
                    new
                    {
                        model.FullName,
                        model.Email,
                        model.Password,
                        model.Role,
                        model.PhoneNumber
                    });

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Registration failed. Check your details or try another email.");

                    return View(model);
                }

                TempData["Success"] =
                    "Registration successful. You can now log in.";

                return RedirectToAction(nameof(Login));
            }
            catch
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to contact the RaceDay API. Please try again.");

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var apiCookie = HttpContext.Session.GetString("ApiSessionCookie");

            try
            {
                using var response = await _api.PostAsync(
                    "api/Auth/logout",
                    new { },
                    apiCookie);
            }
            catch
            {
                // Local logout must still proceed if the API is unavailable.
            }

            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }
    }
}