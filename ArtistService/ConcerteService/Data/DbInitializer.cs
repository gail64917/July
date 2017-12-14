using DebtCardService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DebtCardService.Data
{
    public class DbInitializer
    {
        public static void Initialize(LibraryContext context)
        {
            context.Database.EnsureCreated();

            if (context.Librarys.Any())
            {
                return;   // DB has been seeded
            }

            var Librarys = new Library[]
            {
                new Library { LibraryName = "Marilyn Manson", CountBooksPerLibrary = 10},
                new Library { LibraryName = "30 Seconds to Mars", CountBooksPerLibrary = 7},
                new Library { LibraryName = "Rammstein", CountBooksPerLibrary = 4},
                new Library { LibraryName = "The Beatles", CountBooksPerLibrary = 1},
                new Library { LibraryName = "Suicide Silence", CountBooksPerLibrary = 14},
                new Library { LibraryName = "Depeche mode", CountBooksPerLibrary = 2},
                new Library { LibraryName = "Bullet for my valentine", CountBooksPerLibrary = 15},
                new Library { LibraryName = "Frank Sinatra", CountBooksPerLibrary = 3},
                new Library { LibraryName = "Elvis Presley", CountBooksPerLibrary = 4},
                new Library { LibraryName = "Combichrist", CountBooksPerLibrary = 8},
                new Library { LibraryName = "Devil sold his soul", CountBooksPerLibrary = 9}
            };

            foreach (Library s in Librarys)
            {
                context.Librarys.Add(s);
            }
            context.SaveChanges();
        }
    }
}
