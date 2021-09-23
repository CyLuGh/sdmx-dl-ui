using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_ui.Models;

namespace sdmx_dl_ui.ViewModels
{
    public class DimensionViewModel : ReactiveObject, IActivatableViewModel, IEquatable<DimensionViewModel>
    {
        public ViewModelActivator Activator { get; }

        public Source Source { get; init; }
        public Flow Flow { get; init; }
        public string Concept { get; init; }
        public string Type { get; init; }
        public string Label { get; init; }
        public string Coded { get; init; }
        public int Position { get; init; }

        [Reactive] public int DesiredPosition { get; set; }
        public CodeLabel[] Values { [ObservableAsProperty] get; }

        internal ReactiveCommand<Unit , CodeLabel[]> RetrieveCodesCommand { get; private set; }

        public DimensionViewModel()
        {
            Activator = new ViewModelActivator();

            InitializeCommands( this );

            this.WhenActivated( disposables =>
            {
                //RetrieveCodesCommand
                //    .ToPropertyEx( this , x => Values , scheduler: RxApp.MainThreadScheduler )
                //    .DisposeWith( disposables );

                Observable.Return( Unit.Default )
                    .InvokeCommand( RetrieveCodesCommand )
                    .DisposeWith( disposables );
            } );
        }

        private static void InitializeCommands( DimensionViewModel @this )
        {
            @this.RetrieveCodesCommand = ReactiveCommand.CreateFromObservable( () =>
                Observable.Start( () => PowerShellRunner.Query<CodeLabel>( "list" , "codes" , @this.Source.Name , @this.Flow.Ref ,
                    @this.Concept ) ) );
        }


        public bool Equals( DimensionViewModel other )
        {
            if ( ReferenceEquals( null , other ) ) return false;
            if ( ReferenceEquals( this , other ) ) return true;
            return Concept == other.Concept && Type == other.Type && Position == other.Position;
        }

        public override bool Equals( object obj )
        {
            if ( ReferenceEquals( null , obj ) ) return false;
            if ( ReferenceEquals( this , obj ) ) return true;
            if ( obj.GetType() != this.GetType() ) return false;
            return Equals( (DimensionViewModel) obj );
        }

        public override int GetHashCode() => HashCode.Combine( Concept , Type , Position );
    }
}
