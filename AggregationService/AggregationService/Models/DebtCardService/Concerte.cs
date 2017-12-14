using AggregationService.Models.DebtCardService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AggregationService.Models.DebtCardService
{
    public class DebtCard
    {
        public int ID { get; set; }
        public string CardName { get; set; }
        public int PaymentPerDay { get; set; }
        public int PaymentDefault { get; set; }
        public string AuthorName { get; set; }
        public string BookName { get; set; }
        public string LibraryName { get; set; }
        public DateTime Date { get; set; }
        public int LibrarySystemID { get; set; }

        public virtual LibrarySystem LibrarySystem { get; set; }
    }
}
