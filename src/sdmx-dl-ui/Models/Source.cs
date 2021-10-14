using System;

namespace sdmx_dl_ui.Models
{
    public class Source : IEquatable<Source>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Aliases { get; set; }
        public string Driver { get; set; }
        public string Dialect { get; set; }
        public string Endpoint { get; set; }
        public string Properties { get; set; }
        public string Website { get; set; }
        public string Monitor { get; set; }

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