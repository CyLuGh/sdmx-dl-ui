namespace sdmx_dl_engine.Models
{
    public class Dimension
    {
        public string Concept { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Coded { get; set; }
        public int? Position { get; set; }
    }
}