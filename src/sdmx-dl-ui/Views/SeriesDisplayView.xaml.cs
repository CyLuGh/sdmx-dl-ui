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
using sdmx_dl_ui.Views.Infrastructure;
using System.Drawing;

namespace sdmx_dl_ui.Views
{
    public partial class SeriesDisplayView
    {
        private Crosshair Crosshair { get; }
        private List<ScatterPlot> Scatters { get; }

        public SeriesDisplayView()
        {
            InitializeComponent();

            Scatters = new();

            WpfPlot.Plot.Palette = ScottPlot.Drawing.Palette.ColorblindFriendly;
            WpfPlot.Plot.Style( ScottPlot.Style.Seaborn );
            WpfPlot.Plot.Legend( true , ScottPlot.Alignment.LowerRight );

            Crosshair = WpfPlot.Plot.AddCrosshair( 0 , 0 );
            Crosshair.HorizontalLine.IsVisible = false;
            Crosshair.Color = Color.SlateGray;

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
                vm => vm.PeriodFormat ,
                v => v.Crosshair.VerticalLine.PositionFormatter ,
                s => x => DateTime.FromOADate( x ).ToString( s ) )
                .DisposeWith( disposables );

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
                .Subscribe( _ => view.Crosshair.IsVisible = true )
                .DisposeWith( disposables );

            view.WpfPlot.Events()
                .MouseLeave
                .Subscribe( _ =>
                {
                    view.Crosshair.IsVisible = false;
                    view.WpfPlot.Plot.Clear( typeof( Tooltip ) );

                    view.WpfPlot.Refresh();
                } )
                .DisposeWith( disposables );

            view.WpfPlot.Events()
                .MouseMove
                .Subscribe( _ => view.RenderMouseMove() )
                .DisposeWith( disposables );
        }

        private void RenderMouseMove()
        {
            (double coordinateX, double coordinateY) = WpfPlot.GetMouseCoordinates();
            Crosshair.X = coordinateX;
            //Crosshair.Y = coordinateY;
            var xyRatio = WpfPlot.Plot.XAxis.Dims.PxPerUnit / WpfPlot.Plot.YAxis.Dims.PxPerUnit;

            if ( Scatters.Count > 0 )
            {
                WpfPlot.Plot.Clear( typeof( Tooltip ) );

                var locations = Scatters.Select( sp => (ScatterPlot: sp, Point: sp.GetPointNearestX( coordinateX )) ).ToArray();

                /* Single tooltip */
                //var nearestLocation = locations.MinBy( x => Math.Abs( coordinateY - x.Point.y ) );
                //var tooltipText = $"{nearestLocation.ScatterPlot.Label} > {DateTime.FromOADate( nearestLocation.Point.x ):yyyy-MM-dd} : {nearestLocation.Point.y:N2}";
                //WpfPlot.Plot.AddTooltip( tooltipText , nearestLocation.Point.x , nearestLocation.Point.y );

                /* Multiple tooltips */
                foreach ( var location in locations )
                    DrawTooltip( location );
            }

            WpfPlot.Refresh();
        }

        private void DrawTooltip( (ScatterPlot ScatterPlot, (double x, double y, int index) Point) location )
        {
            var format = $"N{ViewModel.Decimals}";
            var tooltipText = $"{location.ScatterPlot.Label} > {location.Point.y.ToString( format )}";
            var tooltip = WpfPlot.Plot.AddTooltip( tooltipText , location.Point.x , location.Point.y );

            tooltip.FillColor = location.ScatterPlot.Color.MakeTransparent( 200 );
            tooltip.BorderColor = location.ScatterPlot.Color;
            tooltip.Font.Color = location.ScatterPlot.Color.FindForegroundColor();
            tooltip.Font.Bold = true;
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