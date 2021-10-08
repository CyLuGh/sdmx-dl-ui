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
        internal static HierarchicalCodeLabelViewModel[] Build( IEnumerable<DimensionViewModel> dimensions , string[][] keysOccurrences )
        {
            if ( !dimensions.Any() )
                return Array.Empty<HierarchicalCodeLabelViewModel>();

            var key = string.Join( "." , Enumerable.Range( 0 , dimensions.Count() )
                .Select( _ => string.Empty ) );

            return Build( key , 1 , dimensions , keysOccurrences );
        }

        internal static HierarchicalCodeLabelViewModel[] Build( string key , int desiredPosition ,
            IEnumerable<DimensionViewModel> dimensions , string[][] keysOccurrences )
        {
            var dimensionViewModels = dimensions as DimensionViewModel[] ?? dimensions.ToArray();

            if ( desiredPosition > dimensionViewModels.Length )
                return Array.Empty<HierarchicalCodeLabelViewModel>();

            var orderedDimensions = dimensionViewModels.OrderBy( x => x.DesiredPosition ).ToArray();
            var dim = orderedDimensions[desiredPosition - 1];
            return dim
                .Values
                .Where( o => keysOccurrences.Length == 0 || keysOccurrences[dim.Position - 1].Length == 0 || keysOccurrences[dim.Position - 1].Contains( o.Code ) )
                .OrderBy( o => o.Code )
                .Select( o =>
                {
                    var splits = key.Split( '.' );
                    splits[desiredPosition - 1] = o.Code;
                    return new HierarchicalCodeLabelViewModel( desiredPosition != dimensionViewModels.Length )
                    {
                        Label = o.Label ,
                        Code = string.Join( "." , splits ) ,
                        Position = desiredPosition
                    };
                } )
                .ToArray();
        }
    }
}