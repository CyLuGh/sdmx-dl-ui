namespace sdmx_dl_ui.ViewModels
{
    public class CodeLabelInfo
    {
        public int Position { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public string Label { get; set; }

        public CodeLabelInfo()
        {
        }

        public CodeLabelInfo( int position , string description , string code , string label )
        {
            this.Position = position;
            this.Description = description;
            this.Code = code;
            this.Label = label;
        }
    }

    public class DimensionChooser
    {
        public int Position { get; set; }
        public string Description { get; set; }
    }
}