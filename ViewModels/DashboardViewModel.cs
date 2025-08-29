
using IOT_project.Models;

namespace IOT_project;

public class DashboardViewModel
{
    public List<Alert> RecentAlerts { get; set; } = new();
    public List<PeopleDetection> RecentDetections { get; set; } = new();
    public List<History> RecentLoginsAttempts { get; set; } = new();
}
