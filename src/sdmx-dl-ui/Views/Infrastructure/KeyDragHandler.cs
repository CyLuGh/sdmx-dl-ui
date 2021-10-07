using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GongSolutions.Wpf.DragDrop;

namespace sdmx_dl_ui.Views.Infrastructure
{
    public class KeyDragHandler : IDragSource
    {
        public void StartDrag( IDragInfo dragInfo )
        {
            if ( dragInfo.VisualSource is TextBlock textBlock )
            {
                dragInfo.Data = textBlock.Text.Trim();
                dragInfo.DataFormat = DataFormats.GetDataFormat( DataFormats.Text );

                dragInfo.Effects = dragInfo.Data != null ? DragDropEffects.Copy | DragDropEffects.Move : DragDropEffects.None;

            }
        }

        public bool CanStartDrag( IDragInfo dragInfo )
            => true;

        public void Dropped( IDropInfo dropInfo )
        {

        }

        public void DragDropOperationFinished( DragDropEffects operationResult , IDragInfo dragInfo )
        {

        }

        public void DragCancelled()
        {

        }

        public bool TryCatchOccurredException( Exception exception )
        {
            // TODO
            return true;
        }
    }
}
