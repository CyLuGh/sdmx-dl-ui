using ReactiveUI;
using sdmx_dl_ui.ViewModels;
using Splat;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace sdmx_dl_ui
{
    public partial class MainWindow : IViewFor<ScriptsViewModel>
    {
        public ScriptsViewModel ViewModel { get; set; }

        object IViewFor.ViewModel { get => ViewModel; set => ViewModel = (ScriptsViewModel) value; }

        public MainWindow()
        {
            InitializeComponent();

            this.WhenActivated( disposables =>
            {
                this.WhenAnyValue( x => x.ViewModel )
                    .WhereNotNull()
                    .Do( vm => PopulateFromViewModel( this , vm , disposables ) )
                    .Subscribe()
                    .DisposeWith( disposables );
            } );

            ViewModel = Locator.Current.GetService<ScriptsViewModel>();
        }

        private static void PopulateFromViewModel( MainWindow view , ScriptsViewModel viewModel , CompositeDisposable disposables )
        {
            view.OneWayBind( viewModel ,
                vm => vm.IsWorking ,
                v => v.WorkingGrid.Visibility ,
                b => b ? Visibility.Visible : Visibility.Collapsed )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.IsFaulted ,
                v => v.FaultedGrid.Visibility ,
                b => b ? Visibility.Visible : Visibility.Collapsed )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.ToolVersion ,
                v => v.TextBlockVersion.Text )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.Sources ,
                v => v.ComboBoxSources.ItemsSource )
                .DisposeWith( disposables );

            view.Bind( viewModel ,
                vm => vm.ActiveSource ,
                v => v.ComboBoxSources.SelectedItem )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.Flows ,
                v => v.ComboBoxFlows.ItemsSource )
                .DisposeWith( disposables );
        }
    }
}