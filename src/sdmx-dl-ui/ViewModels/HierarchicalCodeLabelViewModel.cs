using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdmx_dl_ui.ViewModels
{
    public class HierarchicalCodeLabelViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }

        public string Code { get; init; }
        public string Label { get; init; }
        [Reactive] public HierarchicalCodeLabelViewModel[] Children { get; set; }

        public HierarchicalCodeLabelViewModel()
        {
            Activator = new ViewModelActivator();
        }
    }
}