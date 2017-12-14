using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtCardService.Data;
using DebtCardService.Models.DebtCard;
using ReflectionIT.Mvc.Paging;
using DebtCardService.Models;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using static DebtCardService.Logger.Logger;
using DebtCardService.Models.JsonBindings;
using EasyNetQ;
using System.Collections.Concurrent;
using RabbitModels;

namespace DebtCardService.Controllers
{
    [Produces("application/json")]
    [Route("api/DebtCards")]
    public class DebtCardsController : Controller
    {
        private const string URLLibraryService = "http://localhost:61883";
        private const string URLBookService = "http://localhost:58349";

        const int StringsPerPage = 10;

        private readonly DebtCardContext _context;

        public DebtCardsController(DebtCardContext context)
        {
            _context = context;
        }

        // GET: api/DebtCards
        [HttpGet]
        public List<DebtCard> GetDebtCardsAll()
        {
            foreach (DebtCard a in _context.DebtCards)
            {
                _context.Entry(a).Navigation("LibrarySystem").Load();
            }
            return _context.DebtCards.ToList();
        }

        // GET: api/DebtCards/page/1
        [HttpGet]
        [Route("page/{page}")]
        public List<DebtCard> GetDebtCards([FromRoute] int page = 1)
        {
            var qry = _context.DebtCards.OrderBy(p => p.ID);
            foreach (DebtCard a in qry)
            {
                _context.Entry(a).Navigation("LibrarySystem").Load();
            }

            PagingList<DebtCard> DebtCardsList;
            if (page != 0)
            {

                DebtCardsList = PagingList.Create(qry, StringsPerPage, page);
            }
            else
            {
                DebtCardsList = PagingList.Create(qry, _context.DebtCards.Count() + 1, 1);
            }

            return DebtCardsList.ToList();
        }


        // GET: api/DebtCards/Valid/Secret
        [HttpGet]
        [Route("Valid/Secret")]
        public async Task<IActionResult> GetValidSecretDebtCards()
        {
            var Bus = RabbitHutch.CreateBus("host=localhost");
            ConcurrentStack<RabbitDebtCardLibrarySystem> DebtCardLibrarySystemCollection = new ConcurrentStack<RabbitDebtCardLibrarySystem>();

            Bus.Receive<RabbitDebtCardLibrarySystem>("DebtCardLibrarySystem", msg =>
            {
                DebtCardLibrarySystemCollection.Push(msg);
            });
            Thread.Sleep(5000);

            foreach (RabbitDebtCardLibrarySystem cs in DebtCardLibrarySystemCollection)
            {
                LibrarySystem s = new LibrarySystem() { LibrarySystemName = cs.LibrarySystemName };
                _context.LibrarySystems.Add(s);
            }
            _context.SaveChanges();

            foreach (RabbitDebtCardLibrarySystem cs in DebtCardLibrarySystemCollection)
            {
                int c_id = 0;
                foreach (LibrarySystem s in _context.LibrarySystems)
                {
                    if (cs.LibrarySystemName == s.LibrarySystemName)
                        c_id = s.ID;
                }

                DebtCard c = new DebtCard() { BookName = cs.BookName, LibraryName = cs.LibraryName, AuthorName = cs.AuthorName, Date = cs.Date, PaymentDefault = cs.PaymentDefault, CardName = cs.CardName, PaymentPerDay = cs.PaymentPerDay, LibrarySystemID = c_id };
                _context.DebtCards.Add(c);
            }
            _context.SaveChanges();



            var qry = _context.DebtCards.OrderBy(p => p.ID);
            foreach (DebtCard a in qry)
            {
                _context.Entry(a).Navigation("LibrarySystem").Load();
            }

            //Проверить, что: 
            // 1) кол-во билетов меньше, чем вместительность арены
            // 2) город существует и корректный
            // 3) арена существует и из этого города
            // 4) артист корректный

            //
            //Вытаскиваем все Арены
            //
            List<Book> QryBooks = new List<Book>();
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            byte[] responseMessage;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/Books";
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: BookService \r\nGET: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var Books = await response.Content.ReadAsStringAsync();
                    QryBooks = JsonConvert.DeserializeObject<List<Book>>(Books);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                }
                await LogQuery(request, responseString, responseMessage);
            }

            //
            //Вытаскиваем всех Артистов
            //
            List<Library> QryLibrarys = new List<Library>();
            var corrId2 = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request2;
            byte[] responseMessage2;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString2 = "api/Librarys";
                HttpResponseMessage response2 = await client.GetAsync(requestString2);
                request2 = "SERVICE: BookService \r\nGET: " + URLLibraryService + "/" + requestString2 + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString2 = response2.Headers.ToString() + "\nStatus: " + response2.StatusCode.ToString();
                if (response2.IsSuccessStatusCode)
                {
                    responseMessage2 = await response2.Content.ReadAsByteArrayAsync();
                    var Librarys = await response2.Content.ReadAsStringAsync();
                    QryLibrarys = JsonConvert.DeserializeObject<List<Library>>(Librarys);
                }
                else
                {
                    responseMessage2 = Encoding.UTF8.GetBytes(response2.ReasonPhrase);
                }
                await LogQuery(request2, responseString2, responseMessage2);
            }

            //
            //Проверить на валидность все концерты
            //
            List<DebtCard> ValidDebtCards = new List<DebtCard>();
            foreach(DebtCard c in qry)
            {
                //находим название Арены с таким же, как в концерте
                Book FindedBook;
                foreach(Book a in QryBooks)
                {
                    if (a.BookName == c.BookName)
                    {
                        FindedBook = a;
                        if (a.PageCount >= c.PaymentPerDay)
                        {
                            if (a.Author.AuthorName == c.AuthorName)
                            {
                                Library Library = QryLibrarys.Where(x => x.LibraryName == c.LibraryName).FirstOrDefault();
                                if (Library != null)
                                {
                                    ValidDebtCards.Add(c);
                                }
                            }
                        }
                    }
                }
            }
            return Ok(ValidDebtCards);
        }

        // GET: api/DebtCards
        [HttpGet]
        [Route("Valid")]
        public async Task<IActionResult> GetValidDebtCards()
        {
            //var Bus = RabbitHutch.CreateBus("host=localhost");
            //ConcurrentStack<RabbitDebtCardLibrarySystem> DebtCardLibrarySystemCollection = new ConcurrentStack<RabbitDebtCardLibrarySystem>();

            //Bus.Receive<RabbitDebtCardLibrarySystem>("DebtCardLibrarySystem", msg =>
            //{
            //    DebtCardLibrarySystemCollection.Push(msg);
            //});
            //Thread.Sleep(5000);

            //foreach (RabbitDebtCardLibrarySystem cs in DebtCardLibrarySystemCollection)
            //{
            //    LibrarySystem s = new LibrarySystem() { LibrarySystemName = cs.LibrarySystemName };
            //    _context.LibrarySystems.Add(s);
            //}
            //_context.SaveChanges();

            //foreach (RabbitDebtCardLibrarySystem cs in DebtCardLibrarySystemCollection)
            //{
            //    int c_id = 0;
            //    foreach (LibrarySystem s in _context.LibrarySystems)
            //    {
            //        if (cs.LibrarySystemName == s.LibrarySystemName)
            //            c_id = s.ID;
            //    }

            //    DebtCard c = new DebtCard() { BookName = cs.BookName, LibraryName = cs.LibraryName, AuthorName = cs.AuthorName, Date = cs.Date, PaymentDefault = cs.PaymentDefault, CardName = cs.CardName, PaymentPerDay = cs.PaymentPerDay, LibrarySystemID = c_id };
            //    _context.DebtCards.Add(c);
            //}
            //_context.SaveChanges();



            var qry = _context.DebtCards.OrderBy(p => p.ID);
            foreach (DebtCard a in qry)
            {
                _context.Entry(a).Navigation("LibrarySystem").Load();
            }

            //Проверить, что: 
            // 1) кол-во билетов меньше, чем вместительность арены
            // 2) город существует и корректный
            // 3) арена существует и из этого города
            // 4) артист корректный

            //
            //Вытаскиваем все Арены
            //
            List<Book> QryBooks = new List<Book>();
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            byte[] responseMessage;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/Books";
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: BookService \r\nGET: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var Books = await response.Content.ReadAsStringAsync();
                    QryBooks = JsonConvert.DeserializeObject<List<Book>>(Books);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                }
                await LogQuery(request, responseString, responseMessage);
            }

            //
            //Вытаскиваем всех Артистов
            //
            List<Library> QryLibrarys = new List<Library>();
            var corrId2 = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request2;
            byte[] responseMessage2;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString2 = "api/Librarys";
                HttpResponseMessage response2 = await client.GetAsync(requestString2);
                request2 = "SERVICE: BookService \r\nGET: " + URLLibraryService + "/" + requestString2 + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString2 = response2.Headers.ToString() + "\nStatus: " + response2.StatusCode.ToString();
                if (response2.IsSuccessStatusCode)
                {
                    responseMessage2 = await response2.Content.ReadAsByteArrayAsync();
                    var Librarys = await response2.Content.ReadAsStringAsync();
                    QryLibrarys = JsonConvert.DeserializeObject<List<Library>>(Librarys);
                }
                else
                {
                    responseMessage2 = Encoding.UTF8.GetBytes(response2.ReasonPhrase);
                }
                await LogQuery(request2, responseString2, responseMessage2);
            }

            //
            //Проверить на валидность все концерты
            //
            List<DebtCard> ValidDebtCards = new List<DebtCard>();
            foreach (DebtCard c in qry)
            {
                //находим название Арены с таким же, как в концерте
                Book FindedBook;
                foreach (Book a in QryBooks)
                {
                    if (a.BookName == c.BookName)
                    {
                        FindedBook = a;
                        if (a.PageCount >= c.PaymentPerDay)
                        {
                            if (a.Author.AuthorName == c.AuthorName)
                            {
                                Library Library = QryLibrarys.Where(x => x.LibraryName == c.LibraryName).FirstOrDefault();
                                if (Library != null)
                                {
                                    ValidDebtCards.Add(c);
                                }
                            }
                        }
                    }
                }
            }
            return Ok(ValidDebtCards);
        }

        // GET: api/DebtCards/all?Valid=1&page=1
        [HttpGet]
        [Route("All")]
        public async Task<IActionResult> GetValidDebtCardsPages(bool? valid=true, int page=1)
        {
            if (valid == false)
            {
                foreach (DebtCard a in _context.DebtCards)
                {
                    _context.Entry(a).Navigation("LibrarySystem").Load();
                }

                PagingList<DebtCard> DebtCardsList;
                if (page != 0)
                {
                    DebtCardsList = PagingList.Create(_context.DebtCards.ToList(), StringsPerPage, page);
                }
                else
                {
                    DebtCardsList = PagingList.Create(_context.DebtCards.ToList(), _context.DebtCards.ToList().Count() + 1, 1);
                }

                return Ok(DebtCardsList.ToList());
            }
            else
            {
                var qry = _context.DebtCards.OrderBy(p => p.ID);
                foreach (DebtCard a in qry)
                {
                    _context.Entry(a).Navigation("LibrarySystem").Load();
                }

                //
                //Вытаскиваем все Арены
                //
                List<Book> QryBooks = new List<Book>();
                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                string request;
                byte[] responseMessage;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(URLBookService);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string requestString = "api/Books";
                    HttpResponseMessage response = await client.GetAsync(requestString);
                    request = "SERVICE: BookService \r\nGET: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                    string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                    if (response.IsSuccessStatusCode)
                    {
                        responseMessage = await response.Content.ReadAsByteArrayAsync();
                        var Books = await response.Content.ReadAsStringAsync();
                        QryBooks = JsonConvert.DeserializeObject<List<Book>>(Books);
                    }
                    else
                    {
                        responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    }
                    await LogQuery(request, responseString, responseMessage);
                }

                //
                //Вытаскиваем всех Артистов
                //
                List<Library> QryLibrarys = new List<Library>();
                var corrId2 = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                string request2;
                byte[] responseMessage2;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(URLLibraryService);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string requestString2 = "api/Librarys";
                    HttpResponseMessage response2 = await client.GetAsync(requestString2);
                    request2 = "SERVICE: BookService \r\nGET: " + URLLibraryService + "/" + requestString2 + "\r\n" + client.DefaultRequestHeaders.ToString();
                    string responseString2 = response2.Headers.ToString() + "\nStatus: " + response2.StatusCode.ToString();
                    if (response2.IsSuccessStatusCode)
                    {
                        responseMessage2 = await response2.Content.ReadAsByteArrayAsync();
                        var Librarys = await response2.Content.ReadAsStringAsync();
                        QryLibrarys = JsonConvert.DeserializeObject<List<Library>>(Librarys);
                    }
                    else
                    {
                        responseMessage2 = Encoding.UTF8.GetBytes(response2.ReasonPhrase);
                    }
                    await LogQuery(request2, responseString2, responseMessage2);
                }

                //
                //Проверить на валидность все концерты
                //
                List<DebtCard> ValidDebtCards = new List<DebtCard>();
                foreach (DebtCard c in qry)
                {
                    //находим название Арены с таким же, как в концерте
                    Book FindedBook;
                    foreach (Book a in QryBooks)
                    {
                        if (a.BookName == c.BookName)
                        {
                            FindedBook = a;
                            if (a.PageCount >= c.PaymentPerDay)
                            {
                                if (a.Author.AuthorName == c.AuthorName)
                                {
                                    Library Library = QryLibrarys.Where(x => x.LibraryName == c.LibraryName).FirstOrDefault();
                                    if (Library != null)
                                    {
                                        ValidDebtCards.Add(c);
                                    }
                                }
                            }
                        }
                    }
                }

                PagingList<DebtCard> DebtCardsList;
                if (page != 0)
                {
                    DebtCardsList = PagingList.Create(ValidDebtCards, StringsPerPage, page);
                }
                else
                {
                    DebtCardsList = PagingList.Create(ValidDebtCards, ValidDebtCards.Count() + 1, 1);
                }

                return Ok(DebtCardsList.ToList());
            }
        }

        // GET: api/DebtCards/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDebtCard([FromRoute] int id)
        {
            var qry = _context.DebtCards.OrderBy(p => p.ID);
            foreach (DebtCard a in qry)
            {
                _context.Entry(a).Navigation("LibrarySystem").Load();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var DebtCard = await _context.DebtCards.SingleOrDefaultAsync(m => m.ID == id);

            if (DebtCard == null)
            {
                return NotFound();
            }

            return Ok(DebtCard);
        }

        // PUT: api/DebtCards/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDebtCard([FromRoute] int id, [FromBody] DebtCard DebtCard)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != DebtCard.ID)
            {
                return BadRequest();
            }

            _context.Entry(DebtCard).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Accepted(DebtCard);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DebtCardExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // POST: api/DebtCards
        [HttpPost]
        public async Task<IActionResult> PostDebtCard([FromBody] DebtCard DebtCard)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.DebtCards.Add(DebtCard);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDebtCard", new { id = DebtCard.ID }, DebtCard);
        }

        // DELETE: api/DebtCards/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDebtCard([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var DebtCard = await _context.DebtCards.SingleOrDefaultAsync(m => m.ID == id);

            _context.Entry(DebtCard).Navigation("LibrarySystem").Load();

            if (DebtCard == null)
            {
                return NotFound();
            }

            _context.DebtCards.Remove(DebtCard);
            await _context.SaveChangesAsync();

            return Ok(DebtCard);
        }

        private bool DebtCardExists(int id)
        {
            return _context.DebtCards.Any(e => e.ID == id);
        }

        // GET: api/DebtCards
        [HttpGet]
        [Route("count")]
        public async Task<IActionResult> GetCountLibrarys()
        {
            var qry = _context.DebtCards.OrderBy(p => p.ID);
            foreach (DebtCard a in qry)
            {
                _context.Entry(a).Navigation("LibrarySystem").Load();
            }

            //Проверить, что: 
            // 1) кол-во билетов меньше, чем вместительность арены
            // 2) город существует и корректный
            // 3) арена существует и из этого города
            // 4) артист корректный

            //
            //Вытаскиваем все Арены
            //
            List<Book> QryBooks = new List<Book>();
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            byte[] responseMessage;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/Books";
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: BookService \r\nGET: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var Books = await response.Content.ReadAsStringAsync();
                    QryBooks = JsonConvert.DeserializeObject<List<Book>>(Books);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                }
                await LogQuery(request, responseString, responseMessage);
            }

            //
            //Вытаскиваем всех Артистов
            //
            List<Library> QryLibrarys = new List<Library>();
            var corrId2 = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request2;
            byte[] responseMessage2;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString2 = "api/Librarys";
                HttpResponseMessage response2 = await client.GetAsync(requestString2);
                request2 = "SERVICE: BookService \r\nGET: " + URLLibraryService + "/" + requestString2 + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString2 = response2.Headers.ToString() + "\nStatus: " + response2.StatusCode.ToString();
                if (response2.IsSuccessStatusCode)
                {
                    responseMessage2 = await response2.Content.ReadAsByteArrayAsync();
                    var Librarys = await response2.Content.ReadAsStringAsync();
                    QryLibrarys = JsonConvert.DeserializeObject<List<Library>>(Librarys);
                }
                else
                {
                    responseMessage2 = Encoding.UTF8.GetBytes(response2.ReasonPhrase);
                }
                await LogQuery(request2, responseString2, responseMessage2);
            }

            //
            //Проверить на валидность все концерты
            //
            List<DebtCard> ValidDebtCards = new List<DebtCard>();
            foreach (DebtCard c in qry)
            {
                //находим название Арены с таким же, как в концерте
                Book FindedBook;
                foreach (Book a in QryBooks)
                {
                    if (a.BookName == c.BookName)
                    {
                        FindedBook = a;
                        if (a.PageCount >= c.PaymentPerDay)
                        {
                            if (a.Author.AuthorName == c.AuthorName)
                            {
                                Library Library = QryLibrarys.Where(x => x.LibraryName == c.LibraryName).FirstOrDefault();
                                if (Library != null)
                                {
                                    ValidDebtCards.Add(c);
                                }
                            }
                        }
                    }
                }
            }
            return Ok(ValidDebtCards.Count());
        }


        // GET: api/DebtCards/fullCount
        [HttpGet]
        [Route("fullCount")]
        public async Task<IActionResult> GetFullCountLibrarys()
        {
            var qry = _context.DebtCards.OrderBy(p => p.ID);
            foreach (DebtCard a in qry)
            {
                _context.Entry(a).Navigation("LibrarySystem").Load();
            }

            
            return Ok(qry.Count());
        }


        // POST: api/DebtCard/FindLibrarySystem
        [Route("FindLibrarySystem")]
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

        // POST: api/DebtCards/Find
        [Route("Find")]
        [HttpPost]
        public async Task<IActionResult> FindDebtCard([FromBody] DebtCardNameBinding name)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var show = await _context.DebtCards.FirstOrDefaultAsync(m => m.CardName == name.Name);

            if (show == null)
            {
                return NotFound();
            }

            return Ok(show);
        }
    }
}