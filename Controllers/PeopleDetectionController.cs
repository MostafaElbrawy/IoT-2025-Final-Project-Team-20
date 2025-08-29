using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace IOT_project.Controllers
{
    [Authorize]
    public class PeopleDetectionController : Controller
    {
        private readonly ISupabaseService _supabaseService;

        public PeopleDetectionController(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new PeopleDetectionViewModel
            {
                RecentDetections = await _supabaseService.GetRecentPeopleDetectionsAsync(20),
                AuthorizedPeople = await _supabaseService.GetAuthorizedPeopleAsync(),
                Stats = await _supabaseService.GetDetectionStatsAsync()
            };
            return View(viewModel);

        }

        
    }
}
