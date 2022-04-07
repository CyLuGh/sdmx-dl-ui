using System;
using System.Text.RegularExpressions;

namespace sdmx_dl_engine.Models
{
    public class Flow : ICloneable
    {
        private static readonly Regex _regex = new( @"(?<=\:)(.*?)(?=\()" );

        public string Ref { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        public string InputRef
            => _regex.Match( Ref ).Value;

        public object Clone()
            => new Flow { Ref = Ref , Label = Label };
    }
}