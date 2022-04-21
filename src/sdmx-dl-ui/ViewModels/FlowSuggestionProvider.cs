using AutoCompleteTextBox.Editors;
using sdmx_dl_engine.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace sdmx_dl_ui.ViewModels
{
    public class FlowSuggestionProvider : IComboSuggestionProvider
    {
        public Flow[] Flows { get; set; }

        public IEnumerable<Flow> GetSuggestions( string filter )
        {
            if ( string.IsNullOrWhiteSpace( filter ) )
                return Flows;

            return Flows.Where( s => s.Ref.Contains( filter , StringComparison.CurrentCultureIgnoreCase )
                || s.Label.Contains( filter , StringComparison.CurrentCultureIgnoreCase ) );
        }

        IEnumerable IComboSuggestionProvider.GetFullCollection()
            => Flows;

        IEnumerable IComboSuggestionProvider.GetSuggestions( string filter )
            => GetSuggestions( filter );
    }
}
