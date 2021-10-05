using sdmx_dl_ui.ViewModels;
using Splat;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using sdmx_dl_ui.Views;
using Splat.NLog;

namespace sdmx_dl_ui
{
    public partial class App : Application
    {
        private void Application_Startup( object sender , StartupEventArgs e )
        {
            ConfigureLogs();
            Locator.CurrentMutable.UseNLogWithWrappingFullLogger();

            Locator.CurrentMutable.RegisterConstant( new ScriptsViewModel() );

            Locator.CurrentMutable.Register( () => new DimensionsOrderingView() , typeof( IViewFor<DimensionsOrderingViewModel> ) );
            Locator.CurrentMutable.Register( () => new DimensionListView() , typeof( IViewFor<DimensionViewModel> ) , "List" );
            Locator.CurrentMutable.Register( () => new HierarchicalCodeLabelView() , typeof( IViewFor<HierarchicalCodeLabelViewModel> ) );
            Locator.CurrentMutable.Register( () => new SeriesDisplayView() , typeof( IViewFor<SeriesDisplayViewModel> ) );
        }

        private static void ConfigureLogs()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget( "logfile" )
            {
                FileName = "sdmx-dl-ui.log" ,
                DeleteOldFileOnStartup = true
            };
            var logconsole = new NLog.Targets.ConsoleTarget( "logconsole" );

            // Rules for mapping loggers to targets
            config.AddRule( NLog.LogLevel.Info , NLog.LogLevel.Fatal , logconsole );
            config.AddRule( NLog.LogLevel.Debug , NLog.LogLevel.Fatal , logfile );

            // Apply config
            NLog.LogManager.Configuration = config;
        }
    }
}