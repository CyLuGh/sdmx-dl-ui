using System.Reflection.Emit;
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
        [Reactive] public bool HasTitle { get; set; }

        public bool IsWorking { [ObservableAsProperty] get; }
        public bool IsParsingKey { [ObservableAsProperty] get; }
        public bool HasEncounteredError { [ObservableAsProperty] get; }

        public DataSeriesViewModel[] Data { [ObservableAsProperty] get; }
        public MetaSeries[] Meta { [ObservableAsProperty] get; }
        public CodeLabelInfo[] Infos { [ObservableAsProperty] get; }
        public DimensionChooser[] DimensionChoosers { [ObservableAsProperty] get; }
        [Reactive] public DimensionChooser SelectedDimensionChooser { get; set; }
        public ISeries[] LineSeries { [ObservableAsProperty] get; }

        internal ReactiveCommand<string , DataSeriesViewModel[]> RetrieveDataSeriesCommand { get; private set; }
        internal ReactiveCommand<string , MetaSeries[]> RetrieveMetaSeriesCommand { get; private set; }
        internal ReactiveCommand<(string, string[]) , CodeLabelInfo[]> ParseKeyCommand { get; private set; }

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

                config.AddLightTheme();
            } );

            this.WhenAnyValue( x => x.Key )
                .Where( s => !string.IsNullOrWhiteSpace( s ) && s.Split( ' ' ).Length == 3 )
                .DistinctUntilChanged()
                .InvokeCommand( RetrieveDataSeriesCommand );

            //this.WhenAnyValue( x => x.Key )
            //    .Delay( TimeSpan.FromMilliseconds( 100 ) )

            RetrieveDataSeriesCommand
                .WhereNotNull()
                .Select( _ => this.Key )
                .Where( s => !string.IsNullOrWhiteSpace( s ) && s.Split( ' ' ).Length == 3 )
                .DistinctUntilChanged()
                .InvokeCommand( RetrieveMetaSeriesCommand );

            this.WhenAnyValue( x => x.Key , x => x.Data )
                .Where( t => !string.IsNullOrEmpty( t.Item1 ) && t.Item2 != null )
                .Select( t => (t.Item1, t.Item2.Select( o => o.Series ).Distinct().ToArray()) )
                .InvokeCommand( ParseKeyCommand );

            RetrieveDataSeriesCommand
                .ToPropertyEx( this , x => x.Data , scheduler: RxApp.MainThreadScheduler );

            RetrieveMetaSeriesCommand
                .Do( meta =>
                {
                    if ( ushort.TryParse( Array.Find( meta , m => m?.Concept.Equals( "DECIMALS" , StringComparison.CurrentCultureIgnoreCase ) == true )
                                        ?.Value ?? "3" , out var dec ) )
                    {
                        Decimals = dec;
                    }
                } )
                .ToPropertyEx( this , x => x.Meta , scheduler: RxApp.MainThreadScheduler );

            ParseKeyCommand
                .ToPropertyEx( this , x => x.Infos , scheduler: RxApp.MainThreadScheduler );

            this.WhenAnyValue( x => x.Infos )
                .WhereNotNull()
                .Select( i => i.Select( x => new DimensionChooser { Position = x.Position , Description = x.Description } )
                                .Distinct()
                                .ToArray()
                )
                .ToPropertyEx( this , x => x.DimensionChoosers , scheduler: RxApp.MainThreadScheduler );

            this.WhenAnyValue( x => x.Data , x => x.Meta )
                .Where( x => x.Item1 != null && x.Item2 != null )
                .ObserveOn( RxApp.TaskpoolScheduler )
                .Subscribe( t =>
                {
                    var (data, meta) = t;

                    meta
                        .Where( x => x != null )
                        .GroupBy( m => m.Series )
                        .AsParallel()
                        .ForAll( group =>
                        {
                            var title = group.FirstOrDefault( s => s.Concept.Equals( "TITLE" , StringComparison.CurrentCultureIgnoreCase ) )
                                 ?.Value ?? string.Empty;

                            title = group.FirstOrDefault( s => s.Concept.Equals( "TITLE_COMPL" , StringComparison.CurrentCultureIgnoreCase ) )
                                ?.Value ?? title;

                            HasTitle = !string.IsNullOrEmpty( title );

                            if ( !ushort.TryParse( Array.Find( meta , m => m.Concept.Equals( "DECIMALS" , StringComparison.CurrentCultureIgnoreCase ) )
                                         ?.Value ?? "3" , out var dec ) )
                            {
                                dec = 3;
                            }

                            data
                                .Where( d => d.Series.Equals( group.Key ) )
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
                    .Select( BuildChartSeries )
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

                this.WhenAnyValue( x => x.SelectedDimensionChooser )
                    .WhereNotNull()
                    .Throttle( TimeSpan.FromMilliseconds( 50 ) )
                    .Subscribe( dimensionChooser =>
                    {
                        Data?.AsParallel().ForAll( d =>
                        {
                            var code = d.Series.Split( '.' )[dimensionChooser.Position - 1];
                            var info = Array.Find( Infos , x => x.Position == dimensionChooser.Position && x.Code.Equals( code ) );
                            d.Title = info?.Label ?? string.Empty;
                        } );
                    } )
                    .DisposeWith( disposables );

                ParseKeyCommand.IsExecuting
                    .ToPropertyEx( this , x => x.IsParsingKey , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );
            } );
        }

        private static LineSeries<ChartModel>[] BuildChartSeries( IEnumerable<DataSeriesViewModel> dataSeries )
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

            @this.ParseKeyCommand = ReactiveCommand.CreateFromObservable( ( (string, string[]) t ) =>
                Observable.Start( () =>
                {
                    var (key, dataKeys) = t;
                    return ParseKey( key , dataKeys );
                } ) );

            @this.ParseKeyCommand.ThrownExceptions
                .Select( exc => $"Couldn't parse key: {exc.Message}" )
                .InvokeCommand( Locator.Current.GetService<ScriptsViewModel>() , x => x.ShowMessageCommand );
        }

        /// <summary>
        /// Parse given key to retrieve intelligible values for each of its components. This will execute several SDMX queries and will probably be slow.
        /// </summary>
        /// <param name="key">Key with flow, source and identifier (ex: ECB EXR M.USD+CHF.EUR.SP00.A)</param>
        /// <returns>Position in the key (1-based), code in key, corresponding label</returns>
        public static CodeLabelInfo[] ParseKey( string key , string[] dataKeys )
        {
            if ( string.IsNullOrWhiteSpace( key ) )
                return Array.Empty<CodeLabelInfo>();

            var elements = key.Split( ' ' );
            if ( elements.Length != 3 )
                return Array.Empty<CodeLabelInfo>();

            var source = elements[0];
            var flow = elements[1];
            var dimensions = PowerShellRunner.Query<Dimension>( "list" , "concepts" , source , flow );

            var details = dimensions
                .AsParallel()
                .Where( d => d.Coded && d.Type.Equals( "dimension" , StringComparison.CurrentCultureIgnoreCase ) )
                .ToDictionary( d => d.Concept , d => PowerShellRunner.Query<CodeLabel>( "list" , "codes" , source , flow , d.Concept )
            );

            var parsed = dataKeys.SelectMany( dk =>
            {
                var splits = dk.Split( '.' );
                return splits.Select( ( s , position ) =>
                {
                    var dimension = Array.Find( dimensions , x => x.Position == position + 1 );
                    if ( dimension != null && details.TryGetValue( dimension.Concept , out var codeLabels ) )
                    {
                        var codeLabel = Array.Find( codeLabels , o => o.Code.Equals( s , StringComparison.CurrentCultureIgnoreCase ) );
                        return new CodeLabelInfo( position + 1 , dimension.Label , s , codeLabel?.Label ?? s );
                    }

                    return new CodeLabelInfo( position + 1 , "?" , s , s );
                } );
            } ).ToArray();

            return parsed;
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