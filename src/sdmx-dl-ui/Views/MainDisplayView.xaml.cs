using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
using MahApps.Metro.Controls;
using ReactiveUI;
using sdmx_dl_ui.ViewModels;

namespace sdmx_dl_ui.Views
{
    public partial class MainDisplayView
    {
        public MainDisplayView()
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

        private static void PopulateFromViewModel( MainDisplayView view , MainDisplayViewModel viewModel , CompositeDisposable disposables )
        {
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget( view , true );
            GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler( view , viewModel );

            view.OneWayBind( viewModel ,
                    vm => vm.HasItems ,
                    v => v.BorderDrag.Visibility ,
                    b => b ? Visibility.Collapsed : Visibility.Visible )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.HasItems ,
                    v => v.TabControl.Visibility ,
                    b => b ? Visibility.Visible : Visibility.Collapsed )
                .DisposeWith( disposables );

            viewModel.CreateTabItemInteraction.RegisterHandler( ctx =>
                {
                    var tabItem = new MetroTabItem
                    {
                        Header = ctx.Input.Key ,
                        Content = new ViewModelViewHost
                        {
                            ViewModel = ctx.Input ,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch ,
                            HorizontalAlignment = HorizontalAlignment.Stretch ,
                            VerticalContentAlignment = VerticalAlignment.Stretch ,
                            VerticalAlignment = VerticalAlignment.Stretch
                        } ,
                        CloseButtonEnabled = true ,
                        CloseTabCommand = viewModel.RemoveSeriesCommand ,
                        CloseTabCommandParameter = ctx.Input
                    };
                    view.TabControl.Items.Add( tabItem );
                    view.TabControl.SelectedItem = tabItem;
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

            viewModel.SwitchTabItemInteraction.RegisterHandler( ctx =>
                {
                    var tabItem = view.TabControl.Items.OfType<MetroTabItem>()
                        .FirstOrDefault( x => x.Header.Equals( ctx.Input.Key ) );

                    if ( tabItem != null )
                        view.TabControl.SelectedItem = tabItem;

                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );
        }
    }
}