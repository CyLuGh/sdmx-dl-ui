using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DynamicData;
using GongSolutions.Wpf.DragDrop;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace sdmx_dl_ui.ViewModels
{
    public class MainDisplayViewModel : ReactiveObject, IDropTarget
    {
        internal SourceCache<SeriesDisplayViewModel , string> SeriesCache { get; }
            = new SourceCache<SeriesDisplayViewModel , string>( s => s.Key );

        public bool HasItems { [ObservableAsProperty] get; }

        internal Interaction<SeriesDisplayViewModel , Unit> CreateTabItemInteraction { get; }
            = new Interaction<SeriesDisplayViewModel , Unit>( RxApp.MainThreadScheduler );

        public MainDisplayViewModel()
        {
            CreateTabItemInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );

            SeriesCache.Connect()
                .DisposeMany()
                .Select( _ => SeriesCache.Count > 0 )
                .ToPropertyEx( this , x => x.HasItems , scheduler: RxApp.MainThreadScheduler );

            SeriesCache.Connect()
                .DisposeMany()
                .SelectMany( c => c.Where( o => o.Reason == ChangeReason.Add ).Select( o => o.Current ) )
                .Do( async s => await CreateTabItemInteraction.Handle( s ) )
                .Subscribe();

        }

        public void DragOver( IDropInfo dropInfo )
        {
            if ( dropInfo.DragInfo.DataFormat == DataFormats.GetDataFormat( DataFormats.Text )
                 && !string.IsNullOrEmpty( dropInfo.Data as string ) )
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }
        }

        public void Drop( IDropInfo dropInfo )
        {
            var key = dropInfo.Data as string;
            var sdvm = new SeriesDisplayViewModel { Key = key };
            SeriesCache.AddOrUpdate( sdvm );
        }
    }
}
