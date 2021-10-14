using System;
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

    public class DimensionChooser : IEquatable<DimensionChooser>
    {
        public int Position { get; set; }
        public string Description { get; set; }

        public bool Equals( DimensionChooser other )
        {
            if ( other is null )
                return false;

            return Position.Equals( other.Position ) && Description.Equals( other.Description );
        }

        public override int GetHashCode()
            => HashCode.Combine( Position , Description );

        public override bool Equals( object obj )
            => Equals( obj as DimensionChooser );
    }
}