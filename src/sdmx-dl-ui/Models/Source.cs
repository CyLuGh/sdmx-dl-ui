using System;

namespace sdmx_dl_ui.Models
{
    public class Source : IEquatable<Source>
    {
        public string Name { get; init; }
        public string Description { get; init; }
        public string Aliases { get; init; }
        public string Driver { get; init; }
        public string Dialect { get; init; }
        public string Endpoint { get; init; }
        public string Properties { get; init; }
        public string Website { get; init; }
        public string Monitor { get; init; }

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