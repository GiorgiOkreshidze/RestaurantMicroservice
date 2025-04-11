using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string resourceName, string resourceKey)
           : base($"The {resourceName} with the key '{resourceKey}' was not found.")
        {
        }
    }
}
