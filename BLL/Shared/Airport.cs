namespace BLL.Shared
{
    public class Airport
    {
        public string iata { get; set; }
        public string name { get; set; }
        public string city { get; set; }
        public string city_iata { get; set; }        
        public string icao { get; set; }
        public string country { get; set; }
        public string country_iata { get; set; }
        public Location location { get; set; }
        public int rating { get; set; }
        public int hubs { get; set; }
        public string timezone_region_name { get; set; }
        public string type { get; set; }
    }
}
