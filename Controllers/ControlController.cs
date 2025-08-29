using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOT_project.Controllers
{

    [Authorize]
    public class ControlController : Controller
    {
        private readonly IMqttService _mqttService;

        public ControlController(IMqttService mqttService)
        {
            _mqttService = mqttService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OpenDoor()
        {
            await _mqttService.PublishDoorControlAsync("OPEN");
            TempData["Message"] = "Door open command sent successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CloseDoor()
        {
            await _mqttService.PublishDoorControlAsync("CLOSE");
            TempData["Message"] = "Door close command sent successfully!";
            return RedirectToAction("Index");
        }
    }

}