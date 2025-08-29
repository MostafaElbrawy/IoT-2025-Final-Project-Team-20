
namespace IOT_project
{
    public class PeopleDetectionViewModel
    {
        public List<PeopleDetection> RecentDetections { get; set; } = new();
        public List<AuthorizedPerson> AuthorizedPeople { get; set; } = new();
        public PeopleDetectionStats Stats { get; set; } = new();
    }
}
