using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_engine.Models;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Splat;
using System.Windows;
using sdmx_dl_ui.Views.Infrastructure;
using MaterialDesignThemes.Wpf;
using System.Reflection;
using System.Runtime.Versioning;
using System.IO;
using sdmx_dl_engine;

namespace sdmx_dl_ui.ViewModels
{
    public class ScriptsViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }
        internal Engine Engine { get; }

        public bool SourcesEnabled { [ObservableAsProperty] get; }
        public bool FlowsEnabled { [ObservableAsProperty] get; }
        public bool DimensionsEnabled { [ObservableAsProperty] get; }

        public bool IsWorking { [ObservableAsProperty] get; }
        public bool IsFaulted { [ObservableAsProperty] get; }
        public string ToolVersion { [ObservableAsProperty] get; }

        [Reactive] public string LookupKey { get; set; }

        public Source[] Sources { [ObservableAsProperty] get; }
        [Reactive] public Source ActiveSource { get; set; }

        public Flow[] Flows { [ObservableAsProperty] get; }
        [Reactive] public Flow ActiveFlow { get; set; }

        public Dimension[] Dimensions { [ObservableAsProperty] get; }

        public string ResultingKey { [ObservableAsProperty] get; }

        public ReactiveCommand<Unit , string> CheckScriptCommand { get; private set; }
        public ReactiveCommand<Unit , Source[]> RetrieveSourcesCommand { get; private set; }
        public ReactiveCommand<Source , Flow[]> RetrieveFlowsCommand { get; private set; }
        public ReactiveCommand<(Source, Flow) , Dimension[]> RetrieveDimensionsCommand { get; private set; }
        public ReactiveCommand<(Source, Flow, Dimension[]) , string[][]> RetrieveKeysCommand { get; private set; }
        public ReactiveCommand<string , Unit> CopyToClipboardCommand { get; private set; }
        public ReactiveCommand<Unit , Unit> LookupKeyCommand { get; private set; }
        public ReactiveCommand<string , Unit> ShowExceptionCommand { get; private set; }

        internal Interaction<string , Unit> ShowExceptionInteraction { get; }
            = new Interaction<string , Unit>( RxApp.MainThreadScheduler );
        internal Interaction<Unit , Unit> ClosePopupInteraction { get; }
            = new Interaction<Unit , Unit>( RxApp.MainThreadScheduler );

        internal DimensionsOrderingViewModel DimensionsOrderingViewModel { get; }
        internal MainDisplayViewModel MainDisplayViewModel { get; }
        internal KeyDragHandler KeyDragHandler { get; }
        internal SourceSuggestionProvider SourceSuggestionProvider { get; }
        internal FlowSuggestionProvider FlowSuggestionProvider { get; }

        public ScriptsViewModel()
        {
            Activator = new ViewModelActivator();
            Engine = new Engine();

            DimensionsOrderingViewModel = new DimensionsOrderingViewModel();
            MainDisplayViewModel = new MainDisplayViewModel();
            KeyDragHandler = new KeyDragHandler();
            SourceSuggestionProvider = new SourceSuggestionProvider();
            FlowSuggestionProvider = new FlowSuggestionProvider();

            InitializeCommands( this );

            ShowExceptionInteraction.RegisterHandler( ctx => ctx.SetOutput( Unit.Default ) );

            CheckScriptCommand
                .Select( _ => Unit.Default )
                .InvokeCommand( RetrieveSourcesCommand );

            RetrieveSourcesCommand
                .ToPropertyEx( this , x => x.Sources , scheduler: RxApp.MainThreadScheduler );

            RetrieveFlowsCommand
                .ToPropertyEx( this , x => x.Flows , scheduler: RxApp.MainThreadScheduler );

            RetrieveDimensionsCommand
                .ToPropertyEx( this , x => x.Dimensions , scheduler: RxApp.MainThreadScheduler );

            RetrieveKeysCommand
                .Subscribe( keys =>
                {
                    DimensionsOrderingViewModel.KeysOccurrences = keys;
                } );

            RetrieveKeysCommand
                .ThrownExceptions
                .Subscribe( _ =>
                {
                    DimensionsOrderingViewModel.KeysOccurrences = Array.Empty<string[]>();
                } );

            Observable.CombineLatest(
                CheckScriptCommand.IsExecuting ,
                RetrieveSourcesCommand.IsExecuting ,
                RetrieveFlowsCommand.IsExecuting ,
                RetrieveDimensionsCommand.IsExecuting ,
                RetrieveSourcesCommand.IsExecuting ,
                RetrieveKeysCommand.IsExecuting ,
                DimensionsOrderingViewModel.WhenAnyValue( x => x.IsRetrievingCodes ) ,
                DimensionsOrderingViewModel.BuildHierarchiesCommand.IsExecuting
                )
                .Select( runs => runs.Any( x => x ) )
                .ToPropertyEx( this , x => x.IsWorking , scheduler: RxApp.MainThreadScheduler );

            this.WhenActivated( disposables =>
            {
                CheckScriptCommand
                    .ThrownExceptions
                    .Select( _ => true )
                    .ToPropertyEx( this , x => x.IsFaulted , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                CheckScriptCommand
                    .ToPropertyEx( this , x => x.ToolVersion , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.Sources )
                    .Subscribe( sources => SourceSuggestionProvider.Sources = sources )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.Flows )
                    .Subscribe( flows => FlowSuggestionProvider.Flows = flows )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.ActiveSource )
                    .Do( _ =>
                    {
                        ActiveFlow = null;
                    } )
                    .WhereNotNull()
                    .InvokeCommand( RetrieveFlowsCommand )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.ActiveFlow )
                    .Subscribe( _ =>
                    {
                        DimensionsOrderingViewModel.DimensionsCache.Clear();
                        DimensionsOrderingViewModel.KeysOccurrences = null;
                    } )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.ActiveSource , x => x.ActiveFlow )
                    .Where( x => x.Item1 != null && x.Item2 != null )
                    .InvokeCommand( RetrieveDimensionsCommand )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.Dimensions )
                    .ObserveOn( RxApp.TaskpoolScheduler )
                    .Do( dimensions =>
                    {
                        DimensionsOrderingViewModel.DimensionsCache.Clear();
                        if ( dimensions != null )
                        {
                            Observable.Return( (ActiveSource, ActiveFlow, dimensions) )
                                .InvokeCommand( RetrieveKeysCommand );

                            DimensionsOrderingViewModel.DimensionsCache.AddOrUpdate( dimensions?.Select( d => new DimensionViewModel
                            {
                                Source = this.ActiveSource.Name ,
                                Flow = this.ActiveFlow.Ref ,
                                Concept = d.Concept ,
                                Type = d.Type ,
                                Label = d.Label ,
                                Coded = d.Coded ,
                                Position = d.Position ?? 0 ,
                                DesiredPosition = d.Position ?? 0
                            } ) );
                        }
                    } )
                    .Subscribe()
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.ActiveSource , x => x.ActiveFlow )
                    .CombineLatest( DimensionsOrderingViewModel.WhenAnyValue( x => x.SelectedHierarchicalCode ) )
                    .Select( t =>
                    {
                        var (sf, selection) = t;
                        var (source, flow) = sf;

                        return $"{source?.Name} {flow?.Ref} {selection?.Code}";
                    } )
                    .ToPropertyEx( this , x => x.ResultingKey , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                Observable.CombineLatest(
                        CheckScriptCommand.IsExecuting ,
                        RetrieveSourcesCommand.IsExecuting ,
                        RetrieveFlowsCommand.IsExecuting ,
                        RetrieveDimensionsCommand.IsExecuting ,
                        RetrieveSourcesCommand.IsExecuting ,
                        RetrieveKeysCommand.IsExecuting ,
                        DimensionsOrderingViewModel.WhenAnyValue( x => x.IsRetrievingCodes ) ,
                        DimensionsOrderingViewModel.BuildHierarchiesCommand.IsExecuting
                    )
                    .Select( runs => runs.All( x => !x ) )
                    .Throttle( TimeSpan.FromMilliseconds( 200 ) )
                    .ToPropertyEx( this , x => x.SourcesEnabled , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                Observable.CombineLatest(
                        CheckScriptCommand.IsExecuting ,
                        RetrieveSourcesCommand.IsExecuting ,
                        RetrieveFlowsCommand.IsExecuting ,
                        RetrieveDimensionsCommand.IsExecuting ,
                        RetrieveSourcesCommand.IsExecuting ,
                        DimensionsOrderingViewModel.WhenAnyValue( x => x.IsRetrievingCodes ) ,
                        RetrieveKeysCommand.IsExecuting ,
                        DimensionsOrderingViewModel.BuildHierarchiesCommand.IsExecuting ,
                        this.WhenAnyValue( x => x.ActiveSource ).Select( x => x == null )
                        )
                        .Select( runs => runs.All( x => !x ) )
                        .Throttle( TimeSpan.FromMilliseconds( 200 ) )
                        .ToPropertyEx( this , x => x.FlowsEnabled , scheduler: RxApp.MainThreadScheduler )
                        .DisposeWith( disposables );

                Observable.CombineLatest(
                        CheckScriptCommand.IsExecuting ,
                        RetrieveSourcesCommand.IsExecuting ,
                        RetrieveFlowsCommand.IsExecuting ,
                        RetrieveDimensionsCommand.IsExecuting ,
                        RetrieveSourcesCommand.IsExecuting ,
                        DimensionsOrderingViewModel.WhenAnyValue( x => x.IsRetrievingCodes ) ,
                        DimensionsOrderingViewModel.BuildHierarchiesCommand.IsExecuting ,
                        RetrieveKeysCommand.IsExecuting ,
                        this.WhenAnyValue( x => x.ActiveSource ).Select( x => x == null ) ,
                        this.WhenAnyValue( x => x.ActiveFlow ).Select( x => x == null )
                    )
                    .Select( runs => runs.All( x => !x ) )
                    .Throttle( TimeSpan.FromMilliseconds( 200 ) )
                    .ToPropertyEx( this , x => x.DimensionsEnabled , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );

                Observable.Return( Unit.Default )
                    .InvokeCommand( CheckScriptCommand );
            } );
        }

        private static void InitializeCommands( ScriptsViewModel @this )
        {
            @this.CheckScriptCommand = ReactiveCommand.CreateFromObservable( () =>
                Observable.Start( () =>
                {
                    var check = @this.Engine.CheckVersion();
                    return check.Match( r => r.Split( Environment.NewLine )[0] , e => e.Message );
                } ) );

            @this.RetrieveSourcesCommand = ReactiveCommand.CreateFromObservable( () =>
                Observable.Start( () => @this.Engine.ListSources()
                    .Match( r => r , _ => Array.Empty<Source>() ) ) );

            @this.RetrieveFlowsCommand = ReactiveCommand.CreateFromObservable( ( Source source ) =>
                Observable.Start( () => @this.Engine.ListFlows( source )
                    .Match( r => r , _ => Array.Empty<Flow>() ) ) );

            @this.RetrieveDimensionsCommand = ReactiveCommand.CreateFromObservable( ( (Source, Flow) t ) =>
                Observable.Start( () =>
                {
                    var (source, flow) = t;
                    return @this.Engine.ListConcepts( source , flow )
                        .Match( r => r , _ => Array.Empty<Dimension>() );
                } ) );

            @this.RetrieveKeysCommand = ReactiveCommand.CreateFromObservable( ( (Source, Flow, Dimension[]) t ) =>
                Observable.Start( () =>
                {
                    var (source, flow, dimensions) = t;

                    var keys = @this.Engine.FetchKeys( source , flow , dimensions )
                        .Match( r => r , _ => Array.Empty<SeriesKey>() );

                    var count = dimensions.Count( x => x.Position.HasValue );
                    var splits = keys.AsParallel()
                        .Select( k => k.Series.Split( '.' ) )
                        .ToArray();

                    return Enumerable.Range( 0 , count )
                        .Select( i => splits.Select( s => s[i] ).Distinct().ToArray() )
                        .ToArray();
                } ) );

            var canCopy = @this.WhenAnyValue( x => x.ResultingKey )
                .Select( key => !string.IsNullOrWhiteSpace( key ) )
                .ObserveOn( RxApp.MainThreadScheduler );
            @this.CopyToClipboardCommand = ReactiveCommand.Create( ( string key ) =>
            {
                Clipboard.SetText( key );
            } , canCopy );

            @this.LookupKeyCommand = ReactiveCommand.CreateFromTask( async () =>
            {
                if ( @this.LookupKey.Split( ' ' ).Length == 3 )
                    @this.MainDisplayViewModel.SeriesCache.AddOrUpdate( new SeriesDisplayViewModel { Key = @this.LookupKey.Trim() } );

                await @this.ClosePopupInteraction.Handle( Unit.Default );
            } );

            @this.ShowExceptionCommand = ReactiveCommand.CreateFromObservable( ( string msg )
                => @this.ShowExceptionInteraction.Handle( msg ) );
        }
    }
}
