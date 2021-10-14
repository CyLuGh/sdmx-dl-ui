using System;
using System.Text.RegularExpressions;

namespace sdmx_dl_ui.Models
{
    public class Flow : ICloneable
    {
        private static readonly Regex _regex = new( @"(?<=\:)(.*?)(?=\()" );

        public string Ref { get; set; }
        public string Label { get; set; }

        public string InputRef
            => _regex.Match( Ref ).Value;

        public object Clone()
            => new Flow { Ref = Ref , Label = Label };
    }
}