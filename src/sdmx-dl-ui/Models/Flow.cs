using System;
using System.Text.RegularExpressions;

namespace sdmx_dl_ui.Models
{
    public class Flow : ICloneable
    {
        private static Regex _regex = new Regex( @"(?<=\:)(.*?)(?=\()" );

        public string Ref { get; init; }
        public string Label { get; init; }

        public string InputRef
            => _regex.Match( Ref ).Value;

        public object Clone()
            => new Flow { Ref = Ref , Label = Label };
    }
}