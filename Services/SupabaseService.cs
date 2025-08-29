// Services/ISupabaseService.cs
using IOT_project;
using IOT_project.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

public interface ISupabaseService
{
    Task<bool> AuthenticateUserAsync(string email, string password);

    // People Detection Methods
    Task<List<PeopleDetection>> GetRecentPeopleDetectionsAsync(int limit = 20);
    Task<List<AuthorizedPerson>> GetAuthorizedPeopleAsync();
    Task<PeopleDetectionStats> GetDetectionStatsAsync();
    Task<bool> LogPeopleDetectionAsync(PeopleDetection detection);
    Task LogLoginAttemptAsync(string email, bool success);

    Task<List<Alert>> GetAlertsAsync(int limit = 20);
    Task<List<History>> GetLoginHistoryAsync(int limit  = 50);

}



public class SupabaseService : ISupabaseService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;

    public SupabaseService(HttpClient httpClient, IOptions<SupabaseSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;

        // Set default headers for Supabase
        _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
    }

   
   

   

    public async Task<bool> AuthenticateUserAsync(string email, string password)
    {
        try
        {
            var loginData = new { email, password };
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.Url}/auth/v1/token?grant_type=password", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<PeopleDetection>> GetRecentPeopleDetectionsAsync(int limit = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_settings.Url}/rest/v1/people_detections?select=*,authorized_people(full_name,role)&order=timestamp.desc&limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PeopleDetection>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }) ?? new List<PeopleDetection>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching people detections: {ex.Message}");
        }
        return new List<PeopleDetection>();
    }

    public async Task<List<AuthorizedPerson>> GetAuthorizedPeopleAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_settings.Url}/rest/v1/authorized_people?is_active=eq.true&order=created_at.desc");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<AuthorizedPerson>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }) ?? new List<AuthorizedPerson>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching authorized people: {ex.Message}");
        }
        return new List<AuthorizedPerson>();
    }

    public async Task<PeopleDetectionStats> GetDetectionStatsAsync()
    {
        try
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

            // Get today's detections
            var todayResponse = await _httpClient.GetAsync($"{_settings.Url}/rest/v1/people_detections?timestamp=gte.{today}&timestamp=lt.{tomorrow}&select=detection_status");

            if (todayResponse.IsSuccessStatusCode)
            {
                var json = await todayResponse.Content.ReadAsStringAsync();
                var detections = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json) ?? new List<Dictionary<string, object>>();

                var stats = new PeopleDetectionStats
                {
                    TodayDetections = detections.Count,
                    AuthorizedAccess = detections.Count(d => d.ContainsKey("detection_status") && d["detection_status"].ToString() == "authorized"),
                    UnauthorizedAttempts = detections.Count(d => d.ContainsKey("detection_status") && d["detection_status"].ToString() == "unauthorized"),
                    SystemActive = true
                };

                return stats;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching detection stats: {ex.Message}");
        }

        return new PeopleDetectionStats { SystemActive = true };
    }


   

    public async Task<bool> LogPeopleDetectionAsync(PeopleDetection detection)
    {
        try
        {
            var json = JsonSerializer.Serialize(detection, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.Url}/rest/v1/people_detections", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging people detection: {ex.Message}");
            return false;
        }
    }


    public async Task<List<Alert>> GetAlertsAsync(int limit = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_settings.Url}/rest/v1/alerts?order=timestamp.desc&limit={limit}"
            );

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var alerts = JsonSerializer.Deserialize<List<Alert>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                return alerts ?? new List<Alert>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching alerts: {ex.Message}");
        }

        return new List<Alert>();
    }


    public async Task LogLoginAttemptAsync(string email, bool success)
    {
        var attempt = new
        {
            email = email,
            success = success
        };

        var json = JsonSerializer.Serialize(attempt);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await _httpClient.PostAsync($"{_settings.Url}/rest/v1/login_history",
            content);
    }




    public async Task<List<History>> GetLoginHistoryAsync(int limit  = 50)
    {
        var response = await _httpClient.GetAsync(
            $"{_settings.Url}/rest/v1/login_history?select=*&order=attempted_at.desc&limit={limit}");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var historyList = JsonSerializer.Deserialize<List<History>>(content, options);

        return historyList ?? new List<History>();
    }



}