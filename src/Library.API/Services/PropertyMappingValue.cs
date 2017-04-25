using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Services
{
    /// <summary>
    /// this contains the list of property of underlying entity
    /// </summary>
    public class PropertyMappingValue
    {
        public IEnumerable<string> DestinationProporties { get; set; }

        public bool Revert { get; set; }

        public PropertyMappingValue(IEnumerable<string> destinationProporties,bool revert=false)
        {
            DestinationProporties = destinationProporties;
            Revert = revert;
        }
    }
}
