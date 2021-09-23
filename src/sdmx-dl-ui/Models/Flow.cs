using System.Text.RegularExpressions;

namespace sdmx_dl_ui.Models
{
    public class Flow
    {
        private static Regex _regex = new Regex( @"(?<=\:)(.*?)(?=\()" );

        public string Ref { get; init; }
        public string Label { get; init; }

        public string InputRef
            => _regex.Match( Ref ).Value;
    }
}