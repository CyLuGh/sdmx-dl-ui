using System;
using System.Drawing;

namespace sdmx_dl_ui.Views.Infrastructure
{
    internal static class ColorHelper
    {
        internal static Color MakeTransparent( this Color color , int alpha )
            => Color.FromArgb( alpha , color.R , color.G , color.B );

        internal static int PerceivedBrightness( this Color c )
            => (int) Math.Sqrt(
                ( c.R * c.R * .299 ) +
                ( c.G * c.G * .587 ) +
                ( c.B * c.B * .114 ) );

        internal static Color FindForegroundColor( this Color color )
            => color.PerceivedBrightness() > 130 ? Color.Black : Color.White;
    }
}