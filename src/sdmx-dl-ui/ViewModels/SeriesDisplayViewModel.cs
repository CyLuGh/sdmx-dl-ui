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

        public bool IsWorking { [ObservableAsProperty] get; }
        public bool HasEncounteredError { [ObservableAsProperty] get; }
        public DataSeries[] Data { [ObservableAsProperty] get; }
        public ISeries[] LineSeries { [ObservableAsProperty] get; }

        internal ReactiveCommand<string , DataSeries[]> RetrieveDataSeriesCommand { get; private set; }

        internal Interaction<Unit , Unit> RefreshChartInteraction { get; }
            = new Interaction<Unit , Unit>( RxApp.MainThreadScheduler );

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

            RetrieveDataSeriesCommand
                .ToPropertyEx( this , x => x.Data , scheduler: RxApp.MainThreadScheduler );

            //this.WhenAnyValue( x => x.Data )
            //    .WhereNotNull()
            //    .Where( _ => PlotModel.Series.Count == 0 )
            //    .Select( data => BuildChartSeries( data ) )
            //    .ObserveOn( RxApp.MainThreadScheduler )
            //    .Subscribe( async lss =>
            //    {
            //        PlotModel.Series.Clear();
            //        foreach ( var ls in lss )
            //            PlotModel.Series.Add( ls );
            //        await RefreshChartInteraction.Handle( Unit.Default );
            //    } );

            this.WhenActivated( disposables =>
            {
                RetrieveDataSeriesCommand.IsExecuting
                    .ToPropertyEx( this , x => x.IsWorking , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.Data )
                    .WhereNotNull()
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
                Fill = null ,
                GeometryFill = null ,
                GeometryStroke = null ,
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
        }

        // override object.Equals
        public override bool Equals( object obj )
            => Equals( obj as SeriesDisplayViewModel );

        // override object.GetHashCode
        public override int GetHashCode() => Key.GetHashCode();

        public bool Equals( SeriesDisplayViewModel other )
        {
            if ( other == null )
                return false;

            return Key.Equals( other.Key );
        }
    }
}