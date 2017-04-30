using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.API.Services;
using Library.API.Models;
using Library.API.Helpers;
using Microsoft.AspNetCore.Http;
using Library.API.Entities;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private ILibraryRepository _libraryRepository;
        IUrlHelper _urlHelper;
        private IPropertyMappingService _propertyMappingService;
        private ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository, IUrlHelper urlHelper, 
            IPropertyMappingService propertyMappingService,ITypeHelperService typeHelperService)
        {
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;

        }

        [HttpGet(Name = "getAuthors")]
        [HttpHead]
        public IActionResult GetAuthors(AuthorsResourceParameters authorResourceParameters)
        {
            if (!_propertyMappingService.ValidateMappingExistsFor<AuthorDto, Author>(authorResourceParameters.OrderBy))
                return BadRequest();

            if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorResourceParameters.Fields))
                return BadRequest();

            IEnumerable<AuthorDto> authors = null;
            var authorsFromRepo = _libraryRepository.GetAuthors(authorResourceParameters);

            //var previousPageLink = authorsFromRepo.HasPreviousPage ?
            //    this.CreateAuthorsResourceUri(authorResourceParameters, ResourceUriType.PreviousPage) : null;

            //var nextPageLink = authorsFromRepo.HasNext ?
            //    this.CreateAuthorsResourceUri(authorResourceParameters, ResourceUriType.NextPage) : null;

            var paginationMetaData = new {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                //previousPageLink = previousPageLink,
                //nextPageLink = nextPageLink
            };

            Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetaData));

            authors = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);
            var links = this.CreateLinksForAuthors(authorResourceParameters, authorsFromRepo.HasNext, authorsFromRepo.HasPreviousPage);
            var shapedAuthors = authors.ShapeData(authorResourceParameters.Fields);
            var shapedAuthorwithLink = shapedAuthors.Select(item => {
                var authorAsDictionary = item as IDictionary<string, object>;
                var authorLink = this.CreateLinksForAuthor((Guid)authorAsDictionary["Id"], authorResourceParameters.Fields);
                authorAsDictionary.Add("links", authorLink);
                return authorAsDictionary;
            });

            var linkedCollectionResource = new { value=shapedAuthorwithLink,links=links };

            return Ok(linkedCollectionResource);
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters parameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("getAuthors", new
                    {
                        fields=parameters.Fields,
                        orderBy=parameters.OrderBy,
                        searchQuery=parameters.SearchQuery,
                        genre=parameters.Genre,
                        pageNumber = parameters.PageNumber - 1,
                        pageSize = parameters.PageSize

                    });

                case ResourceUriType.NextPage:
                    return _urlHelper.Link("getAuthors", new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        searchQuery = parameters.SearchQuery,
                        genre = parameters.Genre,
                        pageNumber = parameters.PageNumber + 1,
                        pageSize = parameters.PageSize

                    });
                case ResourceUriType.Current:
                default:
                    return _urlHelper.Link("getAuthors", new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        searchQuery = parameters.SearchQuery,
                        genre = parameters.Genre,
                        pageNumber = parameters.PageNumber,
                        pageSize = parameters.PageSize

                    });
            }
        }

        [HttpGet("{id}", Name = "getAuthor")]
        public IActionResult GetAuthor(Guid id,[FromQuery]string fields)
        {
            if (!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
                return BadRequest();

            var authorSingle = _libraryRepository.GetAuthor(id);
            if (authorSingle == null)
                return NotFound();

            var author = AutoMapper.Mapper.Map<AuthorDto>(authorSingle);
            var link = this.CreateLinksForAuthor(id, fields);

            var linkedResourceToReturn = author.ShapeData(fields) as IDictionary<string,object>;
            linkedResourceToReturn.Add("links", link);
            //return new JsonResult(author.ShapeData(fields));
            return Ok(linkedResourceToReturn);
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
                return BadRequest();

            var authorEntity = AutoMapper.Mapper.Map<Entities.Author>(author);
            _libraryRepository.AddAuthor(authorEntity);
            if (!_libraryRepository.Save())
                throw new Exception("Creating an author failed on save");

            var authorToReturn = AutoMapper.Mapper.Map<AuthorDto>(authorEntity);


            var link = this.CreateLinksForAuthor(authorToReturn.Id, null);
            var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", link);
            //return new JsonResult(author.ShapeData(fields));
           

            return CreatedAtRoute("getAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (!_libraryRepository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);

            return NotFound();
        }

        [HttpDelete("{id}",Name ="deleteAuthor")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var author = _libraryRepository.GetAuthor(id);

            if (author == null)
                return NotFound();

            _libraryRepository.DeleteAuthor(author);

            if (!_libraryRepository.Save())
                throw new Exception($"Failed to delete author {id}");

            return NoContent();

        }

        [HttpOptions]
        public IActionResult GetAuthorsOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST");
            return Ok();
        }

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id,string fields)
        {
            var links = new List<LinkDto>();

            if(string.IsNullOrEmpty(fields))
            {
                links.Add(new LinkDto(_urlHelper.Link("getAuthor", new { id = id }), "self", "GET"));
            }
            else
            {
                links.Add(new LinkDto(_urlHelper.Link("getAuthor", new { id = id,fields=fields }), "self", "GET"));
            }

            links.Add(new LinkDto(_urlHelper.Link("deleteAuthor", new { id = id}), "delete_author", "DELETE"));

            links.Add(new LinkDto(_urlHelper.Link("createBookForAuthor", new { authorid = id }), "create_book_for_author", "POST"));

            links.Add(new LinkDto(_urlHelper.Link("getBooksForAuthor", new { authorid = id }), "books", "GET"));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters, bool hasNext,bool hasPrevious)
        {
            var links = new List<LinkDto>();
            // self 
            links.Add(
               new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
               ResourceUriType.Current)
               , "self", "GET"));

            if (hasNext)
            {
                links.Add(
                  new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                  ResourceUriType.NextPage),
                  "nextPage", "GET"));
            }

            if (hasPrevious)
            {
                links.Add(
                    new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.PreviousPage),
                    "previousPage", "GET"));
            }
            return links;
        }

    }
}
