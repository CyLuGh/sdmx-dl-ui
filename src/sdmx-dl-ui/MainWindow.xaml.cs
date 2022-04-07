using MahApps.Metro.Controls;
using Notification.Wpf;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using sdmx_dl_ui.ViewModels;
using Splat;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace sdmx_dl_ui
{
    public partial class MainWindow : IViewFor<ScriptsViewModel>
    {
        public ScriptsViewModel ViewModel { get; set; }

        object IViewFor.ViewModel { get => ViewModel; set => ViewModel = (ScriptsViewModel) value; }

        private readonly NotificationManager _notificationManager;

        public MainWindow()
        {
            InitializeComponent();

            _notificationManager = new NotificationManager();

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
                v => v.ProgressBarWorking.Visibility ,
                b => b ? Visibility.Visible : Visibility.Collapsed )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.SourcesEnabled ,
                    v => v.ComboBoxSources.IsEnabled )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.FlowsEnabled ,
                    v => v.ComboBoxFlows.IsEnabled )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.DimensionsEnabled ,
                    v => v.DimensionsOrderingViewHost.IsEnabled )
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

            view.Bind( viewModel ,
                    vm => vm.ActiveFlow ,
                    v => v.ComboBoxFlows.SelectedItem )
                .DisposeWith( disposables );

            view.OneWayBind( viewModel ,
                    vm => vm.ResultingKey ,
                    v => v.TextBlockSelection.Text )
                .DisposeWith( disposables );

            view.BindCommand( viewModel ,
                    vm => vm.CopyToClipboardCommand ,
                    v => v.ButtonCopy ,
                    viewModel.WhenAnyValue( x => x.ResultingKey ) )
                .DisposeWith( disposables );

            view.Bind( viewModel ,
                    vm => vm.LookupKey ,
                    v => v.TextBoxLookup.Text )
                .DisposeWith( disposables );

            viewModel.ClosePopupInteraction.RegisterHandler( ctx =>
                {
                    view.TextBoxLookup.Text = string.Empty;
                    view.PopupBoxLookup.IsPopupOpen = false;
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

            viewModel.ShowExceptionInteraction.RegisterHandler( ctx =>
                {
                    view._notificationManager.Show( ctx.Input , NotificationType.Error , "WindowArea" , TimeSpan.FromMinutes( 5 ) , NotificationTextTrimType.NoTrim );
                    ctx.SetOutput( Unit.Default );
                } )
                .DisposeWith( disposables );

            view.TextBoxLookup.Events()
                .KeyDown
                .Subscribe( evt =>
                {
                    if ( evt.Key == Key.Return || evt.Key == Key.Enter )
                    {
                        Observable.Return( Unit.Default )
                            .InvokeCommand( viewModel , x => x.LookupKeyCommand );
                    }
                } )
                .DisposeWith( disposables );

            TextBoxHelper.SetButtonCommand( view.TextBoxLookup , viewModel.LookupKeyCommand );
            TextBoxHelper.SetButtonCommandParameter( view.TextBoxLookup , Unit.Default );

            view.DimensionsOrderingViewHost.ViewModel = viewModel.DimensionsOrderingViewModel;
            view.MainDisplayViewHost.ViewModel = viewModel.MainDisplayViewModel;

            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource( view.TextBlockSelection , true );
            GongSolutions.Wpf.DragDrop.DragDrop.SetDragHandler( view.TextBlockSelection , viewModel.KeyDragHandler );
        }
    }
}