using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace sdmx_dl_ui
{
    internal static class PowerShellRunner
    {
        public static string[] Query( params string[] arguments )
        {
            var query = PowerShell.Create()
                .AddCommand( "sdmx-dl" );

            foreach ( var argument in arguments )
                query.AddArgument( argument );

            return query.Invoke()
                .Select( p => p.ToString() )
                .ToArray();
        }

        public static T[] Query<T>( params string[] arguments )
        {
            var query = PowerShell.Create()
                .AddCommand( "sdmx-dl" );

            foreach ( var argument in arguments )
                query.AddArgument( argument );

            Splat.LogHost.Default.Debug( string.Join( " " , arguments ) );

            var res = query.Invoke()
                .Select( p => ((string)p.BaseObject).Normalize(System.Text.NormalizationForm.FormD) )
                .ToArray();

            if ( query.HadErrors )
            {
                throw new ApplicationException( $"Query >>sdmx-dl {string.Join( " " , arguments )}<< had errors: {string.Join( " " , query.Streams.Error.Select( e => e.Exception.Message ) )}" );
            }

            var strOutput = string.Join( Environment.NewLine , res );

            var list = new List<T>();
            using ( TextReader textReader = new StringReader( strOutput ) )
            {
                using var csvReader = new CsvReader( textReader , CultureInfo.InvariantCulture );
                while ( csvReader.Read() )
                    list.Add( csvReader.GetRecord<T>() );
            }
            return list.ToArray();
        }
    }
}