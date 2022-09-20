using System;
namespace ColoradoGrandLodgeMapParser
{
    public class LodgeModel
    {
        public int Number { get; set; }
        public string? Name { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? Website { get; set; }
        public string? MeetingSchedule { get; set; }
        public string? RecessInfo { get; set; }
        public decimal? GeoLat { get; set; }
        public decimal? GeoLong { get; set; }
    }
}

