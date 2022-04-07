using System;

namespace sdmx_dl_engine.Models
{
    public class DataSeries
    {
        public string Series { get; set; } = string.Empty;
        public string ObsAttributes { get; set; } = string.Empty;
        public DateTime ObsPeriod { get; set; }
        public double? ObsValue { get; set; }
    }
}