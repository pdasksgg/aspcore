using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.API.Services;
using Library.API.Models;
using Microsoft.AspNetCore.JsonPatch;
using Library.API.Helpers;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private ILibraryRepository _libraryRepository;
        private ILogger<BooksController> _logger;
        private IUrlHelper _urlHelper;

        public BooksController(ILibraryRepository libraryRepository,
            ILogger<BooksController> logger,IUrlHelper urlHelper)
        {
            _libraryRepository = libraryRepository;
            _logger = logger;
            _urlHelper = urlHelper;

        }

        [HttpGet(Name ="getBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var booksForAuthorRepo = _libraryRepository.GetBooksForAuthor(authorId);
            var booksForAuthor = AutoMapper.Mapper.Map<IEnumerable<BookDto>>(booksForAuthorRepo);

            booksForAuthor = booksForAuthor.Select(book => 
            {
                return this.CreateLinksForBook(book);
            });

            var wrapper = new LinkedCollectionWrapperDto<BookDto>(booksForAuthor);
            
            return Ok(this.CreateLinksForBooks(wrapper));
        }

        [HttpGet("{id}",Name ="getBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId,Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var book = _libraryRepository.GetBookForAuthor(authorId, id);
            if (book == null)
                return NotFound();
            var bookForAuthor = AutoMapper.Mapper.Map<BookDto>(book);

            return Ok(CreateLinksForBook(bookForAuthor));
        }

        [HttpPost(Name ="createBookForAuthor")]
        public IActionResult CreateBookForAuthor(Guid authorId,[FromBody] BookForCreationDto book)
        {
            if (book == null)
                return BadRequest();

            if (book.Title == book.Description)
                ModelState.AddModelError(nameof(BookForCreationDto), "The provided description should be different from title");

            if (!ModelState.IsValid)
                return new UnProcessableEntityObjectResult(ModelState);

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookEntity = AutoMapper.Mapper.Map<Entities.Book>(book);

            _libraryRepository.AddBookForAuthor(authorId, bookEntity);

            if (!_libraryRepository.Save())
                throw new Exception($"Creating book for {authorId} failed");

            var bookRes = AutoMapper.Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("getBookForAuthor", new { authorId= authorId, id = bookEntity.Id },CreateLinksForBook(bookRes));
        }

        [HttpDelete("{id}",Name ="deleteBookForAuthor")]
        public IActionResult DeleteBookForAuthor(Guid authorId,Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookFromAuthorRepo = _libraryRepository.GetBookForAuthor(authorId,id);
            if (bookFromAuthorRepo == null)
                return NotFound();

            _libraryRepository.DeleteBook(bookFromAuthorRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"Deleting book for {authorId} failed");

            _logger.LogInformation(100, $"Book {id} for author {authorId} deleted");

            return NoContent();

        }

        /// <summary>
        /// In put we are updating the resource and not entity
        /// </summary>
        /// <param name="authorId"></param>
        /// <param name="id"></param>
        /// <param name="updateDto"></param>
        /// <returns></returns>
        [HttpPut("{id}",Name ="updateBookForAuthor")]
        public IActionResult UpdateBookForAuthor(Guid authorId,Guid id,[FromBody]BookForUpdateDto updateDto)
        {
            if (updateDto == null)
                return BadRequest();

            if (updateDto.Title == updateDto.Description)
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from title");

            if (!ModelState.IsValid)
                return new UnProcessableEntityObjectResult(ModelState);

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookFromAuthorRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookFromAuthorRepo == null)
            {
                //return NotFound(); To allow Upserting we have to comment, here the resource identifier is set by client and that's the reason we choose Guid
                var bookToAdd = AutoMapper.Mapper.Map<Entities.Book>(updateDto);
                bookToAdd.Id = id;
                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);


                if (!_libraryRepository.Save())
                    throw new Exception($"Upserting book {id} for author {authorId} failed");

                var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("getBookForAuthor",new { authorId=authorId, id=bookToReturn.Id},bookToReturn);
            }

            AutoMapper.Mapper.Map(updateDto, bookFromAuthorRepo);

            _libraryRepository.UpdateBookForAuthor(bookFromAuthorRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"Updating book {id} for author {authorId} failed");

            return NoContent();

            //return Ok(AutoMapper.Mapper.Map<BookForUpdateDto>(bookFromAuthorRepo));
        }
        
        [HttpPatch("{id}",Name ="partiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId,Guid id,[FromBody] JsonPatchDocument<BookForUpdateDto> patchDocument)
        {
            if (patchDocument == null)
                return BadRequest();

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookFromAuthorRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookFromAuthorRepo == null)
            {
                //return NotFound();//allow upsert
                var bookDto = new BookForUpdateDto();
                patchDocument.ApplyTo(bookDto);

                var bookToAdd = AutoMapper.Mapper.Map<Entities.Book>(bookDto);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!_libraryRepository.Save())
                    throw new Exception($"Upserting book {id} for author {authorId} failed");

                var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("getBookForAuthor", new { authorId = authorId, id = bookToReturn.Id }, bookToReturn);
            }

            var bookToPatch = AutoMapper.Mapper.Map<BookForUpdateDto>(bookFromAuthorRepo);

            patchDocument.ApplyTo(bookToPatch,ModelState);
            //patchDocument.ApplyTo(bookToPatch);

            if (bookToPatch.Title == bookToPatch.Description)
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from title");

            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid)
                return new UnProcessableEntityObjectResult(ModelState);

            AutoMapper.Mapper.Map(bookToPatch, bookFromAuthorRepo);

            _libraryRepository.UpdateBookForAuthor(bookFromAuthorRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"Updating Patch for book {id} for author {authorId} failed");

            return NoContent();
        }

        private BookDto CreateLinksForBook(BookDto book)
        {
            book.Links.Add(new LinkDto(_urlHelper.Link("getBookForAuthor", new { id = book.Id }), "self", "GET"));
            book.Links.Add(new LinkDto(_urlHelper.Link("deleteBookForAuthor", new { id = book.Id }), "delete_book", "DELETE"));
            book.Links.Add(new LinkDto(_urlHelper.Link("updateBookForAuthor", new { id = book.Id }), "update_book", "PUT"));
            book.Links.Add(new LinkDto(_urlHelper.Link("partiallyUpdateBookForAuthor", new { id = book.Id }), "partially_update_book", "PATCH"));
            return book;
        }

        private LinkedCollectionWrapperDto<BookDto> CreateLinksForBooks(LinkedCollectionWrapperDto<BookDto> booksWrapper)
        {
            booksWrapper.Links.Add(new LinkDto(_urlHelper.Link("getBooksForAuthor", new {  }), "self", "GET"));
            return booksWrapper;
        }
    }
}
