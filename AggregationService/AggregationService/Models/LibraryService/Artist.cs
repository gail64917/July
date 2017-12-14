using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AggregationService.Models.LibraryService
{
    public class Library
    {
        public int ID { get; set; }
        public string LibraryName { get; set; }
        public int CountBooksPerLibrary { get; set; }
    }
}
