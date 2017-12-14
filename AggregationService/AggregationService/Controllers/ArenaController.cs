using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AggregationService.Models;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using AggregationService.Models.BookService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Newtonsoft;
using System.Net.Http.Formatting;
using System.Data.Entity;
using AggregationService.Models.ModelsForView;
using System.Threading;
using static AggregationService.Logger.Logger;

namespace AggregationService.Controllers
{
    [Route("Book")]
    public class BookController : Controller
    {
        private const string URLBookService = "http://localhost:58349";


        //[HttpGet("Index/{id?}")]
        [HttpGet("{id?}")]
        public async Task<IActionResult> Index([FromRoute] int id = 1)
        {
            List<Book> result = new List<Book>();
            int count = 0;

            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);         
            string request;
            byte[] responseMessage;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string requestString = "api/Books/page/" + id;
                HttpResponseMessage response = await client.GetAsync(requestString);
                
                request = "SERVICE: BookService \r\nGET: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();

                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var Books = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<List<Book>>(Books);
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

                string requestStringCount = "api/Books/count";
                HttpResponseMessage responseStringsCount = await client.GetAsync(requestStringCount);

                request = "SERVICE: BookService \r\nGET: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
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
                BookList resultQuery = new BookList() { Books = result, countBooks = count };

                await LogQuery(request, responseString, responseMessage);

                return View(resultQuery);
            }
        }

        //[HttpGet("{page?}")]
        //public IActionResult IndexHead([FromRoute] int page = 1)
        //{
        //    return RedirectToAction(nameof(Index), new { id = page });
        //}


        [Route("AddBook")]
        public async Task<IActionResult> AddBook()
        {
            BookFake BookFake = new BookFake();

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
                    BookFake.Authors = result;
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);
                //Передаем список доступных городов с ID (для дальнейшей сверки)
                return View(BookFake);
            }
        }


        [Route("AddBook")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook([Bind("BookName,PageCount,AuthorID")] Book Book)
        {
            //СЕРИАЛИЗУЕМ Book и посылаем на BookService
            var values = new JObject();
            values.Add("BookName", Book.BookName);
            values.Add("AuthorID", Book.AuthorID);
            values.Add("PageCount", Book.PageCount);

            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            string request;
            string requestMessage = values.ToString();
            byte[] responseMessage;

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URLBookService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");

            string requestString = "api/Books";

            var response = await client.PostAsJsonAsync("api/Books", values);

            if ((int)response.StatusCode == 500)
            {
                string description = "There is no Author with ID (" + Book.AuthorID + ")";
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
                return RedirectToAction(nameof(Index));
            }
            else
            {
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Another error ";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            } 
        }


        [HttpGet("Delete/{id?}")]
        public async Task<IActionResult> Delete(int id)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                string request;
                byte[] responseMessage;

                string route = "api/Books/" + id;

                string requestString = route;

                HttpResponseMessage response = await client.DeleteAsync(route);

                request = "SERVICE: BookService \r\nDELETE: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
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


            BookFake BookFake = new BookFake();

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
                    BookFake.Authors = result;
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }
                await LogQuery(request, responseString, responseMessage);
                //Передаем список доступных городов с ID (для дальнейшей сверки)
            }

                //
                // ПОЛУЧАЕМ СУЩНОСТЬ с ID
                //
            Book Book;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string requestString = "api/Books/" + id;
                HttpResponseMessage response = await client.GetAsync(requestString);
                request = "SERVICE: BookService \r\nGET: " + URLBookService + "/" + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var BookContent = await response.Content.ReadAsStringAsync();
                    Book = JsonConvert.DeserializeObject<Book>(BookContent);
                    if (Book == null)
                    {
                        await LogQuery(request, responseString, responseMessage);
                        return NotFound();
                    }
                    await LogQuery(request, responseString, responseMessage);


                    BookFake.BookName = Book.BookName;
                    BookFake.PageCount = Book.PageCount;
                    BookFake.Author = Book.Author;
                    BookFake.AuthorID = Book.AuthorID;
                    BookFake.ID = Book.ID;

                    return View(BookFake);
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
        public async Task<IActionResult> Edite([Bind("ID,BookName,PageCount,AuthorID")] Book Book)
        {
            if (ModelState.IsValid)
            {
                //СЕРИАЛИЗУЕМ Book и посылаем на BookService
                var values = new JObject();
                values.Add("ID", Book.ID);
                values.Add("BookName", Book.BookName);
                values.Add("AuthorID", Book.AuthorID);
                values.Add("PageCount", Book.PageCount);

                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                string request;
                string requestMessage = values.ToString();
                byte[] responseMessage;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(URLBookService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");

                string requestString = "api/Books/" + Book.ID;

                var response = await client.PutAsJsonAsync(requestString, values);

                request = "SERVICE: BookService \r\nPUT: " + URLBookService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();

                if ((int)response.StatusCode == 500)
                {
                    string description = "There is no Author with ID (" + Book.AuthorID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }

                if (response.IsSuccessStatusCode)
                {
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    return RedirectToAction(nameof(Index), new { id = 1 });
                }
                else
                {
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    return View(response);
                }
            }
            else
            {
                return View();
            }
        }


        [Route("Error")]
        public IActionResult Error()
        {
            return View("Error");
        }
    }
}