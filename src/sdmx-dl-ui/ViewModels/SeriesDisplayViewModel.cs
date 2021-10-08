using DynamicData;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_ui.Models;
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

        public bool IsWorking { [ObservableAsProperty] get; }
        public bool HasEncounteredError { [ObservableAsProperty] get; }


        public DataSeries[] Data { [ObservableAsProperty] get; }
        public ISeries[] LineSeries { [ObservableAsProperty] get; }

        internal ReactiveCommand<string , DataSeries[]> RetrieveDataSeriesCommand { get; private set; }
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

            this.WhenActivated( disposables =>
            {
                Observable.CombineLatest( RetrieveDataSeriesCommand.IsExecuting , RetrieveMetaSeriesCommand.IsExecuting )
                    .Select( runs => runs.Any( x => x ) )
                    .ToPropertyEx( this , x => x.IsWorking , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.Data )
                    .WhereNotNull()
                    .Do( _ =>
                    {
                        XAxes = new List<Axis>
                        {
                            new Axis
                            {
                                Labeler = value => DateTime.FromOADate( value ).ToString("yyyy-MM"),
                                LabelsRotation = 45
                            }
                        };
                    } )
                    .Select( data => BuildChartSeries( data ) )
                    .ToPropertyEx( this , x => x.LineSeries , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );
            } );
        }

        private LineSeries<ChartModel>[] BuildChartSeries( IEnumerable<DataSeries> dataSeries )
            => dataSeries.GroupBy( x => x.Series )
            .Select( g => new LineSeries<ChartModel>
            {
                Values = g.Select( x => new ChartModel( x.ObsPeriod , x.ObsValue.HasValue ? x.ObsValue.Value : double.NaN ) ).ToArray() ,
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
                    .OrderBy( x => x.Series ).ThenBy( x => x.ObsPeriod ).ToArray()
                 ) );

            @this.RetrieveDataSeriesCommand.ThrownExceptions
                .Select( _ => true )
                .ToPropertyEx( @this , x => x.HasEncounteredError , scheduler: RxApp.MainThreadScheduler );

            @this.RetrieveMetaSeriesCommand = ReactiveCommand.CreateFromObservable( ( string key ) =>
                Observable.Start( () => PowerShellRunner.Query<MetaSeries>( new[] { "fetch" , "meta" }.Concat( key.Split( ' ' ) ).ToArray() )
                ) );
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