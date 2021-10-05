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

        internal ReactiveCommand<string , DataSeries[]> RetrieveDataSeriesCommand { get; private set; }

        public ViewModelActivator Activator { get; }

        public SeriesDisplayViewModel()
        {
            InitializeCommands( this );

            this.WhenActivated( disposables =>
            {
                this.WhenAnyValue( x => x.Key )
                    .Where( s => s.Split( ' ' ).Length == 3 )
                    .DistinctUntilChanged()
                    .InvokeCommand( RetrieveDataSeriesCommand )
                    .DisposeWith( disposables );
            } );
        }

        private static void InitializeCommands( SeriesDisplayViewModel @this )
        {
            @this.RetrieveDataSeriesCommand = ReactiveCommand.CreateFromObservable( ( string key ) =>
                Observable.Start( () => PowerShellRunner.Query<DataSeries>( new[] { "fetch" , "data" }.Concat( key.Split( ' ' ) ).ToArray() ) ) );
        }
    }
}