using DebtCardService.Models.DebtCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DebtCardService.Data
{
    public class DbInitializer
    {
        public static void Initialize(DebtCardContext context)
        {
            context.Database.EnsureCreated();

            // Look for any students.
            if (context.LibrarySystems.Any())
            {
                return;   // DB has been seeded
            }

            var LibrarySystems = new LibrarySystem[]
            {
                new LibrarySystem{ LibrarySystemName = "Big Billet" },
                new LibrarySystem{ LibrarySystemName = "Afisha" },
                new LibrarySystem { LibrarySystemName = "Ozone" }
            };
            foreach (LibrarySystem s in LibrarySystems)
            {
                context.LibrarySystems.Add(s);
            }
            context.SaveChanges();

            var DebtCards = new DebtCard[]
            {
                new DebtCard{ BookName = "Crocus Author Hall", LibraryName = "Marilyn Manson", AuthorName = "Moscow", Date=DateTime.Parse("2015-09-01"), LibrarySystemID = 1, CardName = "BigSHow", PaymentPerDay = 1000, PaymentDefault = 2000},
                new DebtCard{ BookName = "Brixton Academy", LibraryName = "Marilyn Manson", AuthorName = "London", Date=DateTime.Parse("2015-10-01"), LibrarySystemID = 2, CardName = "Hello, London", PaymentPerDay = 2000, PaymentDefault = 4000},
                new DebtCard{ BookName = "Mercedes-Benz Book", LibraryName = "Marilyn Manson", AuthorName = "Berlin", Date=DateTime.Parse("2015-11-01"), LibrarySystemID = 2, CardName = "Hello, Berlin", PaymentPerDay = 3000, PaymentDefault = 6000},
                new DebtCard{ BookName = "Crocus Author Hall", LibraryName = "Depeche mode", AuthorName = "Moscow", Date=DateTime.Parse("2016-09-01"), LibrarySystemID = 1, CardName = "BigSHow", PaymentPerDay = 1000, PaymentDefault = 9000},
                new DebtCard{ BookName = "Brixton Academy", LibraryName = "Depeche mode", AuthorName = "London", Date=DateTime.Parse("2016-10-01"), LibrarySystemID = 2, CardName = "Hello, Hello", PaymentPerDay = 2000, PaymentDefault = 11000},
                new DebtCard{ BookName = "Mercedes-Benz Book", LibraryName = "Depeche mode", AuthorName = "Berlin", Date=DateTime.Parse("2016-11-01"), LibrarySystemID = 2, CardName = "Hello, Berlin", PaymentPerDay = 3000, PaymentDefault = 1000},
                new DebtCard{ BookName = "Olimpiyskiy", LibraryName = "30 Seconds to Mars", AuthorName = "Moscow", Date=DateTime.Parse("2016-02-01"), LibrarySystemID = 1, CardName = "BigGiG", PaymentPerDay = 3000, PaymentDefault = 22000},
                new DebtCard{ BookName = "Brixton Academy", LibraryName = "30 Seconds to Mars", AuthorName = "London", Date=DateTime.Parse("2016-03-01"), LibrarySystemID = 2, CardName = "Hello,Good Bye", PaymentPerDay = 5000, PaymentDefault = 3100},
                new DebtCard{ BookName = "Mercedes-Benz Book", LibraryName = "30 Seconds to Mars", AuthorName = "Berlin", Date=DateTime.Parse("2016-04-01"), LibrarySystemID = 2, CardName = "Hello, WORLD", PaymentPerDay = 6000, PaymentDefault = 2400},
                new DebtCard{ BookName = "Vova Book", LibraryName = "Frank Sinatra", AuthorName = "Moscow", Date=DateTime.Parse("2017-09-01"), LibrarySystemID = 3, CardName = "VovaSHow", PaymentPerDay = 1000, PaymentDefault = 2600},
                new DebtCard{ BookName = "Vova Book", LibraryName = "Frank Sinatra", AuthorName = "Moscow", Date=DateTime.Parse("2017-10-01"), LibrarySystemID = 3, CardName = "VovaSHow2", PaymentPerDay = 1000, PaymentDefault = 3500},

                new DebtCard{ BookName = "Mercedes-Benz Book", LibraryName = "30 Seconds to Mars", AuthorName = "Berlin", Date=DateTime.Parse("2016-04-01"), LibrarySystemID = 2, CardName = "Hello, WORLD", PaymentPerDay = 99000, PaymentDefault = 44000},
                new DebtCard{ BookName = "Vova Book", LibraryName = "Frank Sinatra", AuthorName = "Berlin", Date=DateTime.Parse("2017-09-01"), LibrarySystemID = 3, CardName = "VovaSHow", PaymentPerDay = 1000, PaymentDefault = 4500},
                new DebtCard{ BookName = "Mercedes-Benz Book", LibraryName = "Eminem", AuthorName = "Berlin", Date=DateTime.Parse("2017-09-01"), LibrarySystemID = 3, CardName = "VovaSHow", PaymentPerDay = 1000, PaymentDefault = 3000},
                new DebtCard{ BookName = "Trash Book", LibraryName = "Eminem", AuthorName = "Berlin", Date=DateTime.Parse("2017-09-01"), LibrarySystemID = 3, CardName = "VovaSHow", PaymentPerDay = 1000, PaymentDefault = 8000}
            };

            foreach (DebtCard c in DebtCards)
            {
                context.DebtCards.Add(c);
            }
            context.SaveChanges();
        }
    }
}
