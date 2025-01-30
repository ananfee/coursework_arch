using plakplak.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plakplak.Controllers
{
    public class DataBaseController
    {
        public void Insert(Pokemons pokemons, haEntities context)
        {
            context.Pokemons.Add(pokemons);
            context.SaveChanges();
        }

        public void Delete(haEntities context)
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    context.Pokemons.RemoveRange(context.Pokemons);
                    context.SaveChanges();
                    context.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Pokemons', RESEED, 0)");

                    context.Abilities.RemoveRange(context.Abilities);
                    context.SaveChanges();
                    context.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Abilities', RESEED, 0)");


                    context.Types.RemoveRange(context.Types);
                    context.SaveChanges();
                    context.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Types', RESEED, 0)");

                    transaction.Commit();
                    Console.WriteLine("Данные успешно удалены из базы данных.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Ошибка при удалении данных: " + ex.Message);
                }
            }
        }
    }
}
