using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using sdmx_dl_ui.Exceptions;
using sdmx_dl_ui.Models;
using Splat;

namespace sdmx_dl_ui.ViewModels
{
    public class DimensionViewModel : ReactiveObject, IEquatable<DimensionViewModel>
    {
        [Reactive] public string Source { get; set; }
        [Reactive] public string Flow { get; set; }
        [Reactive] public string Concept { get; set; }
        public string Type { get; init; }
        public string Label { get; init; }
        [Reactive] public bool Coded { get; set; }
        public int Position { get; init; }

        [Reactive] public int DesiredPosition { get; set; }
        public CodeLabel[] Values { [ObservableAsProperty] get; }

        internal ReactiveCommand<(string, string, string) , CodeLabel[]> RetrieveCodesCommand { get; private set; }
        public bool IsRetrievingCodes { [ObservableAsProperty] get; }

        public DimensionViewModel()
        {
            InitializeCommands( this );

            RetrieveCodesCommand
                .ToPropertyEx( this , x => x.Values , scheduler: RxApp.MainThreadScheduler );

            RetrieveCodesCommand
                .IsExecuting
                .ToPropertyEx( this , x => x.IsRetrievingCodes , scheduler: RxApp.MainThreadScheduler );

            this.WhenAnyValue( x => x.Source , x => x.Flow , x => x.Concept , x => x.Coded )
                .Throttle( TimeSpan.FromMilliseconds( 50 ) )
                .DistinctUntilChanged()
                .Where( t => t.Item4 && t.Item1 != null && t.Item2 != null && !string.IsNullOrEmpty( t.Item3 ) )
                .Select( t => (t.Item1, t.Item2, t.Item3) )
                .InvokeCommand( RetrieveCodesCommand );
        }

        private static void InitializeCommands( DimensionViewModel @this )
        {
            @this.RetrieveCodesCommand = ReactiveCommand.CreateFromObservable( ( (string, string, string) t ) =>
                 Observable.Start( () =>
                 {
                     if ( @this.Values?.Any() == true )
                         throw new DuplicateQueryException( "Shouldn't retrieve values again" );

                     var (source, flow, concept) = t;
                     return PowerShellRunner.Query<CodeLabel>( "list" , "codes" , source , flow , concept );
                 } ) );

            @this.RetrieveCodesCommand
                .ThrownExceptions
                .Subscribe( exc =>
                {
                    if ( exc is DuplicateQueryException dqe )
                    {
                        /* Ignore */
                    }
                } );
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