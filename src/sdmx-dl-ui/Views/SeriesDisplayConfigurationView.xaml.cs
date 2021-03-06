using ReactiveUI;
using sdmx_dl_ui.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace sdmx_dl_ui.Views
{
    public partial class SeriesDisplayConfigurationView
    {
        public SeriesDisplayConfigurationView()
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
        }

        private static void PopulateFromViewModel( SeriesDisplayConfigurationView view , SeriesDisplayViewModel viewModel , CompositeDisposable disposables )
        {
            view.Bind( viewModel ,
                vm => vm.PeriodFormat ,
                v => v.TextBoxDateFormat.Text )
                .DisposeWith( disposables );

            view.Bind( viewModel ,
                vm => vm.Decimals ,
                v => v.NumericUpDownDecimals.Value ,
                us => (double) us ,
                d => (ushort) d )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.IsParsingKey ,
                v => v.DockPanelParsingNotification.Visibility ,
                b => b ? Visibility.Visible : Visibility.Collapsed )
            .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.IsParsingKey ,
                v => v.ComboBoxTitleDimension.Visibility ,
                b => b ? Visibility.Collapsed : Visibility.Visible )
            .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.DimensionChoosers ,
                v => v.ComboBoxTitleDimension.ItemsSource )
            .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                vm => vm.HasTitle ,
                v => v.ComboBoxTitleDimension.IsEnabled ,
                b => !b )
            .DisposeWith( disposables );

            view.Bind( viewModel ,
                vm => vm.SelectedDimensionChooser ,
                v => v.ComboBoxTitleDimension.SelectedItem )
            .DisposeWith( disposables );
        }
    }
}