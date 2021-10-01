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
using ReactiveUI;
using sdmx_dl_ui.ViewModels;

namespace sdmx_dl_ui.Views
{
    public partial class DimensionListView
    {
        public DimensionListView()
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

        private static void PopulateFromViewModel( DimensionListView view , DimensionViewModel viewModel , CompositeDisposable disposables )
        {
            view.OneWayBind( viewModel ,
                    vm => vm.DesiredPosition ,
                    v => v.TextBlockPosition.Text ,
                    i => $"{i}" )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.Label ,
                    v => v.TextBlockDescription.Text )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.IsRetrievingCodes ,
                    v => v.ProgressBar.Visibility ,
                    b => b ? Visibility.Visible : Visibility.Collapsed )
                .DisposeWith( disposables );
        }
    }
}
