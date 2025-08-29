using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOT_project.Controllers
{

    //[Authorize]
    public class AlertController : Controller
    {
        private readonly ISupabaseService _supabaseService;

        public AlertController(ISupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<IActionResult> Index()
        {
            var alertsModel = new AlertsModelView
            {
                Alerts = await _supabaseService.GetAlertsAsync()
            };
            return View(alertsModel);
        }
    }




}