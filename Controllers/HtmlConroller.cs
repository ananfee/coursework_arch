using HtmlAgilityPack;
using plakplak.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plakplak.Controllers
{
    public class HtmlController
    {
        private string html = @"https://pokemondb.net/pokedex/all";
        private DataBaseController dataBaseController = new DataBaseController();
        public List<Pokemons> GetPokemons()
        {
            List<Pokemons> pokemons = new List<Pokemons>();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument doc = new HtmlDocument();

            try
            {
                doc = htmlWeb.Load(html);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке HTML: {ex.Message}");
                throw;
            }
            var tr = doc.DocumentNode.SelectNodes("//tbody/tr");
            if (tr == null)
            {
                Console.WriteLine("Не найдены узлы tr");
                return pokemons;
            }

            List<Pokemons> uniquePokemons = new List<Pokemons>();
            int count = 0; 

            foreach (HtmlNode row in tr)
            {
                if (count >= 50)
                {
                    break;
                }
                if (string.IsNullOrWhiteSpace(row.OuterHtml))
                {
                    Console.WriteLine("Пустая строка");
                    continue;
                }
                try
                {
                    var nameNode = row.SelectSingleNode(".//td[@class='cell-name']/a");
                    var typeNodes = row.SelectNodes(".//td[@class='cell-icon']/a");
                    var abilityIdNode = row.SelectSingleNode(".//td[@class='cell-num cell-fixed']");

                    if (nameNode == null || typeNodes == null || abilityIdNode == null)
                    {
                        Console.WriteLine("Не удалось получить один из нужных узлов (name/type/ability). Пропускаем строку...");
                        Console.WriteLine($"OuterHtml: {row.OuterHtml}");
                        continue;
                    }

                    string name = nameNode.InnerText.Trim();
                    string typeName = string.Join(", ", typeNodes.Select(node => node.InnerText.Trim()));
                    int abilityId;
                    if (!int.TryParse(abilityIdNode.GetAttributeValue("data-sort-value", "0"), out abilityId))
                    {
                        Console.WriteLine($"Не удалось получить ID способности из {abilityIdNode.OuterHtml}");
                        continue;
                    }

                    Pokemons pokemon = new Pokemons { Name = name, TypeId = 0, AbilityId = 0 };

                    using (var context = new haEntities())
                    {
                        var type = context.Types.FirstOrDefault(t => t.TypeName == typeName);
                        if (type == null)
                        {
                            type = new Types { TypeName = typeName };
                            context.Types.Add(type);
                            context.SaveChanges();
                        }
                        pokemon.TypeId = type.Id;

                        var ability = context.Abilities.FirstOrDefault(a => a.AbilityName == abilityId.ToString());
                        if (ability == null)
                        {
                            ability = new Abilities { AbilityName = abilityId.ToString() };
                            context.Abilities.Add(ability);
                            context.SaveChanges();
                        }
                        pokemon.AbilityId = ability.Id;
                    }
                    if (!uniquePokemons.Any(p => p.Name == pokemon.Name && p.TypeId == pokemon.TypeId && p.AbilityId == pokemon.AbilityId))
                    {
                        uniquePokemons.Add(pokemon);
                    }
                    count++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке строки: {ex.Message}");
                    Console.WriteLine($"OuterHtml: {row.OuterHtml}");
                    continue;
                }
            }
            using (var context = new haEntities())
            {
                foreach (var pokemon in uniquePokemons)
                {
                    var existingPokemon = context.Pokemons.FirstOrDefault(p => p.Name == pokemon.Name && p.TypeId == pokemon.TypeId && p.AbilityId == pokemon.AbilityId);

                    if (existingPokemon == null)
                    {
                        dataBaseController.Insert(pokemon, context);
                        pokemons.Add(pokemon);
                    }
                }
            }
            return pokemons;
        }
    }
}
