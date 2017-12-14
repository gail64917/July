using BookService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookService.Data
{
    public class DbInitializer
    {
        public static void Initialize(BookContext context)
        {
            context.Database.EnsureCreated();

            if (context.Authors.Any())
            {
                return;   // DB has been seeded
            }

            var Authors = new Author[]
            {
                new Author{AuthorName = "Moscow", AuthorRating = 12000000},
                new Author{AuthorName = "Berlin", AuthorRating = 3500000},
                new Author{AuthorName = "London", AuthorRating = 9000000}
            };
            foreach (Author c in Authors)
            {
                context.Authors.Add(c);
            }
            context.SaveChanges();

            var Books = new Book[]
            {
                new Book{ BookName = "Crocus Author Hall", AuthorID = 1, PageCount = 6000},
                new Book{ BookName = "Olimpiyskiy", AuthorID = 1, PageCount = 10000},
                new Book{ BookName = "Vegas Author Hall", AuthorID = 1, PageCount = 6000},
                new Book{ BookName = "Wembley Book", AuthorID = 3, PageCount = 4000},
                new Book{ BookName = "Brixton Academy", AuthorID = 3, PageCount = 10000},
                new Book{ BookName = "Mercedes-Benz Book", AuthorID = 2, PageCount = 6000},
                new Book{ BookName = "Olympiastadion Berlin", AuthorID = 2, PageCount = 9000},
                new Book{ BookName = "Rock am Ring", AuthorID = 2, PageCount = 15000},
                new Book{ BookName = "Vova Book", AuthorID = 1, PageCount = 3000},
                new Book{ BookName = "Natasha Book", AuthorID = 1, PageCount = 3000},
                new Book{ BookName = "Big Book", AuthorID = 2, PageCount = 20000},
                new Book{ BookName = "Hell Fire Book", AuthorID = 3, PageCount = 10000},
            };
            foreach (Book a in Books)
            {
                context.Books.Add(a);
            }
            context.SaveChanges();
        }
    }
}
