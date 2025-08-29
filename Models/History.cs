using System.Text.Json.Serialization;
namespace IOT_project.Models
{


    using System.Text.Json.Serialization;

    public class History
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("attempted_at")]
        public DateTime AttemptedAt { get; set; }
    }


}
