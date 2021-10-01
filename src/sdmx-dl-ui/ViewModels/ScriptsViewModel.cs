using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_ui.Models;
using System;
using System.Linq;
using System.Management.Automation;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Splat;

namespace sdmx_dl_ui.ViewModels
{
    public class ScriptsViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        public bool IsWorking { [ObservableAsProperty] get; }
        public bool IsFaulted { [ObservableAsProperty] get; }
        public string ToolVersion { [ObservableAsProperty] get; }

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

        internal DimensionsOrderingViewModel DimensionsOrderingViewModel { get; }

        public ScriptsViewModel()
        {
            Activator = new ViewModelActivator();
            DimensionsOrderingViewModel = new DimensionsOrderingViewModel();

            InitializeCommands( this );

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

            Observable.CombineLatest(
                CheckScriptCommand.IsExecuting ,
                RetrieveSourcesCommand.IsExecuting ,
                RetrieveFlowsCommand.IsExecuting ,
                RetrieveDimensionsCommand.IsExecuting
                )
                .Select( sources => sources.Any( s => s ) )
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
                        DimensionsOrderingViewModel.KeysOccurrences = Array.Empty<string[]>();
                    } )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.ActiveSource , x => x.ActiveFlow )
                    .Where( x => x.Item1 != null && x.Item2 != null )
                    .InvokeCommand( RetrieveDimensionsCommand )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.ActiveSource , x => x.ActiveFlow , x => x.Dimensions )
                    .Where( t => t.Item1 != null && t.Item2 != null && t.Item3 != null )
                    .InvokeCommand( RetrieveKeysCommand )
                    .DisposeWith( disposables );

                this.WhenAnyValue( x => x.ActiveSource , x => x.ActiveFlow , x => x.Dimensions )
                    .Where( t => t.Item1 != null && t.Item2 != null && t.Item3 != null )
                    .ObserveOn( RxApp.TaskpoolScheduler )
                    .Do( t =>
                    {
                        var (source, flow, dimensions) = t;
                        DimensionsOrderingViewModel.DimensionsCache.Clear();
                        DimensionsOrderingViewModel.DimensionsCache.Edit( e =>
                        {
                            e.AddOrUpdate( dimensions.Select( d => new DimensionViewModel
                            {
                                Source = source ,
                                Flow = flow ,
                                Concept = d.Concept ,
                                Type = d.Type ,
                                Label = d.Label ,
                                Coded = d.Coded ,
                                Position = d.Position.HasValue ? d.Position.Value : 0 ,
                                DesiredPosition = d.Position.HasValue ? d.Position.Value : 0
                            } ) );
                        } );
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

                Observable.Return( Unit.Default )
                    .InvokeCommand( CheckScriptCommand );
            } );
        }

        private static void InitializeCommands( ScriptsViewModel @this )
        {
            @this.CheckScriptCommand = ReactiveCommand.CreateFromObservable( () =>
                Observable.Start( () =>
                {
                    PowerShell.Create()
                        .AddCommand( "Set-ExecutionPolicy" )
                        .AddParameter( "Scope" , "CurrentUser" )
                        .AddParameter( "ExecutionPolicy" , "Unrestricted" )
                        .Invoke();

                    var res = PowerShell.Create()
                        .AddCommand( "sdmx-dl" )
                        .AddParameter( "V" )
                        .Invoke();

                    return res.Count > 0 ? res[0].ToString() : string.Empty;
                } ) );

            @this.RetrieveSourcesCommand = ReactiveCommand.CreateFromObservable( () =>
                Observable.Start( () => PowerShellRunner.Query<Source>( "list" , "sources" ) ) );

            @this.RetrieveFlowsCommand = ReactiveCommand.CreateFromObservable( ( Source source ) =>
                Observable.Start( () => PowerShellRunner.Query<Flow>( "list" , "flows" , source.Name ) ) );

            @this.RetrieveDimensionsCommand = ReactiveCommand.CreateFromObservable( ( (Source, Flow) t ) =>
                Observable.Start( () =>
                {
                    var (source, flow) = t;
                    return PowerShellRunner.Query<Dimension>( "list" , "concepts" , source.Name , flow.Ref );
                } ) );

            @this.RetrieveKeysCommand = ReactiveCommand.CreateFromObservable( ( (Source, Flow, Dimension[]) t ) =>
                Observable.Start( () =>
                {
                    var (source, flow, dimensions) = t;

                    var count = dimensions.Count( x => x.Position.HasValue );
                    var key = string.Join( "." , Enumerable.Range( 0 , count )
                        .Select( _ => string.Empty ) );

                    var keys = PowerShellRunner.Query<SeriesKey>( "fetch" , "keys" , source.Name , flow.Ref , key );

                    var splits = keys.AsParallel()
                        .Select( k => k.Series.Split( '.' ) )
                        .ToArray();

                    return Enumerable.Range( 0 , count )
                        .Select( i => splits.Select( s => s[i] ).Distinct().ToArray() )
                        .ToArray();
                } ) );
        }
    }
}