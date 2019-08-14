using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Database.Users;

namespace PetPlatoon.Godfrey.Extensions
{
    public static class DiscordObjectExtensions
    {
        public static async Task<User> GetDatabaseUserAsync(this DiscordUser member, DatabaseContext context)
        {
            if (member == null)
            {
                return null;
            }

            var result = await context.Users.SingleOrDefaultAsync(x => x.Id == member.Id);

            if (result == null)
            {
                result = new User
                {
                        Id = member.Id,
                        Name = member.Username
                };

                var entity = await context.Users.AddAsync(result);
                await context.SaveChangesAsync();

                result = entity.Entity;
            }

            return result;
        }

        public static Task<User> GetDatabaseUserAsync(this DiscordMember member, DatabaseContext context)
        {
            return GetDatabaseUserAsync((DiscordUser)member, context);
        }
    }
}
