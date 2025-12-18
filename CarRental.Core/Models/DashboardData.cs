namespace CarRental.Core.Models
{
    public class DashboardData
    {
        public Client Client { get; set; } = null!;
        public int TotalLocations { get; set; }
        public int ActiveLocations { get; set; }
        public int CompletedLocations { get; set; }
        public List<Location> Locations { get; set; } = new List<Location>();
        public int PendingLocations { get; set; }
    }
}