using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtCardService.Data;
using DebtCardService.Models.DebtCard;
using DebtCardService.Models.JsonBindings;

namespace DebtCardService.Controllers
{
    [Produces("application/json")]
    [Route("api/LibrarySystems")]
    public class LibrarySystemsController : Controller
    {
        private readonly DebtCardContext _context;

        public LibrarySystemsController(DebtCardContext context)
        {
            _context = context;
        }

        // GET: api/LibrarySystems
        [HttpGet]
        public IEnumerable<LibrarySystem> GetLibrarySystems()
        {
            return _context.LibrarySystems;
        }

        // GET: api/LibrarySystems/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLibrarySystem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var LibrarySystem = await _context.LibrarySystems.SingleOrDefaultAsync(m => m.ID == id);

            if (LibrarySystem == null)
            {
                return NotFound();
            }

            return Ok(LibrarySystem);
        }

        // PUT: api/LibrarySystems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLibrarySystem([FromRoute] int id, [FromBody] LibrarySystem LibrarySystem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != LibrarySystem.ID)
            {
                return BadRequest();
            }

            _context.Entry(LibrarySystem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Accepted(LibrarySystem);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LibrarySystemExists(id))
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

        // POST: api/LibrarySystems
        [HttpPost]
        public async Task<IActionResult> PostLibrarySystem([FromBody] LibrarySystem LibrarySystem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.LibrarySystems.Add(LibrarySystem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLibrarySystem", new { id = LibrarySystem.ID }, LibrarySystem);
        }

        // DELETE: api/LibrarySystems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLibrarySystem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var LibrarySystem = await _context.LibrarySystems.SingleOrDefaultAsync(m => m.ID == id);
            if (LibrarySystem == null)
            {
                return NotFound();
            }

            _context.LibrarySystems.Remove(LibrarySystem);
            await _context.SaveChangesAsync();

            return Ok(LibrarySystem);
        }

        // POST: api/LibrarySystems/Find
        [Route("Find")]
        [HttpPost]
        public async Task<IActionResult> FindByName([FromBody] LibrarySystemNameBinding name)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var LibrarySystem = await _context.LibrarySystems.FirstOrDefaultAsync(m => m.LibrarySystemName == name.Name);

            if (LibrarySystem == null)
            {
                return NotFound();
            }

            return Ok(LibrarySystem);
        }

        private bool LibrarySystemExists(int id)
        {
            return _context.LibrarySystems.Any(e => e.ID == id);
        }
    }
}