using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public class LinkedCollectionWrapperDto<T>:LinkedResourceBasedDto where T :LinkedResourceBasedDto
    {
        public IEnumerable<T> Values { get; set; }

        public LinkedCollectionWrapperDto(IEnumerable<T> values)
        {
            Values = values;
        }
    }
}
