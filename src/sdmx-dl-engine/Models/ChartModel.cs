using System;

namespace sdmx_dl_engine.Models
{
    public class ChartModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }

        public ChartModel( DateTime dateTime , double value )
        {
            DateTime = dateTime;
            Value = value;
        }
    }
}