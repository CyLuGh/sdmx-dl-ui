﻿using System;
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
using Splat;

namespace sdmx_dl_ui.ViewModels
{
    public class DimensionViewModel : ReactiveObject, IEquatable<DimensionViewModel>
    {
        [Reactive] public Source Source { get; set; }
        [Reactive] public Flow Flow { get; set; }
        [Reactive] public string Concept { get; set; }
        public string Type { get; init; }
        public string Label { get; init; }
        [Reactive] public bool Coded { get; set; }
        public int Position { get; init; }

        [Reactive] public int DesiredPosition { get; set; }
        public CodeLabel[] Values { [ObservableAsProperty] get; }

        internal ReactiveCommand<(Source, Flow, string) , CodeLabel[]> RetrieveCodesCommand { get; private set; }

        public DimensionViewModel()
        {
            InitializeCommands( this );

            RetrieveCodesCommand
                .ToPropertyEx( this , x => x.Values , scheduler: RxApp.MainThreadScheduler );

            this.WhenAnyValue( x => x.Source , x => x.Flow , x => x.Concept , x => x.Coded )
                .Throttle( TimeSpan.FromMilliseconds( 50 ) )
                .DistinctUntilChanged()
                .Where( t => t.Item4 && t.Item1 != null && t.Item2 != null && !string.IsNullOrEmpty( t.Item3 ) )
                .Select( t => (t.Item1, t.Item2, t.Item3) )
                .InvokeCommand( RetrieveCodesCommand );
        }

        private static void InitializeCommands( DimensionViewModel @this )
        {
            @this.RetrieveCodesCommand = ReactiveCommand.CreateFromObservable( ( (Source, Flow, string) t ) =>
                 Observable.Start( () =>
                 {
                     var (source, flow, concept) = t;
                     return PowerShellRunner.Query<CodeLabel>( "list" , "codes" , source.Name , flow.Ref , concept );
                 } ) );
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