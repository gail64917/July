using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitModels
{
    public class RabbitDebtCardLibrarySystem
    {
        public string LibrarySystemName { get; set; }

        public string CardName { get; set; }
        public int PaymentPerDay { get; set; }
        public int PaymentDefault { get; set; }
        public string AuthorName { get; set; }
        public string BookName { get; set; }
        public string LibraryName { get; set; }
        public DateTime Date { get; set; }
    }
}
