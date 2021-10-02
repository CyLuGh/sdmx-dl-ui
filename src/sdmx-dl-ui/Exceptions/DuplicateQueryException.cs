using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdmx_dl_ui.Exceptions
{
    public class DuplicateQueryException : ApplicationException
    {
        public DuplicateQueryException() : base()
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