using System.Text.Json.Serialization;

namespace IOT_project
{
    public class PeopleDetection
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public decimal ConfidenceScore { get; set; }
        public string DetectionStatus { get; set; } = string.Empty; // authorized, unauthorized, unknown
        public string ActionTaken { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime Timestamp { get; set; }
        public string CameraId { get; set; } = "CAM_001";

        // Navigation property
        public AuthorizedPerson? AuthorizedPerson { get; set; }
    }
}
