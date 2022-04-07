using System;

namespace sdmx_dl_engine.Models
{
    public class Source : IEquatable<Source>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Aliases { get; set; } = string.Empty;
        public string Driver { get; set; } = string.Empty;
        public string Dialect { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Properties { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Monitor { get; set; } = string.Empty;

        public bool Equals( Source other )
        {
            if ( other == null )
                return false;

            return Name.Equals( other.Name );
        }

        public override bool Equals( object obj )
            => Equals( obj as Source );

        public override int GetHashCode()
            => HashCode.Combine( Name );
    }
}