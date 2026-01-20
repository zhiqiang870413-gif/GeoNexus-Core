namespace GisProject.Models
{
    public class Vendor
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Status { get; set; } = "success";
        public string Type { get; set; } = "";
    }
}