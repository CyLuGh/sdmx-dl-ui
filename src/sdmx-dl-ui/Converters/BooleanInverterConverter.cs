using System;
using System.Windows.Data;

namespace sdmx_dl_ui.Converters
{
    public class BooleanInverterConverter : IValueConverter
    {
        public object Convert( object value , Type targetType , object parameter , System.Globalization.CultureInfo culture )
        {
            if ( value is bool x )
            {
                return !x;
            }
            return value;
        }

        public object ConvertBack( object value , Type targetType , object parameter , System.Globalization.CultureInfo culture )
        {
            if ( value is bool x )
            {
                return !x;
            }
            return value;
        }
    }
}
