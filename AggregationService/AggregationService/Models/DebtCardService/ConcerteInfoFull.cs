using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AggregationService.Models.DebtCardService
{
    public class DebtCardInfoFull
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

        //LibrarySystemName, CardName, PaymentPerDay, PaymentDefault, Date, AuthorName, AuthorRating, BookName, BookPageCount, LibraryName, CountBooksPerLibrary
    }
}
