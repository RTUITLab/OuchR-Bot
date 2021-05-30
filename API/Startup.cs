using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OuchRBot.API.Database;
using OuchRBot.API.Models;
using OuchRBot.API.Services;
using OuchRBot.API.Services.RemoteServices.ProfileParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuchRBot.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<VkBotOptions>(Configuration.GetSection(nameof(VkBotOptions)));
            services.Configure<RealProfileParserOptions>(Configuration.GetSection(nameof(RealProfileParserOptions)));

            services.AddDbContext<BotDbContext>(db => db.UseInMemoryDatabase("IN_MEMORY_DB"));
            services.AddScoped<MessageHandlerService>();
            if (true)
            {
                services.AddScoped<IProfileParser, MockProfileParser>();
            }
            else
            {
                services.AddScoped<IProfileParser, RealProfileParser>();
            }
            services.AddControllers()
                .AddNewtonsoftJson(op => op.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
            });
            services.AddHostedService<VkBotWorker>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceScopeFactory serviceScopeFactory)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                SeedDb(scope.ServiceProvider.GetRequiredService<BotDbContext>());
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
            }

            app.UseRouting();
            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void SeedDb(BotDbContext botDbContext)
        {
            var users = JsonConvert.DeserializeObject<List<BotUser>>(File.ReadAllText("dbExport.json"));
            botDbContext.Users.AddRange(users);
            botDbContext.SaveChanges();
        }
    }
}
