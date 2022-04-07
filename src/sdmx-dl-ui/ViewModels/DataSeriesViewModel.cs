using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdmx_dl_ui.ViewModels
{
    public class DataSeriesViewModel : ReactiveObject, IActivatableViewModel
    {
        [Reactive] public string Series { get; set; }
        [Reactive] public string Title { get; set; }
        [Reactive] public string ObsAttributes { get; set; }
        [Reactive] public DateTime ObsPeriod { get; set; }
        [Reactive] public double ObsValue { get; set; }

        [Reactive] public string PeriodFormat { get; set; } = "yyyy-MM";
        [Reactive] public ushort Decimals { get; set; } = 2;

        public string FormattedPeriod { [ObservableAsProperty] get; }
        public string FormattedValue { [ObservableAsProperty] get; }

        public ViewModelActivator Activator { get; }

        public DataSeriesViewModel()
        {
            Activator = new ViewModelActivator();

            this.WhenAnyValue( x => x.ObsPeriod , x => x.PeriodFormat )
                    .Select( t =>
                    {
                        var (dt, format) = t;
                        return dt.ToString( format );
                    } )
                    .ToPropertyEx( this , x => x.FormattedPeriod , scheduler: RxApp.MainThreadScheduler );

            this.WhenAnyValue( x => x.ObsValue , x => x.Decimals )
                .Select( t =>
                {
                    var (obs, decimals) = t;
                    return obs.ToString( $"N{decimals}" );
                } )
                .ToPropertyEx( this , x => x.FormattedValue , scheduler: RxApp.MainThreadScheduler );

            //this.WhenActivated( disposables =>
            //{
            //this.WhenAnyValue( x => x.ObsPeriod , x => x.PeriodFormat )
            //    .Select( t =>
            //    {
            //        var (dt, format) = t;
            //        return dt.ToString( format );
            //    } )
            //    .ToPropertyEx( this , x => x.FormattedPeriod , scheduler: RxApp.MainThreadScheduler )
            //    .DisposeWith( disposables );

            //this.WhenAnyValue( x => x.ObsValue , x => x.Decimals )
            //    .Select( t =>
            //    {
            //        var (obs, decimals) = t;
            //        return obs.ToString( $"N{decimals}" );
            //    } )
            //    .ToPropertyEx( this , x => x.FormattedValue , scheduler: RxApp.MainThreadScheduler )
            //    .DisposeWith( disposables );
            //} );
        }

        public DataSeriesViewModel( DataSeries dataSeries ) : this()
        {
            Series = dataSeries.Series;
            ObsAttributes = dataSeries.ObsAttributes;
            ObsPeriod = dataSeries.ObsPeriod;
            ObsValue = dataSeries.ObsValue ?? double.NaN;
        }
    }
}