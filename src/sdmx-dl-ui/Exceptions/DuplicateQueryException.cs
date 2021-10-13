using System;

namespace sdmx_dl_ui.Exceptions
{
    public class DuplicateQueryException : ApplicationException
    {
        public DuplicateQueryException()
        {
        }

        public DuplicateQueryException( string message ) : base( message )
        {
        }

        public DuplicateQueryException( string message , Exception innerException ) : base( message , innerException )
        {
        }
    }
}