using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AggregationService.Models.LibraryService;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using static AggregationService.Logger.Logger;
using AggregationService.Models.ModelsForView;
using Newtonsoft.Json.Linq;

namespace AggregationService.Controllers
{
    [Route("Library")]
    public class LibraryController : Controller
    {
        private const string URLLibraryService = "http://localhost:61883";


        // GET: Library
        [HttpGet("{id?}")]
        public async Task<IActionResult> Index([FromRoute] int id = 1)
        {
            List<Library> result = new List<Library>();
            int count = 0;

            /**/
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            /**/
            string request;
            //byte[] requestMessage;
            /**/
            byte[] responseMessage;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                /**/
                string requestString = "api/Librarys/page/" + id;
                HttpResponseMessage response = await client.GetAsync(requestString);

                /**/
                request = "SERVICE: LibraryService \r\nGET: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                /**/
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();

                if (response.IsSuccessStatusCode)
                {
                    /**/
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var Librarys = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<List<Library>>(Librarys);
                }
                else
                {
                    /**/
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    return Error();
                }

                /**/
                await LogQuery(request, responseString, responseMessage);


                //
                // ПОЛУЧАЕМ КОЛ-ВО СУЩНОСТЕЙ В БД МИКРОСЕРВИСА, ЧТОБЫ УЗНАТЬ, СКОЛЬКО СТРАНИЦ РИСОВАТЬ
                //
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string requestStringCount = "api/Librarys/count";
                HttpResponseMessage responseStringsCount = await client.GetAsync(requestStringCount);

                /**/
                request = "SERVICE: LibraryService \r\nGET: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                /**/
                responseString = responseStringsCount.Headers.ToString() + "\nStatus: " + responseStringsCount.StatusCode.ToString();

                if (responseStringsCount.IsSuccessStatusCode)
                {
                    /**/
                    responseMessage = await responseStringsCount.Content.ReadAsByteArrayAsync();
                    var countStringsContent = await responseStringsCount.Content.ReadAsStringAsync();
                    count = JsonConvert.DeserializeObject<int>(countStringsContent);
                }
                else
                {
                    /**/
                    responseMessage = Encoding.UTF8.GetBytes(responseStringsCount.ReasonPhrase);
                    return Error();
                }
                LibraryList resultQuery = new LibraryList() { Librarys = result, countLibrarys = count };

                /**/
                await LogQuery(request, responseString, responseMessage);

                return View(resultQuery);
            }
        }


        [Route("AddLibrary")]
        public IActionResult AddLibrary()
        {
            return View();
        }


        [Route("AddLibrary")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLibrary([Bind("LibraryName,CountBooksPerLibrary")] Library Library)
        {
            //СЕРИАЛИЗУЕМ Library и посылаем на LibraryService
            var values = new JObject();
            values.Add("LibraryName", Library.LibraryName);
            values.Add("CountBooksPerLibrary", Library.CountBooksPerLibrary);

            /**/
            var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            /**/
            string request;
            /**/
            string requestMessage = values.ToString();
            /**/
            byte[] responseMessage;

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URLLibraryService);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");

            /**/
            string requestString = "api/Librarys";

            var response = await client.PostAsJsonAsync(requestString, values);

            if ((int)response.StatusCode == 500)
            {
                string description = "There is no Author with ID (" + Library.ID + ")";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                return View("Error", message);
            }

            /**/
            request = "SERVICE: LibraryService \r\nPOST: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
            /**/
            string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();

            if (response.IsSuccessStatusCode)
            {
                /**/
                responseMessage = await response.Content.ReadAsByteArrayAsync();
                /**/
                await LogQuery(request, requestMessage, responseString, responseMessage);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                /**/
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                /**/
                await LogQuery(request, requestMessage, responseString, responseMessage);
                string description = "Another error ";
                ResponseMessage message = new ResponseMessage();
                message.description = description;
                message.message = response;
                //return View(message);
                return View("Error", message);
            }
        }

        [Route("Error")]
        public IActionResult Error()
        {
            return View("Error");
        }


        [HttpGet("Delete/{id?}")]
        //[HttpDelete("{id?}")]
        public async Task<IActionResult> Delete(int id)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                /**/
                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                /**/
                string request;
                //byte[] requestMessage;
                /**/
                byte[] responseMessage;

                string route = "api/Librarys/" + id;

                /**/
                string requestString = route;

                HttpResponseMessage response = await client.DeleteAsync(route);

                /**/
                request = "SERVICE: LibraryService \r\nDELETE: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                /**/
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();

                if (response.IsSuccessStatusCode)
                {
                    /**/
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    /**/
                    await LogQuery(request, responseString, responseMessage);
                    return RedirectToAction(nameof(Index), new { id = 1 });
                }
                else
                {
                    /**/
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    /**/
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
            Library Library;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string requestString = "api/Librarys/" + id;
                HttpResponseMessage response = await client.GetAsync(requestString);

                /**/
                string request = "SERVICE: LibraryService \r\nGET: " + URLLibraryService + "/" + "\r\n" + client.DefaultRequestHeaders.ToString();
                /**/
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();
                /**/
                byte[] responseMessage;

                if (response.IsSuccessStatusCode)
                {
                    /**/
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    var LibraryContent = await response.Content.ReadAsStringAsync();
                    Library = JsonConvert.DeserializeObject<Library>(LibraryContent);
                    if (Library == null)
                    {
                        /**/
                        await LogQuery(request, responseString, responseMessage);
                        return NotFound();
                    }
                    /**/
                    await LogQuery(request, responseString, responseMessage);
                    return View(Library);
                }
                else
                {
                    /**/
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    /**/
                    await LogQuery(request, responseString, responseMessage);
                    return Error();
                }
            }
        }


        [Route("Edite/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edite([Bind("ID,LibraryName,CountBooksPerLibrary")] Library Library)
        {
            if (ModelState.IsValid)
            {
                //СЕРИАЛИЗУЕМ Library и посылаем на LibraryService
                var values = new JObject();
                values.Add("ID", Library.ID);
                values.Add("LibraryName", Library.LibraryName);
                values.Add("CountBooksPerLibrary", Library.CountBooksPerLibrary);

                /**/
                var corrId = string.Format("{0}{1}", DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
                /**/
                string request;
                /**/
                string requestMessage = values.ToString();
                /**/
                byte[] responseMessage;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(URLLibraryService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new StringContent(values.ToString(), Encoding.UTF8, "application/json");

                /**/
                string requestString = "api/Librarys/" + Library.ID;

                var response = await client.PutAsJsonAsync(requestString, values);

                /**/
                request = "SERVICE: LibraryService \r\nPUT: " + URLLibraryService + "/" + requestString + "\r\n" + client.DefaultRequestHeaders.ToString();
                /**/
                string responseString = response.Headers.ToString() + "\nStatus: " + response.StatusCode.ToString();

                if ((int)response.StatusCode == 500)
                {
                    string description = "There is no Library with ID (" + Library.ID + ")";
                    ResponseMessage message = new ResponseMessage();
                    message.description = description;
                    message.message = response;
                    return View("Error", message);
                }

                if (response.IsSuccessStatusCode)
                {
                    /**/
                    responseMessage = await response.Content.ReadAsByteArrayAsync();
                    /**/
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    return RedirectToAction(nameof(Index), new { id = 1 });
                }
                else
                {
                    /**/
                    responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    /**/
                    await LogQuery(request, requestMessage, responseString, responseMessage);
                    return View(response);
                }
            }
            else
            {
                return View();
            }
        }
    }
}