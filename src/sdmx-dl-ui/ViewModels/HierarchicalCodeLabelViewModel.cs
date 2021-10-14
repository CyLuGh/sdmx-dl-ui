using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;

namespace sdmx_dl_ui.ViewModels
{
    public class HierarchicalCodeLabelViewModel : ReactiveObject, IActivatableViewModel, IEquatable<HierarchicalCodeLabelViewModel>
    {
        public static HierarchicalCodeLabelViewModel Dummy { get; }
            = new() { Code = "Dummy" , Label = "Dummy" };

        public ViewModelActivator Activator { get; }

        [Reactive] public string Code { get; init; }
        [Reactive] public string Label { get; init; }
        [Reactive] public bool IsExpanded { get; set; }
        public int Position { get; init; }
        public ObservableCollection<HierarchicalCodeLabelViewModel> Children { get; }

        public bool HasDummyChild
            => Children.Count == 1 && Children[0].Equals( Dummy );

        public HierarchicalCodeLabelViewModel( bool lazyLoad = true )
        {
            Activator = new ViewModelActivator();

            Children = new ObservableCollection<HierarchicalCodeLabelViewModel>();

            if ( lazyLoad )
                Children.Add( Dummy );

            this.WhenActivated( disposables =>
            {
                this.WhenAnyValue( x => x.IsExpanded )
                    .Where( x => x )
                    .Do( _ =>
                    {
                        if ( HasDummyChild )
                        {
                            Children.Clear();
                            LoadChildren();
                        }
                    } )
                    .Subscribe()
                    .DisposeWith( disposables );
            } );
        }

        private void LoadChildren()
        {
            this.Log().Debug( "LoadChildren" );
            var dovm = Locator.Current.GetService<ScriptsViewModel>().DimensionsOrderingViewModel;
            Children.AddRange( HierarchyBuilder.Build( Code , Position + 1 ,
                dovm.Dimensions , dovm.KeysOccurrences ) );
        }

        public bool Equals( HierarchicalCodeLabelViewModel other )
        {
            if ( other == null )
                return false;

            return Code.Equals( other.Code ) && Label.Equals( other.Label );
        }

        public override bool Equals( object obj )
            => Equals( obj as HierarchicalCodeLabelViewModel );

        public override int GetHashCode()
            => HashCode.Combine( Code.GetHashCode() , Label.GetHashCode() );
    }
}