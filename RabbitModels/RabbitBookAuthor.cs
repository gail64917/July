using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitModels
{
    public class RabbitBookAuthor
    {
        public string AuthorName { get; set; }
        public int AuthorRating { get; set; }

        public string BookName { get; set; }
        public int BookPageCount { get; set; }
    }
}
