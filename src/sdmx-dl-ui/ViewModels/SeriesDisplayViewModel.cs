using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_ui.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdmx_dl_ui.ViewModels
{
    public class SeriesDisplayViewModel : ReactiveObject, IActivatableViewModel
    {
        [Reactive] public string Key { get; set; }

        public bool isWorking { [ObservableAsProperty] get; }
        public bool HasEncounteredError { [ObservableAsProperty] get; }

        internal ReactiveCommand<string , DataSeries[]> RetrieveDataSeriesCommand { get; private set; }

        public ViewModelActivator Activator { get; }

        public SeriesDisplayViewModel()
        {
            Activator = new ViewModelActivator();
            InitializeCommands( this );

            this.WhenAnyValue( x => x.Key )
                .Where( s => !string.IsNullOrWhiteSpace( s ) && s.Split( ' ' ).Length == 3 )
                .DistinctUntilChanged()
                .InvokeCommand( RetrieveDataSeriesCommand );

            this.WhenActivated( disposables =>
            {
                RetrieveDataSeriesCommand.IsExecuting
                    .ToPropertyEx( this , x => x.isWorking , scheduler: RxApp.MainThreadScheduler )
                    .DisposeWith( disposables );
            } );
        }

        private static void InitializeCommands( SeriesDisplayViewModel @this )
        {
            @this.RetrieveDataSeriesCommand = ReactiveCommand.CreateFromObservable( ( string key ) =>
                Observable.Start( () => PowerShellRunner.Query<DataSeries>( new[] { "fetch" , "data" }.Concat( key.Split( ' ' ) ).ToArray() ) ) );

            @this.RetrieveDataSeriesCommand.ThrownExceptions
                .Select( _ => true )
                .ToPropertyEx( @this , x => x.HasEncounteredError , scheduler: RxApp.MainThreadScheduler );
        }
    }
}