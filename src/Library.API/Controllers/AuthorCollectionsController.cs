using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.API.Models;
using Library.API.Services;
using Library.API.Helpers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Library.API.Controllers
{
    [Route("api/authorcollections")]
    public class AuthorCollectionsController : Controller
    {
        private ILibraryRepository _libraryRepository;

        public AuthorCollectionsController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        [HttpPost]
        public IActionResult CreateAuthorCollections([FromBody]IEnumerable<AuthorForCreationDto> authors)
        {
            if (authors == null)
                return BadRequest();

            var authorsCollections = AutoMapper.Mapper.Map<IEnumerable<Entities.Author>>(authors);

            foreach(var item in authorsCollections)
            {
                _libraryRepository.AddAuthor(item);
            }

            if (!_libraryRepository.Save())
                throw new Exception("Not able to add authors");

            var authorsToReturn = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorsCollections);
            var idsAsString = string.Join(",", authorsCollections.Select(a => a.Id));

            return CreatedAtRoute("getAuthorCollection",new { ids=idsAsString},authorsToReturn);
        }

        [HttpGet("({ids})",Name ="getAuthorCollection")]
        public IActionResult GetAuthorCollection([ModelBinder(BinderType =typeof(ArrayModelBinder))]IEnumerable<Guid> ids)
        {
            if (ids == null)
                return BadRequest();

            var authors=_libraryRepository.GetAuthors(ids);

            if (ids.Count() != authors.Count())
                return NotFound();

            var authorsToReturn = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authors);

            return Ok(authorsToReturn);
        }
    }
}
