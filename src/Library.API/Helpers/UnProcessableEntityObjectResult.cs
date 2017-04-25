using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    public class UnProcessableEntityObjectResult : ObjectResult
    {
        /// <summary>
        /// The content that will be written out to reponse body
        /// Serializable error defines serializable container for storing model state information as key value pair
        /// </summary>
        /// <param name="modelState"></param>
        public UnProcessableEntityObjectResult(ModelStateDictionary modelState) : 
            base(new SerializableError(modelState))
        {
            if (modelState == null)
                throw new ArgumentNullException(nameof(modelState));

            StatusCode = 422;
        }
    }
}
