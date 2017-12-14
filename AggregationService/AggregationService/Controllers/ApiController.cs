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
using static AggregationService.Logger.Logger;
using AggregationService.Models.BookService;
using AggregationService.Models.LibraryService;
using AggregationService.Models.ModelsForView;
using Newtonsoft.Json.Linq;
using RabbitModels;
using EasyNetQ;

namespace AggregationService.Controllers
{
    [Produces("application/json")]
    [Route("Api")]
    public class ApiController : Controller
    {
        private const string URLLibraryService = "http://localhost:61883";
        private const string URLBookService = "http://localhost:58349";
        private const string URLDebtCardService = "http://localhost:61438";

        // GET: api/3
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
                    return BadRequest("DebtCard Service unavailable");
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
                    return BadRequest("Book Service unavailable");
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
                    return BadRequest("Library Service unavailable");
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
            return Ok(resultQuery);
        }

        // Delete: api/3
        [HttpDelete("{id?}")]
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
                    return Ok();
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, responseString, responseMessage);
                    return BadRequest("DebtCard service unavailable");
                }
            }
        }

        // PUT: api/edite/3
        [Route("Edite/{id}")]
        [HttpPut]
        public async Task<IActionResult> Edite([FromBody] DebtCardInfoFull DebtCardInfoFull)
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
                    return BadRequest("Book Service unavailable");
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
                        return BadRequest("PageCount lower than tickets number");
                    }
                    if (Book.Author.AuthorName != DebtCardInfoFull.AuthorName)
                    {
                        ResponseMessage message = new ResponseMessage();
                        message.description = "This Book is not in this Author!";
                        message.message = response;
                        return BadRequest("This Author does not have this Book");
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
                    return NoContent();
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
                    return BadRequest("Library Service unavailable");
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
                    return NoContent();
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
                    return BadRequest("DebtCard Service unavailable");
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
                    return NoContent();
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
                requestString = "api/DebtCards/" + DebtCardInfoFull.ID;
                response = await client.PutAsJsonAsync(requestString, values);
                if ((int)response.StatusCode == 500)
                {
                    string description = "Error occur while adding DebtCard (" + DebtCardInfoFull.ID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return BadRequest("DebtCard Service unavailable");
                }
                request = "SERVICE: DebtCardService \r\nPUT: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var Json = await response.Content.ReadAsStringAsync();
                    var DebtCard = JsonConvert.DeserializeObject<DebtCardInfoFullWithId>(Json);
                    return Ok(DebtCard);
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "CANNOT UPDATE DebtCard";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return NoContent();
                }
            }
            else
            {
                return BadRequest("Model is invalid!");
            }
        }

        // PUT: api/editeall/3
        [Route("EditeAll/{id}")]
        [HttpPut]
        public async Task<IActionResult> EditeAll([FromBody] DebtCardInfoFullWithId DebtCardInfoFullWithId)
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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
                }
                request = "SERVICE: DebtCardService \r\nPUT: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    var responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    var Json = await response.Content.ReadAsStringAsync();
                    DebtCardInfoFullWithId = JsonConvert.DeserializeObject<DebtCardInfoFullWithId>(Json);
                    return Ok(DebtCardInfoFullWithId);
                }
                else
                {
                    var responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    string description = "CANNOT UPDATE DebtCard";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return BadRequest(description);
                }
            }
            else
            {
                return BadRequest();
            }
        }


        // POST: api/AddDebtCardValid
        [Route("AddDebtCardValid")]
        [HttpPost]
        public async Task<IActionResult> AddDebtCardValid([FromBody] DebtCardInfoFull DebtCardInfoFull)
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
                return BadRequest(description);
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
                    return BadRequest(message.description);
                }
                if (Book.Author.AuthorName != DebtCardInfoFull.AuthorName)
                {
                    ResponseMessage message = new ResponseMessage();
                    message.description = "This Book is not in this Author!";
                    message.message = response;
                    return BadRequest(message.description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
            }
            request = "SERVICE: DebtCardService \r\nPOST: " + URLDebtCardService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                await LogQuery(request, requestMessage, responseString, responseMessage);
                var Json = await response.Content.ReadAsStringAsync();
                var DebtCard = JsonConvert.DeserializeObject<DebtCardInfoFullWithId>(Json);
                return Ok(DebtCard);
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "CANNOT CREATE DebtCard";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return BadRequest(description);
            }
        }

        // POST: api/AddDebtCardToAll
        [Route("AddDebtCardToAll")]
        [HttpPost]
        public async Task<IActionResult> AddDebtCardToAll([FromBody] DebtCardInfoFull DebtCardInfoFull)
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
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
                return BadRequest(description);
            }
            return Ok(DebtCardInfoFull);
        }

        [Route("AddDebtCardDelayed")]
        [HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddDebtCardDelayed([Bind("LibrarySystemName, CardName, PaymentPerDay, PaymentDefault, Date, AuthorName, AuthorRating, BookName, BookPageCount, LibraryName, CountBooksPerLibrary")] DebtCardInfoFull DebtCardInfoFull)
        public async Task<IActionResult> AddDebtCardDelayed([FromBody] DebtCardInfoFull DebtCardInfoFull)
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

            string MethodResult = "";

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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
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
                    return BadRequest(description);
                }
            }
            catch
            {
                RabbitBookAuthor rabbitBookAuthor = new RabbitBookAuthor() { BookPageCount = DebtCardInfoFull.BookPageCount, BookName = DebtCardInfoFull.BookName, AuthorName = DebtCardInfoFull.AuthorName, AuthorRating = DebtCardInfoFull.AuthorRating };
                var bus = RabbitHutch.CreateBus("host=localhost");
                bus.Send("BookAuthor", rabbitBookAuthor);

                MethodResult += "Book Service does not respond. We will save all later\r\n";
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

                MethodResult += "Library Service does not respond. We will save all later\r\n";
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

                MethodResult += "DebtCard Service does not respond. We will save all later\r\n";
            }
            //return RedirectToAction(nameof(Index), new { id = 1 };
            return Ok(MethodResult);
        }
    }
}