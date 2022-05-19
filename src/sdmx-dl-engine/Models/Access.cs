using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

namespace sdmx_dl_engine.Models
{
    public class Access
    {
        public string Source { get; set; } = string.Empty;

        [TypeConverter( typeof( YesNoConverter ) )]
        public bool Accessible { get; set; }

        public int? DurationInMillis { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class YesNoConverter : DefaultTypeConverter
    {
        public override object ConvertFromString( string text , IReaderRow row , MemberMapData memberMapData )
            => "YES".StartsWith( text , StringComparison.OrdinalIgnoreCase );

        public override string ConvertToString( object value , IWriterRow row , MemberMapData memberMapData )
            => value is bool b && b ? "YES" : "NO";
    }
}