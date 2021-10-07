using System;

namespace sdmx_dl_ui.Models
{
    public class DataSeries
    {
        public string Series { get; set; }
        public string ObsAttributes { get; set; }
        public DateTime ObsPeriod { get; set; }
        public double? ObsValue { get; set; }
    }
}