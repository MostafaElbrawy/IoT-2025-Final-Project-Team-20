using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOT_project.Controllers
{

    [Authorize]
    public class HistoryController : Controller
    {
        private readonly ISupabaseService _supabaseService;

        public HistoryController(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch history list from Supabase service
            var historyList = await _supabaseService.GetLoginHistoryAsync();

            // Map to ViewModel
            var viewModel = historyList.Select(h => new HistoryViewModel
            {
                Email = h.Email,
                Success = h.Success,
                AttemptedAt = h.AttemptedAt,
            }).ToList();

            return View(viewModel);
        }
    }
}
