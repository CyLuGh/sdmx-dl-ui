using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using sdmx_dl_ui.ViewModels;

namespace sdmx_dl_ui.Views
{
    public partial class SeriesDisplayView
    {
        public SeriesDisplayView()
        {
            InitializeComponent();

            CartesianChart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;

            this.WhenActivated( disposables =>
            {
                this.WhenAnyValue( x => x.ViewModel )
                    .WhereNotNull()
                    .Do( vm => PopulateFromViewModel( this , vm , disposables ) )
                    .Subscribe()
                    .DisposeWith( disposables );
            } );
        }

        private static void PopulateFromViewModel( SeriesDisplayView view , SeriesDisplayViewModel viewModel , CompositeDisposable disposables )
        {
            view.OneWayBind( viewModel ,
                    vm => vm.HasEncounteredError ,
                    v => v.BorderError.Visibility ,
                    b => b ? Visibility.Visible : Visibility.Collapsed )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.IsWorking ,
                    v => v.BorderWorking.Visibility ,
                    b => b ? Visibility.Visible : Visibility.Collapsed )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.Data ,
                    v => v.DataGrid.ItemsSource )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.LineSeries ,
                    v => v.CartesianChart.Series )
                .DisposeWith( disposables );
        }
    }
}