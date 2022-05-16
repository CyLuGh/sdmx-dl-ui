using CsvHelper;
using LanguageExt;
using LanguageExt.Common;
using sdmx_dl_engine.Models;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace sdmx_dl_engine
{
    public class Engine
    {
        private readonly string _enginePath;

        public Engine()
        {
            var path = Path.Combine(
                Path.GetDirectoryName( Process.GetCurrentProcess().MainModule.FileName ) ,
                "res" ,
                "sdmx-dl-bin.jar" );

            if ( !File.Exists( path ) )
            {
                throw new FileNotFoundException( "sdmx-dl-bin.jar not found" );
            }

            _enginePath = path;
        }

        private ProcessStartInfo ConfigureProcess( params string[] arguments )
            => new( "java" )
            {
                Arguments = new StringBuilder()
                    .Append( "-jar " )
                    .Append( '"' )
                    .Append( _enginePath )
                    .Append( '"' )
                    .Append( " " )
                    .AppendJoin( ' ' , arguments )
                    .ToString() ,
                CreateNoWindow = true ,
                RedirectStandardError = true ,
                RedirectStandardOutput = true ,
                UseShellExecute = false
            };

        private Either<Error , string> RunProcess( params string[] arguments )
        {
            var process = Process.Start( ConfigureProcess( arguments ) );
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if ( process.ExitCode != 0 )
            {
                var error = process.StandardError.ReadToEnd();
                return Error.New( new StringBuilder().Append( "Error running sdmx-dl " )
                    .AppendJoin( ' ' , arguments )
                    .Append( ": " )
                    .Append( error )
                    .ToString() );
            }

            return output;
        }

        public Either<Error , string> CheckVersion()
            => RunProcess( "--version" );

        private Either<Error , T[]> Query<T>( params string[] arguments )
        {
            var result = RunProcess( arguments );

            return result.Map( res =>
            {
                var list = new List<T>();
                using ( TextReader textReader = new StringReader( res ) )
                {
                    using var csvReader = new CsvReader( textReader , CultureInfo.InvariantCulture );
                    while ( csvReader.Read() )
                        list.Add( csvReader.GetRecord<T>() );
                }
                return list.ToArray();
            } );
        }

        public Either<Error , Source[]> ListSources()
            => Query<Source>( "list" , "sources" );

        public Either<Error , Flow[]> ListFlows( Source source )
            => ListFlows( source.Name );

        public Either<Error , Flow[]> ListFlows( string source )
            => Query<Flow>( "list" , "flows" , source );

        public Either<Error , Dimension[]> ListConcepts( Source source , Flow flow )
            => ListConcepts( source.Name , flow.Ref );

        public Either<Error , Dimension[]> ListConcepts( string source , string flow )
            => Query<Dimension>( "list" , "concepts" , source , flow );

        public Either<Error , CodeLabel[]> ListCodes( string source , string flow , string concept )
            => Query<CodeLabel>( "list" , "codes" , source , flow , concept );

        public Either<Error , SeriesKey[]> FetchKeys( Source source , Flow flow , Dimension[] dimensions )
        {
            var count = dimensions.Count( x => x.Position.HasValue );
            var key = string.Join( "." , Enumerable.Range( 0 , count )
                .Select( _ => string.Empty ) );

            return FetchKeys( source , flow , key );
        }

        public Either<Error , SeriesKey[]> FetchKeys( Source source , Flow flow , string key )
            => FetchKeys( source.Name , flow.Ref , key );

        public Either<Error , SeriesKey[]> FetchKeys( string source , string flow , string key )
            => Query<SeriesKey>( "fetch" , "keys" , source , flow , key );

        public Either<Error , DataSeries[]> FetchData( string key )
            => Query<DataSeries>( new[] { "fetch" , "data" }.Concat( key.Split( ' ' ) ).ToArray() );

        public Either<Error , MetaSeries[]> FetchMeta( string key )
            => Query<MetaSeries>( new[] { "fetch" , "meta" }.Concat( key.Split( ' ' ) ).ToArray() );

        public Either<Error , Access> CheckAccess( Source source )
            => CheckAccess( source.Name );

        public Either<Error , Access> CheckAccess( string source )
            => Query<Access>( "check" , "access" , source )
                .Match<Either<Error , Access>>( x => x[0] , e => e );
    }
}