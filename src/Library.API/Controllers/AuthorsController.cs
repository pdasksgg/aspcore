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
        public IActionResult GetAuthors(AuthorsResourceParameters authorResourceParameters)
        {
            if (!_propertyMappingService.ValidateMappingExistsFor<AuthorDto, Author>(authorResourceParameters.OrderBy))
                return BadRequest();

            if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorResourceParameters.Fields))
                return BadRequest();

            IEnumerable<AuthorDto> authors = null;
            var authorsFromRepo = _libraryRepository.GetAuthors(authorResourceParameters);

            var previousPageLink = authorsFromRepo.HasPreviousPage ?
                this.CreateAuthorsResourceUri(authorResourceParameters, ResourceUriType.PeviousPage) : null;

            var nextPageLink = authorsFromRepo.HasNext ?
                this.CreateAuthorsResourceUri(authorResourceParameters, ResourceUriType.NextPage) : null;

            var paginationMetaData = new {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };

            Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetaData));

            authors = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);


            return Ok(authors.ShapeData(authorResourceParameters.Fields));
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters parameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PeviousPage:
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
            return new JsonResult(author.ShapeData(fields));
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

            return CreatedAtRoute("getAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (!_libraryRepository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);

            return NotFound();
        }

        [HttpDelete("{id}")]
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
    }
}
