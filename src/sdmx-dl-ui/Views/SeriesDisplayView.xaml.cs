using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using sdmx_dl_engine.Models;
using sdmx_dl_ui.ViewModels;
using ReactiveMarbles.ObservableEvents;
using ScottPlot.Plottable;
using ScottPlot;
using System.Linq;
using System.Text;

namespace sdmx_dl_ui.Views
{
    public partial class SeriesDisplayView
    {
        //private Crosshair Crosshair { get; }
        private List<ScatterPlot> Scatters { get; }

        private MarkerPlot HighlightedPoint { get; }

        public SeriesDisplayView()
        {
            InitializeComponent();

            Scatters = new();

            WpfPlot.Plot.Palette = ScottPlot.Drawing.Palette.DarkPastel;
            WpfPlot.Plot.Style( ScottPlot.Style.Seaborn );
            WpfPlot.Plot.Legend( true , ScottPlot.Alignment.LowerRight );

            //Crosshair = WpfPlot.Plot.AddCrosshair( 0 , 0 );
            //Crosshair.VerticalLine.PositionFormatter = x => DateTime.FromOADate( x ).ToString( "yyyy-MM-dd" );

            HighlightedPoint = WpfPlot.Plot.AddPoint( 0 , 0 );
            HighlightedPoint.Color = System.Drawing.Color.Red;
            HighlightedPoint.MarkerSize = 10;
            HighlightedPoint.MarkerShape = ScottPlot.MarkerShape.filledCircle;
            HighlightedPoint.IsVisible = false;

            WpfPlot.Refresh();

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
            view.ConfigurationViewHost.ViewModel = viewModel;

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

            viewModel.DrawChartInteraction.RegisterHandler( ctx =>
            {
                view.DisplaySeries( ctx.Input );
                ctx.SetOutput( Unit.Default );
            } ).DisposeWith( disposables );

            view.WpfPlot.Events()
                .MouseEnter
                .Subscribe( _ =>
                {
                    //view.Crosshair.IsVisible = true;
                } )
                .DisposeWith( disposables );

            view.WpfPlot.Events()
                .MouseLeave
                .Subscribe( _ =>
                {
                    view.HighlightedPoint.IsVisible = false;
                    //view.Crosshair.IsVisible = false;
                    view.WpfPlot.Plot.Clear( typeof( Tooltip ) );

                    view.WpfPlot.Refresh();
                } )
                .DisposeWith( disposables );

            view.WpfPlot.Events()
                .MouseMove
                .Subscribe( _ =>
                {
                    view.RenderMouseMove();
                } )
                .DisposeWith( disposables );
        }

        private void RenderMouseMove()
        {
            (double coordinateX, double coordinateY) = WpfPlot.GetMouseCoordinates();
            //Crosshair.X = coordinateX;
            //Crosshair.Y = coordinateY;
            var xyRatio = WpfPlot.Plot.XAxis.Dims.PxPerUnit / WpfPlot.Plot.YAxis.Dims.PxPerUnit;

            var location = Scatters.Select( sp => (ScatterPlot: sp, Point: sp.GetPointNearestX( coordinateX )) )
                .MinBy( x => Math.Abs( coordinateY - x.Point.y ) );

            HighlightedPoint.X = location.Point.x;
            HighlightedPoint.Y = location.Point.y;
            HighlightedPoint.Color = location.ScatterPlot.Color;
            HighlightedPoint.IsVisible = true;

            WpfPlot.Plot.Clear( typeof( Tooltip ) );
            var sb = new StringBuilder();
            sb.AppendLine( location.ScatterPlot.Label );
            sb.Append( "Period: " );
            sb.AppendFormat( "{0:yyyy-MM-dd}" , DateTime.FromOADate( location.Point.x ) ).AppendLine();
            sb.Append( "Value: " );
            sb.AppendFormat( "{0:N2}" , location.Point.y );
            WpfPlot.Plot.AddTooltip( sb.ToString() , location.Point.x , location.Point.y );

            WpfPlot.Refresh();
        }

        private void DisplaySeries( ChartSeries[] series )
        {
            WpfPlot.Plot.RenderLock();

            foreach ( var s in series )
                Scatters.Add( WpfPlot.Plot.AddScatter( s.Xs , s.Ys , label: s.Title ) );

            WpfPlot.Plot.XAxis.DateTimeFormat( true );

            WpfPlot.Plot.RenderUnlock();
            WpfPlot.Refresh();
        }
    }
}