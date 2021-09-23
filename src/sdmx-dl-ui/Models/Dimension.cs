namespace sdmx_dl_ui.Models
{
    public class Dimension
    {
        public string Concept { get; init; }
        public string Label { get; init; }
        public string Type { get; init; }
        public bool Coded { get; init; }
        public int? Position { get; init; }
    }
}