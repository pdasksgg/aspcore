using Library.API.Entities;
using Library.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Services
{
    public class PropertyMappingService : IPropertyMappingService
    {

        //private IDictionary<string, Func<IQueryable<Author>, IOrderedQueryable<Author>>> orderByFunctions =
        //    new Dictionary<string, Func<IQueryable<Author>, IOrderedQueryable<Author>>>()
        //    {
        //        {"Id",author=>author.OrderBy(p=>p.Id)}
        //    };


        /// <summary>
        /// Defines the mapping as key value pair from AuthorDto to Author
        /// 
        /// </summary>
        private Dictionary<string, PropertyMappingValue> _authorPropertyMapping =
            new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id",new PropertyMappingValue(new List<string>{ "Id"})},
                { "Genre",new PropertyMappingValue(new List<string>{ "Genre"})},
                { "Age",new PropertyMappingValue(new List<string>{ "DateOfBirth"},true)},
                { "Name",new PropertyMappingValue(new List<string>{ "FirstName","LastName"})}
            };

        /// <summary>
        /// eg.AuthorDto to Author
        /// mapping property of AuthorDto to underlying entity
        /// </summary>
        private IList<IPropertyMapping> propertyMappings = new List<IPropertyMapping>();

        public PropertyMappingService()
        {
            propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
        }

        /// <summary>
        /// gets the specific mapping from eg. from AuthorDto to Author
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <returns></returns>
        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
        {

            //get matching mapping
            var matchMapping = propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();

            if (matchMapping.Count() == 1)
                return matchMapping.First()._mappingDictionary;

            throw new Exception($"Cannot find exact property mapping instance for <{typeof(TSource)},{typeof(TDestination)}>");
        }

        public bool ValidateMappingExistsFor<TSource,TDestination>(string fields)
        {
            var propertyMapping = GetPropertyMapping<TSource, TDestination>();

            if (string.IsNullOrEmpty(fields))
                return true;

            //the string is separated by "," split it
            var fieldsAfterSplit = fields.Split(',');

            foreach(var field in fieldsAfterSplit)
            {
                var trimmedField = field.Trim();

                var indexOfFirstSpace = trimmedField.IndexOf(" ");

                var propertyName = indexOfFirstSpace==-1?trimmedField:trimmedField.Remove(indexOfFirstSpace);

                if(!propertyMapping.ContainsKey(propertyName))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
