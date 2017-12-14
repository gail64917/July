﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DebtCardService.Models
{
    public class Book
    {
        public int ID { get; set; }
        public string BookName { get; set; }
        public int PageCount { get; set; }
        public int AuthorID { get; set; }

        public virtual Author Author { get; set; }
    }
}
