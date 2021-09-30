using sdmx_dl_ui.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdmx_dl_ui
{
    internal static class HierarchyBuilder
    {
        internal static HierarchicalCodeLabelViewModel[] Build( DimensionViewModel[] dimensions )
        {
            if ( dimensions.Length == 0 )
                return Array.Empty<HierarchicalCodeLabelViewModel>();

            var orderedDimensions = dimensions.OrderBy( x => x.DesiredPosition ).ToArray();
            var remaining = orderedDimensions.Skip( 1 ).ToArray();
            return orderedDimensions[0]
                .Values
                .Select( o =>
                {
                    var hclvm = new HierarchicalCodeLabelViewModel { Label = o.Label , Code = o.Code };
                    //hclvm.Children = Build( hclvm , string.Empty , remaining );
                    return hclvm;
                } )
                .ToArray();
        }

        private static HierarchicalCodeLabelViewModel[] Build( HierarchicalCodeLabelViewModel parent , string key , DimensionViewModel[] remainingDimensions )
        {
            if ( remainingDimensions.Length == 0 )
                return Array.Empty<HierarchicalCodeLabelViewModel>();

            var remaining = remainingDimensions.Skip( 1 ).ToArray();
            return remainingDimensions[0].Values
                        .Select( o =>
                        {
                            var hclvm = new HierarchicalCodeLabelViewModel { Label = o.Label };
                            // hclvm.Children = Build( hclvm , string.Empty , remaining );
                            return hclvm;
                        } )
                        .ToArray();
        }
    }
}