using sdmx_dl_ui.ViewModels;
using Splat;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace sdmx_dl_ui
{
    public partial class App : Application
    {
        private void Application_Startup( object sender , StartupEventArgs e )
        {
            Locator.CurrentMutable.RegisterConstant( new ScriptsViewModel() );
        }
    }
}