using CsvHelper;
using LanguageExt;
using LanguageExt.Common;
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
                "sdmx-dl-3.0.0-beta.5-bin.jar" );

            if ( !File.Exists( path ) )
            {
                throw new FileNotFoundException( "sdmx-dl-3.0.0-beta.5-bin.jar not found" );
            }

            _enginePath = path;
        }

        private ProcessStartInfo ConfigureProcess( params string[] arguments )
            => new( "java" )
            {
                Arguments = new StringBuilder()
                    .Append( "-jar " )
                    .Append( _enginePath )
                    .Append( " " )
                    .AppendJoin( ' ',  arguments )
                    .ToString(),
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

        public Either<Error, T[]> Query<T>( params string[] arguments )
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
    }
}