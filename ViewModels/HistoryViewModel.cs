namespace IOT_project { 
    public class HistoryViewModel
    {
        public string Email { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Status => Success ? "Success" : "Failure";
        public DateTime AttemptedAt { get; set; }
    }
}
