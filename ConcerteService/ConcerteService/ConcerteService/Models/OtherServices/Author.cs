using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DebtCardService.Models
{
    public class Author
    {
        public int ID { get; set; }
        public string AuthorName { get; set; }
        public int AuthorRating { get; set; }

        //public virtual List<Book> Books { get; set; }
    }
}
