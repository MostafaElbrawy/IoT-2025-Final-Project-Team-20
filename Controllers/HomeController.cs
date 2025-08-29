using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IOT_project;
public class HomeController : Controller
{
    private readonly ISupabaseService _supabaseService;

    public HomeController(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var viewModel = new DashboardViewModel
        {
            RecentAlerts = await _supabaseService.GetAlertsAsync(5),
            RecentDetections = await _supabaseService.GetRecentPeopleDetectionsAsync(5),
            RecentLoginsAttempts = await _supabaseService.GetLoginHistoryAsync(5)
            
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}









