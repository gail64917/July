using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtCardService.Data;
using DebtCardService.Models;
using ReflectionIT.Mvc.Paging;
using LibraryService.Models.JsonBindings;
using EasyNetQ;
using RabbitModels;
using System.Collections.Concurrent;
using System.Threading;

namespace DebtCardService.Controllers
{
    [Produces("application/json")]
    [Route("api/Librarys")]
    public class LibrarysController : Controller
    {
        private readonly LibraryContext _context;

        const int StringsPerPage = 10;

        public LibrarysController(LibraryContext context)
        {
            _context = context;
        }

        // GET: api/Librarys
        [HttpGet]
        public IEnumerable<Library> GetLibrarys()
        {
            //var Bus = RabbitHutch.CreateBus("host=localhost");
            //ConcurrentStack<Library> LibrarysCollection = new ConcurrentStack<Library>();

            //Bus.Receive<RabbitLibrary>("Library", msg =>
            //{
            //    Library Library = new Library() { LibraryName = msg.LibraryName, CountBooksPerLibrary = msg.CountBooksPerLibrary };
            //    LibrarysCollection.Push(Library);
            //});
            //Thread.Sleep(5000);

            //foreach (Library a in LibrarysCollection)
            //{
            //    _context.Add(a);
            //}
            //_context.SaveChanges();
            return _context.Librarys;
        }


        // GET: api/Librarys/Secret
        [Route("Secret")]
        [HttpGet]
        public IEnumerable<Library> GetLibrarysSecret()
        {
            var Bus = RabbitHutch.CreateBus("host=localhost");
            ConcurrentStack<Library> LibrarysCollection = new ConcurrentStack<Library>();

            Bus.Receive<RabbitLibrary>("Library", msg =>
            {
                Library Library = new Library() { LibraryName = msg.LibraryName, CountBooksPerLibrary = msg.CountBooksPerLibrary };
                LibrarysCollection.Push(Library);
            });
            Thread.Sleep(5000);

            foreach (Library a in LibrarysCollection)
            {
                _context.Add(a);
            }
            _context.SaveChanges();
            return _context.Librarys;
        }


        // GET: api/Librarys/page/{id}
        [HttpGet]
        [Route("page/{page}")]
        public List<Library> GetLibrarys([FromRoute] int page = 1)
        {
            var qry = _context.Librarys.OrderBy(p => p.LibraryName);

            PagingList<Library> LibraryList;
            if (page != 0)
            {
                LibraryList = PagingList.Create(qry, StringsPerPage, page);
            }
            else
            {
                LibraryList = PagingList.Create(qry, _context.Librarys.Count() + 1, 1);
            }

            return LibraryList.ToList();
        }


        // GET: api/Librarys/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLibrary([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var Library = await _context.Librarys.SingleOrDefaultAsync(m => m.ID == id);

            if (Library == null)
            {
                return NotFound();
            }

            return Ok(Library);
        }

        // POST: api/Librarys/Find
        [Route("Find")]
        [HttpPost]
        public async Task<IActionResult> FindByName([FromBody] LibraryNameBinding name)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var Library = await _context.Librarys.FirstOrDefaultAsync(m => m.LibraryName == name.Name);

            if (Library == null)
            {
                return NotFound();
            }

            return Ok(Library);
        }

        // PUT: api/Librarys/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLibrary([FromRoute] int id, [FromBody] Library Library)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != Library.ID)
            {
                return BadRequest();
            }

            _context.Entry(Library).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Accepted(Library);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LibraryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            //return NoContent();
        }

        // POST: api/Librarys
        [HttpPost]
        public async Task<IActionResult> PostLibrary([FromBody] Library Library)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Librarys.Add(Library);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLibrary", new { id = Library.ID }, Library);
        }

        // DELETE: api/Librarys/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLibrary([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var Library = await _context.Librarys.SingleOrDefaultAsync(m => m.ID == id);
            if (Library == null)
            {
                return NotFound();
            }

            _context.Librarys.Remove(Library);
            await _context.SaveChangesAsync();

            return Ok(Library);
        }

        private bool LibraryExists(int id)
        {
            return _context.Librarys.Any(e => e.ID == id);
        }

        // GET: api/Library
        [HttpGet]
        [Route("count")]
        public int GetCountLibrarys()
        {
            return _context.Librarys.Count();
        }
    }
}