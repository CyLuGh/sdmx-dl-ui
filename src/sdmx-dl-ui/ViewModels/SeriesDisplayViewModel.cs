using DynamicData;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MoreLinq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_ui.Models;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdmx_dl_ui.ViewModels
{
    public class SeriesDisplayViewModel : ReactiveObject, IActivatableViewModel, IEquatable<SeriesDisplayViewModel>
    {
        [Reactive] public string Key { get; set; }
        [Reactive] public List<Axis> XAxes { get; set; }

        [Reactive] public string PeriodFormat { get; set; } = "yyyy-MM";
        [Reactive] public ushort Decimals { get; set; } = 2;

        public bool IsWorking { [ObservableAsProperty] get; }
        public bool HasEncounteredError { [ObservableAsProperty] get; }

        public DataSeriesViewModel[] Data { [ObservableAsProperty] get; }
        public MetaSeries[] Meta { [ObservableAsProperty] get; }
        public ISeries[] LineSeries { [ObservableAsProperty] get; }

        internal ReactiveCommand<string , DataSeriesViewModel[]> RetrieveDataSeriesCommand { get; private set; }
        internal ReactiveCommand<string , MetaSeries[]> RetrieveMetaSeriesCommand { get; private set; }

        public ViewModelActivator Activator { get; }

        public SeriesDisplayViewModel()
        {
            Activator = new ViewModelActivator();

            InitializeCommands( this );

            LiveCharts.Configure( config =>
           {
               config.HasMap<ChartModel>( ( cm , point ) =>
               {
                   point.PrimaryValue = cm.Value;
                   point.SecondaryValue = cm.DateTime.ToOADate();
               } );
           } );

            this.WhenAnyValue( x => x.Key )
                .Where( s => !string.IsNullOrWhiteSpace( s ) && s.Split( ' ' ).Length == 3 )
                .DistinctUntilChanged()
                .InvokeCommand( RetrieveDataSeriesCommand );

            this.WhenAnyValue( x => x.Key )
                .Where( s => !string.IsNullOrWhiteSpace( s ) && s.Split( ' ' ).Length == 3 )
                .DistinctUntilChanged()
                .InvokeCommand( RetrieveMetaSeriesCommand );

            RetrieveDataSeriesCommand
                .ToPropertyEx( this , x => x.Data , scheduler: RxApp.MainThreadScheduler );

            RetrieveMetaSeriesCommand
                .Do( meta =>
                {
                    if ( ushort.TryParse( meta.FirstOrDefault( m => m?.Concept.Equals( "DECIMALS" , StringComparison.CurrentCultureIgnoreCase ) == true )
                                        ?.Value ?? "3" , out var dec ) )
                        Decimals = dec;
                } )
                .ToPropertyEx( this , x => x.Meta , scheduler: RxApp.MainThreadScheduler );

            this.WhenAnyValue( x => x.Data , x => x.Meta )
                .Where( x => x.Item1 != null && x.Item2 != null )
                .ObserveOn( RxApp.TaskpoolScheduler )
                .Subscribe( t =>
                {
                    var (data, meta) = t;

                    meta
                       .GroupBy( m => m.Series )
                       .AsParallel()
                       .ForAll( group =>
                       {
                           var title = group.FirstOrDefault( s => s.Concept.Equals( "TITLE" , StringComparison.CurrentCultureIgnoreCase ) )
                                ?.Value ?? string.Empty;

                           title = group.FirstOrDefault( s => s.Concept.Equals( "TITLE_COMPL" , StringComparison.CurrentCultureIgnoreCase ) )
                               ?.Value ?? title;

                           if ( !ushort.TryParse( meta.FirstOrDefault( m => m.Concept.Equals( "DECIMALS" , StringComparison.CurrentCultureIgnoreCase ) )
                                        ?.Value ?? "3" , out var dec ) )
                               dec = 3;

                           data.Where( d => d.Series.Equals( group.Key ) )
                        .ForEach( d =>
                        {
                            d.Title = title;
                            d.Decimals = dec;
                        } );
                       } );
                } );

            this.WhenActivated( disposables =>
            {
                Observable.CombineLatest( RetrieveDataSeriesCommand.IsExecuting , RetrieveMetaSeriesCommand.IsExecuting )
                    .Select( runs => runs.Any( x => x ) )
                    .ToPropertyEx( this , x => x.IsWorking , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.PeriodFormat )
                    .Do( _ =>
                        {
                            XAxes = new List<Axis>
                            {
                                new Axis
                                {
                                    Labeler = value => DateTime.FromOADate( value ).ToString(PeriodFormat),
                                    LabelsRotation = 45
                                }
                            };
                        } )
                    .Subscribe()
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.Data )
                    .WhereNotNull()
                    .Select( data => BuildChartSeries( data ) )
                    .ToPropertyEx( this , x => x.LineSeries , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.Decimals )
                    .Throttle( TimeSpan.FromMilliseconds( 200 ) )
                    .Subscribe( dc => Data?.AsParallel().ForAll( d => d.Decimals = dc ) )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.PeriodFormat )
                    .Throttle( TimeSpan.FromMilliseconds( 200 ) )
                    .Subscribe( s => Data?.AsParallel().ForAll( d => d.PeriodFormat = s ) )
                    .DisposeWith( disposables );
            } );
        }

        private LineSeries<ChartModel>[] BuildChartSeries( IEnumerable<DataSeriesViewModel> dataSeries )
            => dataSeries.GroupBy( x => x.Series )
            .Select( g => new LineSeries<ChartModel>
            {
                Values = g.Select( x => new ChartModel( x.ObsPeriod , x.ObsValue ) ).ToArray() ,
                LineSmoothness = 0d ,
                Fill = null ,
                GeometryFill = null ,
                GeometryStroke = null ,
                //GeometrySize = 3,
                Name = g.Key
            } )
            .ToArray();

        private static void InitializeCommands( SeriesDisplayViewModel @this )
        {
            @this.RetrieveDataSeriesCommand = ReactiveCommand.CreateFromObservable( ( string key ) =>
                Observable.Start( () => PowerShellRunner.Query<DataSeries>( new[] { "fetch" , "data" }.Concat( key.Split( ' ' ) ).ToArray() )
                    .Select( d => new DataSeriesViewModel( d ) )
                    .OrderBy( x => x.Series ).ThenBy( x => x.ObsPeriod ).ToArray()
                 ) );

            @this.RetrieveMetaSeriesCommand = ReactiveCommand.CreateFromObservable( ( string key ) =>
               Observable.Start( () => PowerShellRunner.Query<MetaSeries>( new[] { "fetch" , "meta" }.Concat( key.Split( ' ' ) ).ToArray() )
               ) );

            @this.RetrieveDataSeriesCommand.ThrownExceptions
                .Select( _ => true )
                .ToPropertyEx( @this , x => x.HasEncounteredError , scheduler: RxApp.MainThreadScheduler );

            @this.RetrieveMetaSeriesCommand.ThrownExceptions
                .Select( exc => exc.Message )
                .InvokeCommand( Locator.Current.GetService<ScriptsViewModel>() , x => x.ShowMessageCommand );
        }

        public override bool Equals( object obj )
            => Equals( obj as SeriesDisplayViewModel );

        public override int GetHashCode() => Key.GetHashCode();

        public bool Equals( SeriesDisplayViewModel other )
        {
            if ( other == null )
                return false;

            return Key.Equals( other.Key );
        }
    }
}