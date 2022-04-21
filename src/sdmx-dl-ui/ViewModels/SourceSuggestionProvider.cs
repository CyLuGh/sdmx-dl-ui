using AutoCompleteTextBox.Editors;
using sdmx_dl_engine.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdmx_dl_ui.ViewModels
{
    public class SourceSuggestionProvider : IComboSuggestionProvider
    {
        public Source[] Sources { get; set; }

        public IEnumerable<Source> GetSuggestions(string filter )
        {
            if ( string.IsNullOrWhiteSpace( filter ) )
                return Sources;

            return Sources.Where( s => s.Name.Contains( filter , StringComparison.CurrentCultureIgnoreCase )
                || s.Description.Contains( filter , StringComparison.CurrentCultureIgnoreCase ) );
        }

        IEnumerable IComboSuggestionProvider.GetFullCollection()
            => Sources;

        IEnumerable IComboSuggestionProvider.GetSuggestions( string filter )
            => GetSuggestions( filter );
    }
}
