using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using RaceDay.MVC.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace RaceDay.MVC.Controllers
{
    public class EventsController : Controller
    {
        private readonly RaceDayApiService _api;

        public EventsController(RaceDayApiService api)
        {
            _api = api;
        }

        // Browse all events
        public async Task<IActionResult> Index()
        {
            try
            {
                var events = await _api.GetAsync<List<EventViewModel>>(
                    "api/events");

                events ??= new List<EventViewModel>();

                foreach (var ev in events)
                {
                    ev.ImageUrl = await _api.GetEventImageUrlAsync(
                        ev.EventID);
                }

                return View(events);
            }
            catch
            {
                ViewBag.Error = "Unable to load events. Please check that the RaceDay API is running.";
                return View(new List<EventViewModel>());
            }
        }

        // Display details for one event
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var eventDetails = await _api.GetAsync<EventViewModel>(
                    $"api/events/{id}");

                if (eventDetails == null)
                {
                    return NotFound();
                }

                eventDetails.ImageUrl =
                    await _api.GetEventImageUrlAsync(id);

                var categories = await _api.GetAsync<List<CategoryViewModel>>(
                    $"api/events/{id}/categories");

                var model = new EventDetailsViewModel
                {
                    Event = eventDetails,
                    Categories = categories ?? new List<CategoryViewModel>()
                };

                return View(model);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Unable to load event details. Please check that the RaceDay API is running.";
                return View("DetailsError");
            }
        }
    }

    // Event information used by the Browse Events and Details pages
    public class EventViewModel
    {
        public int EventID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public double Distance { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }

    // Combines event information and its categories for the Details page
    public class EventDetailsViewModel
    {
        public EventViewModel Event { get; set; } = new EventViewModel();

        public List<CategoryViewModel> Categories { get; set; }
            = new List<CategoryViewModel>();
    }
}
