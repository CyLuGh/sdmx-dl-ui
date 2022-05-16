namespace sdmx_dl_engine.Models
{
    public class ChartSeries
    {
        public string Title { get; set; } = string.Empty;
        public ChartModel[] Values { get; set; } = Array.Empty<ChartModel>();

        public double[] Xs => Values.Select( x => x.DateTime.ToOADate() ).ToArray();
        public double[] Ys => Values.Select( x => x.Value ).ToArray();
    }
}