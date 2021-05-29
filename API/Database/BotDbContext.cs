using Microsoft.EntityFrameworkCore;
using OuchRBot.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OuchRBot.API.Database
{
    public class BotDbContext : DbContext
    {
        public DbSet<BotUser> Users { get; set; }
        public BotDbContext(DbContextOptions<BotDbContext> options): base(options)
        {

        }
    }
}
