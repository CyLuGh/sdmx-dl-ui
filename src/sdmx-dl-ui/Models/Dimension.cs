namespace sdmx_dl_ui.Models
{
    public class Dimension
    {
        public string Concept { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public bool Coded { get; set; }
        public int? Position { get; set; }
    }
}