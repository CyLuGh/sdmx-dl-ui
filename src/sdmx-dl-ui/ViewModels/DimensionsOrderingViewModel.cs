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
using Splat;

namespace sdmx_dl_ui.ViewModels
{
    public class DimensionsOrderingViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        internal SourceCache<DimensionViewModel , (string, string, string)> DimensionsCache { get; }
            = new SourceCache<DimensionViewModel , (string, string, string)>( s => (s.Source, s.Flow, s.Concept) );

        [Reactive] public DimensionViewModel SelectedDimension { get; set; }
        [Reactive] public string[][] KeysOccurrences { get; set; }

        public bool IsRetrievingCodes { [ObservableAsProperty] get; }

        private ReadOnlyObservableCollection<DimensionViewModel> _dimensions;
        public ReadOnlyObservableCollection<DimensionViewModel> Dimensions => _dimensions;

        public HierarchicalCodeLabelViewModel[] Hierarchies { [ObservableAsProperty] get; }
        [Reactive] public HierarchicalCodeLabelViewModel SelectedHierarchicalCode { get; set; }

        public ReactiveCommand<DimensionViewModel , Unit> ForwardPositionCommand { get; private set; }
        public ReactiveCommand<DimensionViewModel , Unit> BackwardPositionCommand { get; private set; }

        public ReactiveCommand<(DimensionViewModel[], string[][]) , HierarchicalCodeLabelViewModel[]> BuildHierarchiesCommand { get; private set; }

        public DimensionsOrderingViewModel()
        {
            Activator = new ViewModelActivator();

            InitializeCommands( this );

            BuildHierarchiesCommand
                .ToPropertyEx( this , x => x.Hierarchies , scheduler: RxApp.MainThreadScheduler );

            this.WhenActivated( disposables =>
            {
                IDisposable buildEvent = null;

                DimensionsCache.Connect()
                    .AutoRefresh( x => x.DesiredPosition )
                    .Filter( x => x.Type.Equals( "dimension" , StringComparison.InvariantCultureIgnoreCase ) )
                    .Sort( SortExpressionComparer<DimensionViewModel>.Ascending( o => o.DesiredPosition ) )
                    .ObserveOn( RxApp.MainThreadScheduler )
                    .Bind( out _dimensions )
                    .DisposeMany()
                    .Subscribe()
                    .DisposeWith( disposables );

                DimensionsCache.Connect()
                    .AutoRefresh( x => x.IsRetrievingCodes )
                    .Throttle( TimeSpan.FromMilliseconds( 20 ) )
                    .DisposeMany()
                    .Select( _ => DimensionsCache.Items.Any( x => x.IsRetrievingCodes ) )
                    .ToPropertyEx( this , x => x.IsRetrievingCodes , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                DimensionsCache.Connect()
                    .Where( t => t.Adds == 0 )
                    .DisposeMany()
                    .Do( _ =>
                    {
                        buildEvent?.Dispose();
                        Observable.Return( (Array.Empty<DimensionViewModel>(), Array.Empty<string[]>()) )
                            .InvokeCommand( BuildHierarchiesCommand );
                    } )
                    .Subscribe()
                    .DisposeWith( disposables );

                DimensionsCache.Connect()
                    .Where( t => t.Removes == 0 )
                    .Filter( x => x.Type.Equals( "dimension" , StringComparison.InvariantCultureIgnoreCase ) )
                    .Throttle( TimeSpan.FromMilliseconds( 50 ) )
                    .DisposeMany()
                    .Do( _ =>
                    {
                        buildEvent = DimensionsCache.Items
                            .Where( x => x.Type.Equals( "dimension" , StringComparison.InvariantCultureIgnoreCase ) )
                            .Select( x => x.WhenAnyValue( o => o.DesiredPosition , o => o.Values ) )
                            .CombineLatest()
                            .Where( t => t.All( o => o.Item2?.Length > 0 ) )
                            .Select( _ => DimensionsCache.Items
                                .Where( x => x.Type.Equals( "dimension" , StringComparison.InvariantCultureIgnoreCase ) )
                                .OrderBy( x => x.DesiredPosition )
                                .ToArray() )
                            .CombineLatest( this.WhenAnyValue( x => x.KeysOccurrences ) )
                            .Where( t => t.First?.Any() == true && t.Second?.Any() == true )
                            .Throttle( TimeSpan.FromMilliseconds( 150 ) )
                            .InvokeCommand( BuildHierarchiesCommand );
                    } )
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

            @this.BuildHierarchiesCommand = ReactiveCommand.CreateFromObservable( ( (DimensionViewModel[], string[][]) t ) =>
                Observable.Start( () => HierarchyBuilder.Build( t.Item1 , t.Item2 ) )
            );
        }
    }
}