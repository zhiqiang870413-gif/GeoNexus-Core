namespace GisProject.Models
{
    public class MonitorPoint
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "水利"; // 水利, 電力, 交通
        public string Status { get; set; } = "Normal"; // Normal, Warning, Danger
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime LastUpdate { get; set; }
        public double Value { get; set; } // 例如水位高度、電壓值
    }
}
