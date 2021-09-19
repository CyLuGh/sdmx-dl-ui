using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_ui.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public ReactiveCommand<Unit , string> CheckScriptCommand { get; private set; }
        public ReactiveCommand<Unit , Source[]> RetrieveSourcesCommand { get; private set; }
        public ReactiveCommand<Source , Flow[]> RetrieveFlowsCommand { get; private set; }

        public ScriptsViewModel()
        {
            Activator = new ViewModelActivator();

            InitializeCommands( this );

            CheckScriptCommand
                .Select( _ => Unit.Default )
                .InvokeCommand( RetrieveSourcesCommand );

            RetrieveSourcesCommand
                .ToPropertyEx( this , x => x.Sources , scheduler: RxApp.MainThreadScheduler );

            RetrieveFlowsCommand
                .ToPropertyEx( this , x => x.Flows , scheduler: RxApp.MainThreadScheduler );

            Observable.Merge(
                CheckScriptCommand.IsExecuting ,
                RetrieveSourcesCommand.IsExecuting ,
                RetrieveFlowsCommand.IsExecuting
                )
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
                    .WhereNotNull()
                    .InvokeCommand( RetrieveFlowsCommand )
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
        }
    }
}