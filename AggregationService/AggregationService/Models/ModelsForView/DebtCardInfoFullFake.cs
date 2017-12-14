using AggregationService.Models.BookService;
using AggregationService.Models.LibraryService;
using AggregationService.Models.DebtCardService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AggregationService.Models.ModelsForView
{
    public class DebtCardInfoFullFake
    {
        public int ID { get; set; }
        public string LibrarySystemName { get; set; }

        public string CardName { get; set; }
        public int PaymentPerDay { get; set; }
        public int PaymentDefault { get; set; }
        public DateTime Date { get; set; }

        public string AuthorName { get; set; }
        public int AuthorRating { get; set; }

        public string BookName { get; set; }
        public int BookPageCount { get; set; }

        public string LibraryName { get; set; }
        public int CountBooksPerLibrary { get; set; }

        public List<Author> Authors { get; set; }
        public List<LibrarySystem> LibrarySystemNames { get; set; }
        public List<Book> Books { get; set; }
        public List<Library> Librarys { get; set; }
    }
}
