using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_ui.Models;

namespace sdmx_dl_ui.ViewModels
{
    public class DimensionsOrderingViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }


        internal SourceCache<DimensionViewModel , (Source, Flow, string)> DimensionsCache { get; }
            = new SourceCache<DimensionViewModel , (Source, Flow, string)>( s => (s.Source, s.Flow, s.Concept) );

        [Reactive] public DimensionViewModel SelectedDimension { get; set; }

        private ReadOnlyObservableCollection<DimensionViewModel> _dimensions;
        public ReadOnlyObservableCollection<DimensionViewModel> Dimensions => _dimensions;

        public ReactiveCommand<DimensionViewModel , Unit> ForwardPositionCommand { get; private set; }
        public ReactiveCommand<DimensionViewModel , Unit> BackwardPositionCommand { get; private set; }

        public DimensionsOrderingViewModel()
        {
            Activator = new ViewModelActivator();

            InitializeCommands( this );

            this.WhenActivated( disposables =>
            {
                DimensionsCache.Connect()
                    .AutoRefresh( x => x.DesiredPosition )
                    .Filter( x => x.Type.Equals( "dimension" , StringComparison.InvariantCultureIgnoreCase ) )
                    .Sort( SortExpressionComparer<DimensionViewModel>.Ascending( o => o.DesiredPosition ) )
                    .ObserveOn( RxApp.MainThreadScheduler )
                    .Bind( out _dimensions )
                    .DisposeMany()
                    .Subscribe()
                    .DisposeWith( disposables );
            } );
        }

        private static void InitializeCommands( DimensionsOrderingViewModel @this )
        {
            var canForward = @this.WhenAnyValue( x => x.SelectedDimension )
                .CombineLatest( @this.DimensionsCache.Connect()
                    .AutoRefresh( x => x.DesiredPosition )
                    .DisposeMany() )
                .Select( x => x.First )
                .ObserveOn( RxApp.MainThreadScheduler )
                .Select( x => x != null && x.DesiredPosition > 1 );
            @this.ForwardPositionCommand = ReactiveCommand.Create( ( DimensionViewModel dim ) =>
                {
                    @this.Dimensions.First( x => x.DesiredPosition == dim.DesiredPosition - 1 )
                        .DesiredPosition++;
                    dim.DesiredPosition--;
                } , canForward );

            var canBackward = @this.WhenAnyValue( x => x.SelectedDimension )
                .CombineLatest( @this.DimensionsCache.Connect()
                    .AutoRefresh( x => x.DesiredPosition )
                    .DisposeMany() )
                .Select( x => x.First )
                .ObserveOn( RxApp.MainThreadScheduler )
                .Select( x => x != null && x.DesiredPosition < @this.Dimensions.Count );
            @this.BackwardPositionCommand = ReactiveCommand.Create( ( DimensionViewModel dim ) =>
                {
                    @this.Dimensions.First( x => x.DesiredPosition == dim.DesiredPosition + 1 )
                        .DesiredPosition--;
                    dim.DesiredPosition++;
                } , canBackward );
        }
    }
}
