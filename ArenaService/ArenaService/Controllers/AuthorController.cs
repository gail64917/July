using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookService.Data;
using BookService.Models;
using System.Text;
using BookService.Models.JsonBindings;
using EasyNetQ;
using System.Collections.Concurrent;
using RabbitModels;
using System.Threading;

namespace BookService.Controllers
{
    [Produces("application/json")]
    [Route("api/Authors")]
    public class AuthorController : Controller
    {
        private readonly BookContext _context;

        public AuthorController(BookContext context)
        {
            _context = context;
        }

        // GET: api/Authors/Secret
        [Route("Secret")]
        [HttpGet]
        public IEnumerable<Author> GetAuthorsSecret()
        {
            IEnumerable<Author> Authors = _context.Authors;

            var Bus = RabbitHutch.CreateBus("host=localhost");
            ConcurrentStack<RabbitBookAuthor> BookAuthorCollection = new ConcurrentStack<RabbitBookAuthor>();

            Bus.Receive<RabbitBookAuthor>("BookAuthor", msg =>
            {
                BookAuthorCollection.Push(msg);
            });
            Thread.Sleep(5000);

            foreach (RabbitBookAuthor a in BookAuthorCollection)
            {
                Author c = new Author() { AuthorName = a.AuthorName, AuthorRating = a.AuthorRating };
                _context.Authors.Add(c);
            }
            _context.SaveChanges();

            foreach (RabbitBookAuthor a in BookAuthorCollection)
            {
                int c_id = 0;
                foreach (Author c in _context.Authors)
                {
                    if (a.AuthorName == c.AuthorName)
                        c_id = c.ID;
                }
                
                Book ar = new Book() { BookName = a.BookName, PageCount = a.BookPageCount, AuthorID = c_id};
                _context.Books.Add(ar);
            }
            _context.SaveChanges();

            return Authors;
        }

        // GET: api/Authors
        [HttpGet]
        public IEnumerable<Author> GetAuthors()
        {
            IEnumerable<Author> Authors = _context.Authors;

            //var Bus = RabbitHutch.CreateBus("host=localhost");
            //ConcurrentStack<RabbitBookAuthor> BookAuthorCollection = new ConcurrentStack<RabbitBookAuthor>();

            //Bus.Receive<RabbitBookAuthor>("BookAuthor", msg =>
            //{
            //    BookAuthorCollection.Push(msg);
            //});
            //Thread.Sleep(5000);

            //foreach (RabbitBookAuthor a in BookAuthorCollection)
            //{
            //    Author c = new Author() { AuthorName = a.AuthorName, AuthorRating = a.AuthorRating };
            //    _context.Authors.Add(c);
            //}
            //_context.SaveChanges();

            //foreach (RabbitBookAuthor a in BookAuthorCollection)
            //{
            //    int c_id = 0;
            //    foreach (Author c in _context.Authors)
            //    {
            //        if (a.AuthorName == c.AuthorName)
            //            c_id = c.ID;
            //    }

            //    Book ar = new Book() { BookName = a.BookName, PageCount = a.BookPageCount, AuthorID = c_id };
            //    _context.Books.Add(ar);
            //}
            //_context.SaveChanges();

            return Authors;
        }

        // GET: api/Authors/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuthor([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var Author = await _context.Authors.SingleOrDefaultAsync(m => m.ID == id);

            if (Author == null)
            {
                return NotFound();
            }

            return Ok(Author);
        }

        // PUT: api/Authors/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAuthor([FromRoute] int id, [FromBody] Author Author)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != Author.ID)
            {
                return BadRequest();
            }

            _context.Entry(Author).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Accepted(Author);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        // POST: api/Authors
        [HttpPost]
        public async Task<IActionResult> PostAuthor([FromBody] Author Author)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Authors.Add(Author);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAuthor", new { id = Author.ID }, Author);
        }


        // DELETE: api/Authors/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var Author = await _context.Authors.SingleOrDefaultAsync(m => m.ID == id);
            if (Author == null)
            {
                return NotFound();
            }

            _context.Authors.Remove(Author);
            await _context.SaveChangesAsync();

            return Ok(Author);
        }

        private bool AuthorExists(int id)
        {
            return _context.Authors.Any(e => e.ID == id);
        }


        // POST: api/Authors/Find
        [Route("Find")]
        [HttpPost]
        public async Task<IActionResult> FindByName([FromBody] AuthorBinding AuthorBinding)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var Author = await _context.Authors.FirstOrDefaultAsync(m => m.AuthorName == AuthorBinding.Name);

            if (Author == null)
            {
                return NotFound();
            }

            return Ok(Author);
        }
    }
}