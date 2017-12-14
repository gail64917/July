using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AggregationService.Models.DebtCardService;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using AggregationService.Models.ModelsForView;
using static AggregationService.Logger.Logger;
using AggregationService.Models.LibraryService;
using AggregationService.Models.BookService;
using Newtonsoft.Json.Linq;
using RestBus.RabbitMQ.Client;
using RestBus.RabbitMQ;
using RabbitModels;
using EasyNetQ;

namespace AggregationService.Controllers
{
    [Route("DebtCard")]
    public class DebtCardController : Controller
    {
        private const string URLLibraryService = "http://localhost:61883";
        private const string URLBookService = "http://localhost:58349";
        private const string URLDebtCardService = "http://localhost:61438";

        // GET: DebtCard
        [Route("Index")]
        [HttpGet("{id?}")]
        public async Task<IActionResult> Index([FromRoute] int id = 1)
        {
            List<DebtCard> result = new List<DebtCard>();
            List<DebtCardInfoFull> FinalResult = new List<DebtCardInfoFull>();
            int count = 0;
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            byte[] responseMessage;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/DebtCards/all?valid=true&page=" + id;
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: DebtCardService \r\nGET: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var DebtCards = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<List<DebtCard>>(DebtCards);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);


                //
                // ПОЛУЧАЕМ КОЛ-ВО СУЩНОСТЕЙ В БД МИКРОСЕРВИСА, ЧТОБЫ УЗНАТЬ, СКОЛЬКО СТРАНИЦ РИСОВАТЬ
                //
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestStringCount = "api/DebtCards/count";
                HttpResponseMessage responseStringsCount = await client.GetAsync(requestStringCount);
                request = "SERVICE: DebtCardService \r\nGET: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = responseStringsCount.Headers.ToString() + "\nStatus: " + responseStringsCount.StatusCode.ToString();

                if (responseStringsCount.IsSuccessStatusCode)
                {
                    responseMessage = await responseStringsCount.Content.ReadAsByteArrayAsync();
                    var countStringsContent = await responseStringsCount.Content.ReadAsStringAsync();
                    count = JsonConvert.DeserializeObject<int>(countStringsContent);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(responseStringsCount.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);
            }

            //
            //Вытаскиваем все Арены
            //
            List<Book> QryBooks = new List<Book>();
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
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
                    return Error();
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
                    return Error();
                }
                await LogQuery(request2, responseString2, responseMessage2);
            }


            //СОЕДИНЯЕМ ИНФОРМАЦИЮ ИЗ 3-х сервисов
            foreach (DebtCard c in result)
            {
                Book Book = QryBooks.Where(x => x.BookName == c.BookName).FirstOrDefault();
                Library Library = QryLibrarys.Where(x => x.LibraryName == c.LibraryName).FirstOrDefault();
                DebtCardInfoFull DebtCardInfoFull = new DebtCardInfoFull() { ID = c.ID, BookName = c.BookName, LibraryName = c.LibraryName, LibrarySystemName = c.LibrarySystem.LibrarySystemName, AuthorName = c.AuthorName, Date = c.Date, PaymentDefault = c.PaymentDefault, CardName = c.CardName, PaymentPerDay = c.PaymentPerDay, BookPageCount = Book.PageCount, AuthorRating = Book.Author.AuthorRating, CountBooksPerLibrary = Library.CountBooksPerLibrary };
                FinalResult.Add(DebtCardInfoFull);
            }
            DebtCardList resultQuery = new DebtCardList() { DebtCardsInfoFull = FinalResult, countDebtCards = count };
            return View(resultQuery);
        }

        // GET: DebtCard/Degradation
        [Route("Degradation")]
        [HttpGet("{id?}")]
        public async Task<IActionResult> GetWithDegradation([FromRoute] int id = 1)
        {
            List<DebtCard> result = new List<DebtCard>();
            List<DebtCardInfoFull> FinalResult = new List<DebtCardInfoFull>();
            int count = 0;
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            byte[] responseMessage;


            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/DebtCards";
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: DebtCardService \r\nGET: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var DebtCards = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<List<DebtCard>>(DebtCards);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);


                //
                // ПОЛУЧАЕМ КОЛ-ВО СУЩНОСТЕЙ В БД МИКРОСЕРВИСА, ЧТОБЫ УЗНАТЬ, СКОЛЬКО СТРАНИЦ РИСОВАТЬ
                //
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestStringCount = "api/DebtCards/fullCount";
                HttpResponseMessage responseStringsCount = await client.GetAsync(requestStringCount);
                request = "SERVICE: DebtCardService \r\nGET: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = responseStringsCount.Headers.ToString() + "\nStatus: " + responseStringsCount.StatusCode.ToString();

                if (responseStringsCount.IsSuccessStatusCode)
                {
                    responseMessage = await responseStringsCount.Content.ReadAsByteArrayAsync();
                    var countStringsContent = await responseStringsCount.Content.ReadAsStringAsync();
                    count = JsonConvert.DeserializeObject<int>(countStringsContent);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(responseStringsCount.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);
            }

            //
            //Вытаскиваем все Арены
            //
            List<Book> QryBooks = new List<Book>();
            try
            {
                QryBooks = new List<Book>();
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
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
                        return Error();
                    }
                    await LogQuery(request, responseString, responseMessage);
                }
            }
            catch
            {

            }

            //
            //Вытаскиваем всех Артистов
            //
            List<Library> QryLibrarys = new List<Library>();
            try
            {
                QryLibrarys = new List<Library>();
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
                        return Error();
                    }
                    await LogQuery(request2, responseString2, responseMessage2);
                }
            }
            catch
            {

            }


            //СОЕДИНЯЕМ ИНФОРМАЦИЮ ИЗ 3-х сервисов
            foreach (DebtCard c in result)
            {
                try
                {
                    DebtCardInfoFull DebtCardInfoFull;
                    if (QryBooks.Any() && QryLibrarys.Any())
                    {
                        Book Book = QryBooks.Where(x => x.BookName == c.BookName).FirstOrDefault();
                        Library Library = QryLibrarys.Where(x => x.LibraryName == c.LibraryName).FirstOrDefault();
                        //DebtCardInfoFull = new DebtCardInfoFull() { ID = c.ID, BookName = c.BookName, LibraryName = c.LibraryName, LibrarySystemName = c.LibrarySystem.LibrarySystemName, AuthorName = c.AuthorName, Date = c.Date, PaymentDefault = c.PaymentDefault, CardName = c.CardName, PaymentPerDay = c.PaymentPerDay, BookPageCount = Book.PageCount, AuthorRating = Book.Author.AuthorRating, CountBooksPerLibrary = Library.CountBooksPerLibrary };
                        DebtCardInfoFull = new DebtCardInfoFull() { ID = c.ID, BookName = Book.BookName, LibraryName = Library.LibraryName, LibrarySystemName = c.LibrarySystem.LibrarySystemName, AuthorName = Book.Author.AuthorName, Date = c.Date, PaymentDefault = c.PaymentDefault, CardName = c.CardName, PaymentPerDay = c.PaymentPerDay, BookPageCount = Book.PageCount, AuthorRating = Book.Author.AuthorRating, CountBooksPerLibrary = Library.CountBooksPerLibrary };
                    }
                    else if (!QryBooks.Any() && QryLibrarys.Any())
                    {
                        Author Author = new Author() { AuthorName = "unknown", AuthorRating = 0 };
                        Book Book = new Book() { BookName = "unknown", PageCount = 0, Author = Author };
                        Library Library = QryLibrarys.Where(x => x.LibraryName == c.LibraryName).FirstOrDefault();
                        //DebtCardInfoFull = new DebtCardInfoFull() { ID = c.ID, BookName = c.BookName, LibraryName = c.LibraryName, LibrarySystemName = c.LibrarySystem.LibrarySystemName, AuthorName = c.AuthorName, Date = c.Date, PaymentDefault = c.PaymentDefault, CardName = c.CardName, PaymentPerDay = c.PaymentPerDay, BookPageCount = Book.PageCount, AuthorRating = Book.Author.AuthorRating, CountBooksPerLibrary = Library.CountBooksPerLibrary };
                        DebtCardInfoFull = new DebtCardInfoFull() { ID = c.ID, BookName = Book.BookName, LibraryName = Library.LibraryName, LibrarySystemName = c.LibrarySystem.LibrarySystemName, AuthorName = Book.Author.AuthorName, Date = c.Date, PaymentDefault = c.PaymentDefault, CardName = c.CardName, PaymentPerDay = c.PaymentPerDay, BookPageCount = Book.PageCount, AuthorRating = Book.Author.AuthorRating, CountBooksPerLibrary = Library.CountBooksPerLibrary };
                    }
                    else if (QryBooks.Any() && !QryLibrarys.Any())
                    {
                        Book Book = QryBooks.Where(x => x.BookName == c.BookName).FirstOrDefault();
                        Library Library = new Library() { LibraryName = "unknow", CountBooksPerLibrary = 0 };
                        //DebtCardInfoFull = new DebtCardInfoFull() { ID = c.ID, BookName = c.BookName, LibraryName = c.LibraryName, LibrarySystemName = c.LibrarySystem.LibrarySystemName, AuthorName = c.AuthorName, Date = c.Date, PaymentDefault = c.PaymentDefault, CardName = c.CardName, PaymentPerDay = c.PaymentPerDay, BookPageCount = Book.PageCount, AuthorRating = Book.Author.AuthorRating, CountBooksPerLibrary = Library.CountBooksPerLibrary };
                        DebtCardInfoFull = new DebtCardInfoFull() { ID = c.ID, BookName = Book.BookName, LibraryName = Library.LibraryName, LibrarySystemName = c.LibrarySystem.LibrarySystemName, AuthorName = Book.Author.AuthorName, Date = c.Date, PaymentDefault = c.PaymentDefault, CardName = c.CardName, PaymentPerDay = c.PaymentPerDay, BookPageCount = Book.PageCount, AuthorRating = Book.Author.AuthorRating, CountBooksPerLibrary = Library.CountBooksPerLibrary };
                    }
                    else
                    {
                        Author Author = new Author() { AuthorName = "unknown", AuthorRating = 0 };
                        Book Book = new Book() { BookName = "unknown", PageCount = 0, Author = Author };
                        Library Library = new Library() { LibraryName = "unknow", CountBooksPerLibrary = 0 };
                        DebtCardInfoFull = new DebtCardInfoFull() { ID = c.ID, BookName = Book.BookName, LibraryName = Library.LibraryName, LibrarySystemName = c.LibrarySystem.LibrarySystemName, AuthorName = Book.Author.AuthorName, Date = c.Date, PaymentDefault = c.PaymentDefault, CardName = c.CardName, PaymentPerDay = c.PaymentPerDay, BookPageCount = Book.PageCount, AuthorRating = Book.Author.AuthorRating, CountBooksPerLibrary = Library.CountBooksPerLibrary };
                    }

                    FinalResult.Add(DebtCardInfoFull);
                }
       
                catch
                {

                }
            }
            DebtCardList resultQuery = new DebtCardList() { DebtCardsInfoFull = FinalResult, countDebtCards = count };
            return View(resultQuery);
        }

        [Route("Error")]
        public IActionResult Error()
        {
            return View("Error");
        }

        [HttpGet("Delete/{id?}")]
        public async Task<IActionResult> Delete(int id)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                string request;
                byte[] responseMessage;
                string route = "api/DebtCards/" + id;
                string requestString = route;
                HttpResponseMessage response = await client.DeleteAsync(route);
                request = "SERVICE: DebtCardService \r\nDELETE: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, responseString, responseMessage);
                    return RedirectToAction(nameof(Index), new { id = 1 });
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, responseString, responseMessage);
                    return View("Error");
                }
            }
        }

        [HttpGet("Edite/{id?}")]
        public async Task<IActionResult> Edite(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            //
            // ПОЛУЧАЕМ СУЩНОСТЬ с ID
            //
            DebtCard DebtCard;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/DebtCards/" + id;
                HttpResponseMessage response = await client.GetAsync(requestString);
                string request = "SERVICE: DebtCardService \r\nGET: " + URLDebtCardService + "/" + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                byte[] responseMessage;
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var DebtCardContent = await response.Content.ReadAsStringAsync();
                    DebtCard = JsonConvert.DeserializeObject<DebtCard>(DebtCardContent);
                    if (DebtCard == null)
                    {
                        await LogQuery(request, responseString, responseMessage);
                        return NotFound();
                    }
                    await LogQuery(request, responseString, responseMessage);
                    DebtCardInfoFull DebtCardInfoFull = new DebtCardInfoFull() { ID = DebtCard.ID, LibraryName = DebtCard.LibraryName,
                                                                                        BookName = DebtCard.BookName, LibrarySystemName = DebtCard.LibrarySystem.LibrarySystemName,
                                                                                        AuthorName = DebtCard.AuthorName, PaymentPerDay = DebtCard.PaymentPerDay,
                                                                                        Date = DebtCard.Date, PaymentDefault = DebtCard.PaymentDefault, CardName = DebtCard.CardName };
                    return View(DebtCardInfoFull);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, responseString, responseMessage);
                    return Error();
                }
            }
        }

        [Route("Edite/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edite([Bind("ID,CardName,PaymentPerDay,PaymentDefault,AuthorName,BookName,LibraryName,Date,LibrarySystemName")] DebtCardInfoFull DebtCardInfoFull)
        {
            if (ModelState.IsValid)
            {
                //Проверяем, валиден ли арена и артист (запрашиваем соответствующие сущности и проверяем)
                //Если валидно - СЕРИАЛИЗУЕМ DebtCardInfoFull и посылаем на DebtCardService
                Book Book;
                Library Library;
                LibrarySystem LibrarySystem;

                //
                //
                //ЗАПРОС АРЕНЫ
                //
                //
                var values = new JObject();
                values.Add("Name", DebtCardInfoFull.BookName);
                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                string request;
                string requestMessage = values.ToString();
                byte[] responseMessage;
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpContent content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                string requestString = "api/Books/Find";
                var response = await client.PostAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "There is no Book with Book-NAME (" + DebtCardInfoFull.BookName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                    //
                    //НЕ НАШЛИ АРЕНУ С ТАКИМ НАЗВАНИЕМ
                    //
                }
                request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    //
                    //НАШЛИ АРЕНУ - ПРОВЕРЯЕМ ВЕРЕН ЛИ ГОРОД И ВМЕСТИТЕЛЬНОСТЬ / БИЛЕТАМ
                    //
                    var BookJson = await response.Content.ReadAsStringAsync();
                    Book = JsonConvert.DeserializeObject<Book>(BookJson);
                    if (Book.PageCount < DebtCardInfoFull.PaymentPerDay)
                    {
                        ResponseMessage message = new ResponseMessage();
                        message.description = "Book PageCount lower than tickets number!";
                        message.message = response;
                        return View("Error", message);
                    }
                    if (Book.Author.AuthorName != DebtCardInfoFull.AuthorName)
                    {
                        ResponseMessage message = new ResponseMessage();
                        message.description = "This Book is not in this Author!";
                        message.message = response;
                        return View("Error", message);
                    }
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Book NOT FOUND";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                    //
                    //НЕ НАШЛИ АРЕНУ С ТАКИМ НАЗВАНИЕМ
                    //
                }


                //
                //
                //ЗАПРОС АРТИСТА
                //
                //
                values = new JObject();
                values.Add("Name", DebtCardInfoFull.LibraryName);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Librarys/Find";
                response = await client.PostAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "There is no Library with Library-NAME (" + DebtCardInfoFull.LibraryName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                    //
                    //НЕ НАШЛИ Library С ТАКИМ NAME
                    //
                }
                request = "SERVICE: LibraryService \r\nPOST: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    //
                    //НАШЛИ Library - УСПЕХ
                    //
                    var LibraryJson = await response.Content.ReadAsStringAsync();
                    Library = JsonConvert.DeserializeObject<Library>(LibraryJson);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Library NOT FOUND";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                    //
                    //НЕ НАШЛИ АРЕНУ С ТАКИМ НАЗВАНИЕМ
                    //
                }

                //
                //Если ошибки не произошло - пихаем концерт в концерты
                //

                //УЗНАЕМ ПО LibrarySystemName номер LibrarySystemID или ошибку, если ошибка
                //
                //
                //ЗАПРОС LibrarySystem'a
                //
                //
                values = new JObject();
                values.Add("Name", DebtCardInfoFull.LibrarySystemName);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/DebtCards/FindLibrarySystem";
                response = await client.PostAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "There is no LibrarySystem with LibrarySystemName (" + DebtCardInfoFull.LibrarySystemName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                    //
                    //НЕ НАШЛИ LibrarySystem С ТАКИМ NAME
                    //
                }
                request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    //
                    //НАШЛИ LibrarySystem - УСПЕХ
                    //
                    var LibrarySystemJson = await response.Content.ReadAsStringAsync();
                    LibrarySystem = JsonConvert.DeserializeObject<LibrarySystem>(LibrarySystemJson);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "LibrarySystem NOT FOUND";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                    //
                    //НЕ НАШЛИ LibrarySystem С ТАКИМ НАЗВАНИЕМ
                    //
                }

                //СЕРИАЛИЗУЕМ DebtCard и посылаем на DebtCardService
                values = new JObject();
                values.Add("ID", DebtCardInfoFull.ID);
                values.Add("CardName", DebtCardInfoFull.CardName);
                values.Add("PaymentPerDay", DebtCardInfoFull.PaymentPerDay);
                values.Add("PaymentDefault", DebtCardInfoFull.PaymentDefault);
                values.Add("AuthorName", DebtCardInfoFull.AuthorName);
                values.Add("BookName", DebtCardInfoFull.BookName);
                values.Add("LibraryName", DebtCardInfoFull.LibraryName);
                values.Add("Date", DebtCardInfoFull.Date);
                values.Add("LibrarySystemID", LibrarySystem.ID);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/DebtCards/"+DebtCardInfoFull.ID;
                response = await client.PutAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "Error occur while adding DebtCard (" + DebtCardInfoFull.ID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: DebtCardService \r\nPUT: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "CANNOT UPDATE DebtCard";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
            }
            else
            {
                return View();
            }
        }

        [HttpGet("EditeAll/{id?}")]
        public async Task<IActionResult> EditeAll(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Book Book;
            Library Library;
            LibrarySystem LibrarySystem;
            DebtCard DebtCard;
            Author Author;

            DebtCardInfoFullWithId result;

            //
            //
            //Запрос КОНЦЕРТА
            //
            //
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            byte[] responseMessage;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URLDebtCardService);
            string requestString = "api/DebtCards/"+id;
            var response = await client.GetAsync(requestString);
            if ((int)response.StatusCode == 500)
            {
                string description = "There is no DebtCard with Id (" + id + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ DebtCard С ТАКИМ НАЗВАНИЕМ
                //
            }
            request = "SERVICE: DebtCardService \r\nGET: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, responseString, responseMessage);
                //
                //НАШЛИ DebtCard
                //
                var DebtCardJson = await response.Content.ReadAsStringAsync();
                DebtCard = JsonConvert.DeserializeObject<DebtCard>(DebtCardJson);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, responseString, responseMessage);
                string description = "DebtCard NOT FOUND";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ DebtCard С ТАКИМ НАЗВАНИЕМ
                //
            }

            //
            //
            //ЗАПРОС АРЕНЫ
            //
            //
            var values = new JObject();
            values.Add("Name", DebtCard.BookName);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLBookService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpContent content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/Books/Find";
            response = await client.PostAsJsonAsync(requestString, values);
            if ((int)response.StatusCode == 500)
            {
                string description = "There is no Book with Book-NAME (" + DebtCard.BookName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ АРЕНУ С ТАКИМ НАЗВАНИЕМ
                //
            }
            request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                //
                //НАШЛИ АРЕНУ - ПРОВЕРЯЕМ ВЕРЕН ЛИ ГОРОД И ВМЕСТИТЕЛЬНОСТЬ / БИЛЕТАМ
                //
                var BookJson = await response.Content.ReadAsStringAsync();
                Book = JsonConvert.DeserializeObject<Book>(BookJson);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Book NOT FOUND";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ АРЕНУ С ТАКИМ НАЗВАНИЕМ
                //
            }


            //
            //
            //ЗАПРОС АРТИСТА
            //
            //
            values = new JObject();
            values.Add("Name", DebtCard.LibraryName);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLLibraryService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/Librarys/Find";
            response = await client.PostAsJsonAsync(requestString, values);
            if ((int)response.StatusCode == 500)
            {
                string description = "There is no Library with Library-NAME (" + DebtCard.LibraryName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ Library С ТАКИМ NAME
                //
            }
            request = "SERVICE: LibraryService \r\nPOST: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                //
                //НАШЛИ Library - УСПЕХ
                //
                var LibraryJson = await response.Content.ReadAsStringAsync();
                Library = JsonConvert.DeserializeObject<Library>(LibraryJson);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Library NOT FOUND";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ АRTIST С ТАКИМ NAME
                //
            }

            Author = Book.Author;
            LibrarySystem = DebtCard.LibrarySystem;

            result = new DebtCardInfoFullWithId()
            {
                ID = DebtCard.ID,
                CardName = DebtCard.CardName,
                PaymentDefault = DebtCard.PaymentDefault,
                Date = DebtCard.Date,
                PaymentPerDay = DebtCard.PaymentPerDay,
                LibrarySystemID = LibrarySystem.ID,
                LibrarySystemName = LibrarySystem.LibrarySystemName,
                AuthorID = Author.ID,
                AuthorName = Author.AuthorName,
                AuthorRating = Author.AuthorRating,
                BookID = Book.ID,
                BookPageCount = Book.PageCount,
                BookName = Book.BookName,
                LibraryID = Library.ID,
                LibraryName = Library.LibraryName,
                CountBooksPerLibrary = Library.CountBooksPerLibrary
            };

            return View(result);

        }

        [Route("EditeAll/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditeAll([Bind("ID,CardName,PaymentPerDay,PaymentDefault,Date,LibrarySystemID,LibrarySystemName,AuthorID,AuthorName,AuthorRating,BookID,BookName,BookPageCount,LibraryID,LibraryName,CountBooksPerLibrary")] DebtCardInfoFullWithId DebtCardInfoFullWithId)
        {
            if (ModelState.IsValid)
            {
                //СЕРИАЛИЗУЕМ LibrarySystem и посылаем на DebtCardService
                var values = new JObject();
                values.Add("ID", DebtCardInfoFullWithId.LibrarySystemID);
                values.Add("LibrarySystemName", DebtCardInfoFullWithId.LibrarySystemName);
                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                var requestMessage = values.ToString();
                var client = new HttpClient();
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                var requestString = "api/LibrarySystems/" + DebtCardInfoFullWithId.LibrarySystemID;
                var response = await client.PutAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "Error occur while adding LibrarySystem (" + DebtCardInfoFullWithId.LibrarySystemID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                var request = "SERVICE: DebtCardService \r\nPUT: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                var responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    var responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                }
                else
                {
                    var responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "CANNOT UPDATE LibrarySystem";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }


                //СЕРИАЛИЗУЕМ Library и посылаем на LibraryService
                values = new JObject();
                values.Add("ID", DebtCardInfoFullWithId.LibraryID);
                values.Add("LibraryName", DebtCardInfoFullWithId.LibraryName);
                values.Add("CountBooksPerLibrary", DebtCardInfoFullWithId.CountBooksPerLibrary);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Librarys/" + DebtCardInfoFullWithId.LibraryID;
                response = await client.PutAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "Error occur while adding an Library (" + DebtCardInfoFullWithId.LibraryID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: LibraryService \r\nPUT: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    var responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                }
                else
                {
                    var responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "CANNOT UPDATE Library";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }


                //СЕРИАЛИЗУЕМ Author и посылаем на BookService
                values = new JObject();
                values.Add("ID", DebtCardInfoFullWithId.AuthorID);
                values.Add("AuthorName", DebtCardInfoFullWithId.AuthorName);
                values.Add("AuthorRating", DebtCardInfoFullWithId.AuthorRating);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Authors/" + DebtCardInfoFullWithId.AuthorID;
                response = await client.PutAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "Error occur while adding a Author (" + DebtCardInfoFullWithId.AuthorID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: BookService \r\nPUT: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    var responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                }
                else
                {
                    var responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "CANNOT UPDATE Author";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }


                //СЕРИАЛИЗУЕМ Book и посылаем на BookService
                values = new JObject();
                values.Add("ID", DebtCardInfoFullWithId.BookID);
                values.Add("BookName", DebtCardInfoFullWithId.BookName);
                values.Add("PageCount", DebtCardInfoFullWithId.BookPageCount);
                values.Add("AuthorId", DebtCardInfoFullWithId.AuthorID);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Books/" + DebtCardInfoFullWithId.BookID;
                response = await client.PutAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "Error occur while adding an Book (" + DebtCardInfoFullWithId.BookID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: BookService \r\nPUT: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    var responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                }
                else
                {
                    var responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "CANNOT UPDATE Book";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }


                //СЕРИАЛИЗУЕМ DebtCard и посылаем на DebtCardService
                values = new JObject();
                values.Add("ID", DebtCardInfoFullWithId.ID);
                values.Add("CardName", DebtCardInfoFullWithId.CardName);
                values.Add("PaymentPerDay", DebtCardInfoFullWithId.PaymentPerDay);
                values.Add("PaymentDefault", DebtCardInfoFullWithId.PaymentDefault);
                values.Add("AuthorName", DebtCardInfoFullWithId.AuthorName);
                values.Add("BookName", DebtCardInfoFullWithId.BookName);
                values.Add("LibraryName", DebtCardInfoFullWithId.LibraryName);
                values.Add("Date", DebtCardInfoFullWithId.Date);
                values.Add("LibrarySystemID", DebtCardInfoFullWithId.LibrarySystemID);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/DebtCards/" + DebtCardInfoFullWithId.ID;
                response = await client.PutAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "Error occur while adding DebtCard (" + DebtCardInfoFullWithId.ID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: DebtCardService \r\nPUT: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    var responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "CANNOT UPDATE DebtCard";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
            }
            else
            {
                return View();
            }
        }

        [Route("AddDebtCardValid")]
        public async Task<IActionResult> AddDebtCardValid()
        {
            DebtCardInfoFullFake DebtCardFake = new DebtCardInfoFullFake();

            List<Author> result = new List<Author>();
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            byte[] responseMessage;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/Authors";
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: BookService \r\nGET: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var json = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<List<Author>>(json);
                    DebtCardFake.Authors = result;
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);
                //Передаем список доступных городов с ID (для дальнейшей сверки)
            }

            List<LibrarySystem> resultLibrarySystems = new List<LibrarySystem>();
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/LibrarySystems";
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: DebtCardService \r\nGET: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var json = await response.Content.ReadAsStringAsync();
                    resultLibrarySystems = JsonConvert.DeserializeObject<List<LibrarySystem>>(json);
                    DebtCardFake.LibrarySystemNames = resultLibrarySystems;
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);
                //Передаем список доступных городов с ID (для дальнейшей сверки)
            }


            List<Book> resultBooks = new List<Book>();
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
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
                    var json = await response.Content.ReadAsStringAsync();
                    resultBooks = JsonConvert.DeserializeObject<List<Book>>(json);
                    DebtCardFake.Books = resultBooks;
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);
                //Передаем список доступных городов с ID (для дальнейшей сверки)
            }

            List<Library> resultLibrarys = new List<Library>();
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/Librarys";
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: LibraryService \r\nGET: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var json = await response.Content.ReadAsStringAsync();
                    resultLibrarys = JsonConvert.DeserializeObject<List<Library>>(json);
                    DebtCardFake.Librarys = resultLibrarys;
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);
                //Передаем список доступных городов с ID (для дальнейшей сверки)
            }
            return View(DebtCardFake);
        }

        [Route("AddDebtCardToAll")]
        public IActionResult AddDebtCardToAll()
        {
            return View();
        }

        [Route("AddDebtCardRollBack")]
        public IActionResult AddDebtCardRollBack()
        {
            return View();
        }

        [Route("AddDebtCardDelayed")]
        public IActionResult AddDebtCardDelayed()
        {
            return View();
        }

        [Route("AddDebtCardValid")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDebtCardValid([Bind("LibrarySystemName, CardName, PaymentPerDay, PaymentDefault, Date, AuthorName, BookName, LibraryName")] DebtCardInfoFull DebtCardInfoFull)
        {
            //Проверяем, валиден ли арена и артист (запрашиваем соответствующие сущности и проверяем)
            //Если валидно - СЕРИАЛИЗУЕМ DebtCardInfoFull и посылаем на DebtCardService
            Book Book;
            Library Library;
            LibrarySystem LibrarySystem;
            
            //
            //
            //ЗАПРОС АРЕНЫ
            //
            //
            var values = new JObject();
            values.Add("Name", DebtCardInfoFull.BookName);
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            string requestMessage = values.ToString();
            byte[] responseMessage;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URLBookService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpContent content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            string requestString = "api/Books/Find";
            var response = await client.PostAsJsonAsync(requestString, values);
            if ((int)response.StatusCode == 500)
            {
                string description = "There is no Book with Book-NAME (" + DebtCardInfoFull.BookName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ АРЕНУ С ТАКИМ НАЗВАНИЕМ
                //
            }
            request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                //
                //НАШЛИ АРЕНУ - ПРОВЕРЯЕМ ВЕРЕН ЛИ ГОРОД И ВМЕСТИТЕЛЬНОСТЬ / БИЛЕТАМ
                //
                var BookJson = await response.Content.ReadAsStringAsync();
                Book = JsonConvert.DeserializeObject<Book>(BookJson);
                if (Book.PageCount < DebtCardInfoFull.PaymentPerDay)
                {
                    ResponseMessage message = new ResponseMessage();
                    message.description = "Book PageCount lower than tickets number!";
                    message.message = response;
                    return View("Error", message);
                }
                if (Book.Author.AuthorName != DebtCardInfoFull.AuthorName)
                {
                    ResponseMessage message = new ResponseMessage();
                    message.description = "This Book is not in this Author!";
                    message.message = response;
                    return View("Error", message);
                }
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Book NOT FOUND";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ АРЕНУ С ТАКИМ НАЗВАНИЕМ
                //
            }


            //
            //
            //ЗАПРОС АРТИСТА
            //
            //
            values = new JObject();
            values.Add("Name", DebtCardInfoFull.LibraryName);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLLibraryService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/Librarys/Find";
            response = await client.PostAsJsonAsync(requestString, values);
            if ((int)response.StatusCode == 500)
            {
                string description = "There is no Library with Library-NAME (" + DebtCardInfoFull.LibraryName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ Library С ТАКИМ NAME
                //
            }
            request = "SERVICE: LibraryService \r\nPOST: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                //
                //НАШЛИ Library - УСПЕХ
                //
                var LibraryJson = await response.Content.ReadAsStringAsync();
                Library = JsonConvert.DeserializeObject<Library>(LibraryJson);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Library NOT FOUND";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ АРЕНУ С ТАКИМ НАЗВАНИЕМ
                //
            }

            //
            //Если ошибки не произошло - пихаем концерт в концерты
            //

            //УЗНАЕМ ПО LibrarySystemName номер LibrarySystemID или ошибку, если ошибка
            //
            //
            //ЗАПРОС LibrarySystem'a
            //
            //
            values = new JObject();
            values.Add("Name", DebtCardInfoFull.LibrarySystemName);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLDebtCardService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/DebtCards/FindLibrarySystem";
            response = await client.PostAsJsonAsync(requestString, values);
            if ((int)response.StatusCode == 500)
            {
                string description = "There is no LibrarySystem with LibrarySystemName (" + DebtCardInfoFull.LibrarySystemName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ LibrarySystem С ТАКИМ NAME
                //
            }
            request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                //
                //НАШЛИ LibrarySystem - УСПЕХ
                //
                var LibrarySystemJson = await response.Content.ReadAsStringAsync();
                LibrarySystem = JsonConvert.DeserializeObject<LibrarySystem>(LibrarySystemJson);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "LibrarySystem NOT FOUND";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
                //
                //НЕ НАШЛИ LibrarySystem С ТАКИМ НАЗВАНИЕМ
                //
            }

            //СЕРИАЛИЗУЕМ DebtCard и посылаем на DebtCardService
            values = new JObject();
            values.Add("CardName", DebtCardInfoFull.CardName);
            values.Add("PaymentPerDay", DebtCardInfoFull.PaymentPerDay);
            values.Add("PaymentDefault", DebtCardInfoFull.PaymentDefault);
            values.Add("AuthorName", DebtCardInfoFull.AuthorName);
            values.Add("BookName", DebtCardInfoFull.BookName);
            values.Add("LibraryName", DebtCardInfoFull.LibraryName);
            values.Add("Date", DebtCardInfoFull.Date);
            values.Add("LibrarySystemID", LibrarySystem.ID);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLDebtCardService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/DebtCards";
            response = await client.PostAsJsonAsync(requestString, values);
            if ((int)response.StatusCode == 500)
            {
                string description = "Error occur while adding DebtCard (" + DebtCardInfoFull.ID + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }
            request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "CANNOT CREATE DebtCard";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }
        }

        [Route("AddDebtCardToAll")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDebtCardToAll([Bind("LibrarySystemName, CardName, PaymentPerDay, PaymentDefault, Date, AuthorName, AuthorRating, BookName, BookPageCount, LibraryName, CountBooksPerLibrary")] DebtCardInfoFull DebtCardInfoFull)
        {
            //Пихаем все везде
            Book Book;
            Library Library;
            LibrarySystem LibrarySystem;
            Author Author;
            DebtCard DebtCard;

            //
            //Пихаем город, возвращается объект - у него берем ID и запихиваем в арену
            //
            var values = new JObject();
            values.Add("AuthorName", DebtCardInfoFull.AuthorName);
            values.Add("AuthorRating", DebtCardInfoFull.AuthorRating);
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            string requestMessage = values.ToString();
            byte[] responseMessage;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URLBookService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpContent content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            string requestString = "api/Authors";
            var response = await client.PostAsJsonAsync("api/Authors", values);
            if ((int)response.StatusCode == 500)
            {
                string description = "CANNOT ADD Author (" + DebtCardInfoFull.AuthorName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }
            request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                var AuthorContent = await response.Content.ReadAsStringAsync();
                Author = JsonConvert.DeserializeObject<Author>(AuthorContent);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Cannot Add Author";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }

            //
            //
            //В арену
            //
            //
            //СЕРИАЛИЗУЕМ Book и посылаем на BookService
            values = new JObject();
            values.Add("BookName", DebtCardInfoFull.BookName);
            values.Add("AuthorID", Author.ID);
            values.Add("PageCount", DebtCardInfoFull.BookPageCount);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLBookService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/Books";
            response = await client.PostAsJsonAsync("api/Books", values);
            if ((int)response.StatusCode == 500)
            {
                string description = "CANNOT ADD Book (" + DebtCardInfoFull.BookName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }
            request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                var BookContent = await response.Content.ReadAsStringAsync();
                Book = JsonConvert.DeserializeObject<Book>(BookContent);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Cannot Add Book";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }


            //
            //
            //В артиста
            //
            //
            values = new JObject();
            values.Add("LibraryName", DebtCardInfoFull.LibraryName);
            values.Add("CountBooksPerLibrary", DebtCardInfoFull.CountBooksPerLibrary);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLLibraryService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/Librarys";
            response = await client.PostAsJsonAsync("api/Librarys", values);
            if ((int)response.StatusCode == 500)
            {
                string description = "CANNOT ADD Library (" + DebtCardInfoFull.LibraryName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }
            request = "SERVICE: LibraryService \r\nPOST: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                var LibraryContent = await response.Content.ReadAsStringAsync();
                Library = JsonConvert.DeserializeObject<Library>(LibraryContent);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Cannot Add Library";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }


            //
            //
            //В LibrarySystem'a
            //
            //
            values = new JObject();
            values.Add("LibrarySystemName", DebtCardInfoFull.LibrarySystemName);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLDebtCardService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/LibrarySystems";
            response = await client.PostAsJsonAsync("api/LibrarySystems", values);
            if ((int)response.StatusCode == 500)
            {
                string description = "CANNOT ADD LibrarySystem (" + DebtCardInfoFull.LibrarySystemName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }
            request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                var LibrarySystemContent = await response.Content.ReadAsStringAsync();
                LibrarySystem = JsonConvert.DeserializeObject<LibrarySystem>(LibrarySystemContent);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Cannot Add LibrarySystem";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }


            //
            //
            //В Concert
            //
            //
            values = new JObject();
            values.Add("CardName", DebtCardInfoFull.CardName);
            values.Add("PaymentPerDay", DebtCardInfoFull.PaymentPerDay);
            values.Add("PaymentDefault", DebtCardInfoFull.PaymentDefault);
            values.Add("AuthorName", DebtCardInfoFull.AuthorName);
            values.Add("BookName", DebtCardInfoFull.BookName);
            values.Add("LibraryName", DebtCardInfoFull.LibraryName);
            values.Add("Date", DebtCardInfoFull.Date);
            values.Add("LibrarySystemID", LibrarySystem.ID);
            corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            requestMessage = values.ToString();
            client = new HttpClient();
            client.BaseAddress = new Uri(URLDebtCardService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
            requestString = "api/DebtCards";
            response = await client.PostAsJsonAsync("api/DebtCards", values);
            if ((int)response.StatusCode == 500)
            {
                string description = "CANNOT ADD DebtCard (" + DebtCardInfoFull.LibrarySystemName + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }
            request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                var DebtCardContent = await response.Content.ReadAsStringAsync();
                DebtCard = JsonConvert.DeserializeObject<DebtCard>(DebtCardContent);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Cannot Add DebtCard";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }
            return RedirectToAction(nameof(Index), new { id = 1 });
        }

        [Route("AddDebtCardDelayed")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDebtCardDelayed([Bind("LibrarySystemName, CardName, PaymentPerDay, PaymentDefault, Date, AuthorName, AuthorRating, BookName, BookPageCount, LibraryName, CountBooksPerLibrary")] DebtCardInfoFull DebtCardInfoFull)
        //public async Task<IActionResult> AddDebtCardDelayed([FromBody] DebtCardInfoFull DebtCardInfoFull)
        {
            //Пихаем все везде
            Book Book;
            Library Library;
            LibrarySystem LibrarySystem;
            Author Author;
            DebtCard DebtCard;

            var values = new JObject();
            string request;
            string requestMessage;
            byte[] responseMessage;
            System.String corrId;
            HttpClient client;
            HttpContent content;
            string requestString;
            HttpResponseMessage response;
            string responseString;

            //
            //Пихаем город, возвращается объект - у него берем ID и запихиваем в арену
            //
            try
            {

                values.Add("AuthorName", DebtCardInfoFull.AuthorName);
                values.Add("AuthorRating", DebtCardInfoFull.AuthorRating);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);

                requestMessage = values.ToString();

                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Authors";
                response = await client.PostAsJsonAsync("api/Authors", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD Author (" + DebtCardInfoFull.AuthorName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var AuthorContent = await response.Content.ReadAsStringAsync();
                    Author = JsonConvert.DeserializeObject<Author>(AuthorContent);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add Author";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }

                //
                //
                //В арену
                //
                //
                //СЕРИАЛИЗУЕМ Book и посылаем на BookService
                values = new JObject();
                values.Add("BookName", DebtCardInfoFull.BookName);
                values.Add("AuthorID", Author.ID);
                values.Add("PageCount", DebtCardInfoFull.BookPageCount);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Books";
                response = await client.PostAsJsonAsync("api/Books", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD Book (" + DebtCardInfoFull.BookName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var BookContent = await response.Content.ReadAsStringAsync();
                    Book = JsonConvert.DeserializeObject<Book>(BookContent);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add Book";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
            }
            catch
            {
                RabbitBookAuthor rabbitBookAuthor = new RabbitBookAuthor() { BookPageCount = DebtCardInfoFull.BookPageCount, BookName = DebtCardInfoFull.BookName, AuthorName = DebtCardInfoFull.AuthorName, AuthorRating = DebtCardInfoFull.AuthorRating };
                var bus = RabbitHutch.CreateBus("host=localhost");
                bus.Send("BookAuthor", rabbitBookAuthor);
            }


            //
            //
            //В артиста
            //
            //
            try
            {
                values = new JObject();
                values.Add("LibraryName", DebtCardInfoFull.LibraryName);
                values.Add("CountBooksPerLibrary", DebtCardInfoFull.CountBooksPerLibrary);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Librarys";
                response = await client.PostAsJsonAsync("api/Librarys", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD Library. WILL TRY IT LATER(" + DebtCardInfoFull.LibraryName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    //return View("Error", message);
                }
                request = "SERVICE: LibraryService \r\nPOST: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var LibraryContent = await response.Content.ReadAsStringAsync();
                    Library = JsonConvert.DeserializeObject<Library>(LibraryContent);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add Library";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
            }
            catch
            {
                RabbitLibrary rabbitLibrary = new RabbitLibrary() { LibraryName = DebtCardInfoFull.LibraryName, CountBooksPerLibrary = DebtCardInfoFull.CountBooksPerLibrary };
                var bus = RabbitHutch.CreateBus("host=localhost");
                bus.Send("Library", rabbitLibrary);
            }

            //
            //
            //В LibrarySystem'a
            //
            //
            try
            {
                values = new JObject();
                values.Add("LibrarySystemName", DebtCardInfoFull.LibrarySystemName);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/LibrarySystems";
                response = await client.PostAsJsonAsync("api/LibrarySystems", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD LibrarySystem (" + DebtCardInfoFull.LibrarySystemName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var LibrarySystemContent = await response.Content.ReadAsStringAsync();
                    LibrarySystem = JsonConvert.DeserializeObject<LibrarySystem>(LibrarySystemContent);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add LibrarySystem";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }


                //
                //
                //В Concert
                //
                //
                values = new JObject();
                values.Add("CardName", DebtCardInfoFull.CardName);
                values.Add("PaymentPerDay", DebtCardInfoFull.PaymentPerDay);
                values.Add("PaymentDefault", DebtCardInfoFull.PaymentDefault);
                values.Add("AuthorName", DebtCardInfoFull.AuthorName);
                values.Add("BookName", DebtCardInfoFull.BookName);
                values.Add("LibraryName", DebtCardInfoFull.LibraryName);
                values.Add("Date", DebtCardInfoFull.Date);
                values.Add("LibrarySystemID", LibrarySystem.ID);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/DebtCards";
                response = await client.PostAsJsonAsync("api/DebtCards", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD DebtCard (" + DebtCardInfoFull.LibrarySystemName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var DebtCardContent = await response.Content.ReadAsStringAsync();
                    DebtCard = JsonConvert.DeserializeObject<DebtCard>(DebtCardContent);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add DebtCard";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
            }
            catch
            {
                RabbitDebtCardLibrarySystem DebtCardLibrarySystem = new RabbitDebtCardLibrarySystem() { BookName = DebtCardInfoFull.BookName, LibraryName = DebtCardInfoFull.LibraryName, LibrarySystemName = DebtCardInfoFull.LibrarySystemName, AuthorName = DebtCardInfoFull.AuthorName, Date = DebtCardInfoFull.Date, PaymentDefault = DebtCardInfoFull.PaymentDefault, CardName = DebtCardInfoFull.CardName, PaymentPerDay = DebtCardInfoFull.PaymentPerDay };
                var bus = RabbitHutch.CreateBus("host=localhost");
                bus.Send("DebtCardLibrarySystem", DebtCardLibrarySystem);
            }
            //return RedirectToAction(nameof(Index), new { id = 1 });
            return RedirectToAction(nameof(Index), "Default");
        }

        [Route("AddDebtCardRollBack")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDebtCardRollBack([Bind("LibrarySystemName, CardName, PaymentPerDay, PaymentDefault, Date, AuthorName, AuthorRating, BookName, BookPageCount, LibraryName, CountBooksPerLibrary")] DebtCardInfoFull DebtCardInfoFull)
        {
            //Пихаем все везде
            Book Book;
            Library Library;
            LibrarySystem LibrarySystem;
            Author Author;
            DebtCard DebtCard;

            var values = new JObject();
            string request;
            string requestMessage;
            byte[] responseMessage;
            System.String corrId;
            HttpClient client;
            HttpContent content;
            string requestString;
            HttpResponseMessage response;
            string responseString;

            string commentHere = "";

            //
            //Пихаем город, возвращается объект - у него берем ID и запихиваем в арену
            //
            try
            {
                values = new JObject();
                values.Add("AuthorName", DebtCardInfoFull.AuthorName);
                values.Add("AuthorRating", DebtCardInfoFull.AuthorRating);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Authors";
                response = await client.PostAsJsonAsync("api/Authors", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD Author (" + DebtCardInfoFull.AuthorName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var AuthorContent = await response.Content.ReadAsStringAsync();
                    Author = JsonConvert.DeserializeObject<Author>(AuthorContent);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add Author";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }

                //
                //
                //В арену
                //
                //
                //СЕРИАЛИЗУЕМ Book и посылаем на BookService
                values = new JObject();
                values.Add("BookName", DebtCardInfoFull.BookName);
                values.Add("AuthorID", Author.ID);
                values.Add("PageCount", DebtCardInfoFull.BookPageCount);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Books";
                response = await client.PostAsJsonAsync("api/Books", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD Book (" + DebtCardInfoFull.BookName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: BookService \r\nPOST: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var BookContent = await response.Content.ReadAsStringAsync();
                    Book = JsonConvert.DeserializeObject<Book>(BookContent);
                    commentHere += "Saving Author\r\n";
                    commentHere += "Saving Book\r\n";
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add Book";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
            }
            catch
            {
                ResponseMessage message = new ResponseMessage();
                message.description = "Book Service Unavailable. Rollback!";
                //message.message = response;
                commentHere += "Book Service Unavailable. Rollback!\r\n";
                return RedirectToAction("Comment", "Default", new { comment = commentHere });
            }


            //
            //
            //В артиста
            //
            //
            try
            {
                values = new JObject();
                values.Add("LibraryName", DebtCardInfoFull.LibraryName);
                values.Add("CountBooksPerLibrary", DebtCardInfoFull.CountBooksPerLibrary);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/Librarys";
                response = await client.PostAsJsonAsync("api/Librarys", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD Library (" + DebtCardInfoFull.LibraryName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: LibraryService \r\nPOST: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var LibraryContent = await response.Content.ReadAsStringAsync();
                    Library = JsonConvert.DeserializeObject<Library>(LibraryContent);
                    commentHere += "Saving Library\r\n";
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add Library";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
            }
            catch
            {
                commentHere += "Library Service Unavailable. Rollback!\r\n";
                commentHere += "Deleting Book!\r\n";
                commentHere += "Deleting Author!\r\n";
                //Сервис Артистов недоступен - удаляем арену и город, добавленные на 1 этапе
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                requestString = "api/Books/"+Book.ID;
                response = await client.DeleteAsync(requestString);
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                request = "SERVICE: BookService \r\nDELETE: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                await LogQuery(request, responseString, responseMessage);

                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                requestString = "api/Authors/" + Author.ID;
                response = await client.DeleteAsync(requestString);
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                request = "SERVICE: BookService \r\nDELETE: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                await LogQuery(request, responseString, responseMessage);

                string description = "Library Service Unavailable. Rollback!";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                //message.message = response;
                //return RedirectToAction(nameof(Index), "Default");
                
                return RedirectToAction("Comment", "Default", new { comment = commentHere });
            }


            //
            //
            //В LibrarySystem'a
            //
            //
            try
            {
                values = new JObject();
                values.Add("LibrarySystemName", DebtCardInfoFull.LibrarySystemName);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/LibrarySystems";
                response = await client.PostAsJsonAsync("api/LibrarySystems", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD LibrarySystem (" + DebtCardInfoFull.LibrarySystemName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var LibrarySystemContent = await response.Content.ReadAsStringAsync();
                    LibrarySystem = JsonConvert.DeserializeObject<LibrarySystem>(LibrarySystemContent);
                    commentHere += "Saving LibrarySystem!\r\n";
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add LibrarySystem";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }


                //
                //
                //В Concert
                //
                //
                values = new JObject();
                values.Add("CardName", DebtCardInfoFull.CardName);
                values.Add("PaymentPerDay", DebtCardInfoFull.PaymentPerDay);
                values.Add("PaymentDefault", DebtCardInfoFull.PaymentDefault);
                values.Add("AuthorName", DebtCardInfoFull.AuthorName);
                values.Add("BookName", DebtCardInfoFull.BookName);
                values.Add("LibraryName", DebtCardInfoFull.LibraryName);
                values.Add("Date", DebtCardInfoFull.Date);
                values.Add("LibrarySystemID", LibrarySystem.ID);
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                requestMessage = values.ToString();
                client = new HttpClient();
                client.BaseAddress = new Uri(URLDebtCardService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
                requestString = "api/DebtCards";
                response = await client.PostAsJsonAsync("api/DebtCards", values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "CANNOT ADD DebtCard (" + DebtCardInfoFull.LibrarySystemName + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
                request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var DebtCardContent = await response.Content.ReadAsStringAsync();
                    DebtCard = JsonConvert.DeserializeObject<DebtCard>(DebtCardContent);
                    commentHere += "Saving DebtCard!\r\n";
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "Cannot Add DebtCard";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }
            }
            catch
            {
                commentHere += "DebtCard Service Unavailable. Rollback!\r\n";
                commentHere += "Deleting Book!\r\n";
                commentHere += "Deleting Author!\r\n";
                commentHere += "Deleting Library!\r\n";
                //Сервис Концертов недоступен - удаляем арену, город, артистов  добавленные на 1 этапе
                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                requestString = "api/Books/" + Book.ID;
                response = await client.DeleteAsync(requestString);
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                request = "SERVICE: BookService \r\nDELETE: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                await LogQuery(request, responseString, responseMessage);

                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                requestString = "api/Authors/" + Author.ID;
                response = await client.DeleteAsync(requestString);
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                request = "SERVICE: BookService \r\nDELETE: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                await LogQuery(request, responseString, responseMessage);

                corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                client = new HttpClient();
                client.BaseAddress = new Uri(URLLibraryService);
                requestString = "api/Librarys/" + Library.ID;
                response = await client.DeleteAsync(requestString);
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                request = "SERVICE: LibraryService \r\nDELETE: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                await LogQuery(request, responseString, responseMessage);

                string description = "DebtCard Service Unavailable. Rollback!";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                //message.message = response;
                return RedirectToAction("Comment", "Default", new { comment = commentHere });
            }
            //return RedirectToAction(nameof(Index), new { id = 1 });
            return RedirectToAction(nameof(Index), "Default");
        }

        ////private async Task<IActionResult> SendMessageToMicroservice<T>(JObject values, string uri, string postUri, string ErrorDescription, string RequestLog,  )
        //private T SendMessageToMicroservice<T>(JObject values, string uri, string postUri, string ErrorDescription, string RequestLog)
        //{
        //    //await Task.Run(() =>
        //    //{
        //        string request;
        //        string requestMessage;
        //        byte[] responseMessage;
        //        System.String corrId;
        //        HttpClient client;
        //        HttpContent content;
        //        string requestString;
        //        HttpResponseMessage response;
        //        string responseString;

        //        corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
        //        requestMessage = values.ToString();
        //        client = new HttpClient();
        //        client.BaseAddress = new Uri(uri);
        //        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //        content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");
        //        requestString = postUri;
        //        response = await client.PostAsJsonAsync(requestString, values);
        //        if ((int)response.StatusCode == 500)
        //        {
        //            string description = ErrorDescription;
        //            ResponseMessage message = new ResponseMessage();
        //            message.description = description;
        //            message.message = response;
        //            return View("Error", message);
        //        }
        //        request = RequestLog;
        //        responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
        //        if (response.IsSuccessStatusCode)
        //        {
        //            responseMessage = await response.Content.ReadAsByteArrayAsync();
        //            await LogQuery(request, requestMessage, responseString, responseMessage);
        //            var DebtCardContent = await response.Content.ReadAsStringAsync();
        //            T DebtCard = JsonConvert.DeserializeObject<T>(DebtCardContent);
        //        }
        //        else
        //        {
        //            responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
        //            await LogQuery(request, requestMessage, responseString, responseMessage);
        //            string description = "Cannot Add " + nameof(T).ToString();
        //            ResponseMessage message = new ResponseMessage();
        //            message.description = description;
        //            message.message = response;
        //            return View("Error", message);
        //        }
        //    //}
        //}
    }
}