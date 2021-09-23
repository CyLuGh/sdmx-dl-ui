using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using sdmx_dl_ui.ViewModels;

namespace sdmx_dl_ui.Views
{
    public partial class DimensionsOrderingView
    {
        public DimensionsOrderingView()
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

        private static void PopulateFromViewModel( DimensionsOrderingView view , DimensionsOrderingViewModel viewModel , CompositeDisposable disposables )
        {
            view.OneWayBind( viewModel ,
                    vm => vm.Dimensions ,
                    v => v.ListBoxDimensions.ItemsSource )
                .DisposeWith( disposables );

            view.Bind( viewModel ,
                vm => vm.SelectedDimension ,
                v => v.ListBoxDimensions.SelectedItem )
                .DisposeWith( disposables );

            view.BindCommand( viewModel ,
                    vm => vm.ForwardPositionCommand ,
                    v => v.ButtonForward ,
                    viewModel.WhenAnyValue( x => x.SelectedDimension ) )
                .DisposeWith( disposables );

            view.BindCommand( viewModel ,
                    vm => vm.BackwardPositionCommand ,
                    v => v.ButtonBackward ,
                    viewModel.WhenAnyValue( x => x.SelectedDimension ) )
                .DisposeWith( disposables );
        }
    }
}
