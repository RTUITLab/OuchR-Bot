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
            services.Configure<CalendarOptions>(Configuration.GetSection(nameof(CalendarOptions)));

            services.AddDbContext<BotDbContext>(db => db.UseInMemoryDatabase("IN_MEMORY_DB"));
            services.AddScoped<MessageHandlerService>();
            if (Configuration.GetValue<bool>("USE_MOCK_PROFILE_PARSER_SERVICE"))
            {
                services.AddScoped<IProfileParser, MockProfileParser>();
            }
            else
            {
                services.AddScoped<IProfileParser, RealProfileParser>();
            }
            if (Configuration.GetValue<bool>("DUMP_JSON_DATABASE"))
            {
                services.AddHostedService<Dumper>();
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<Startup> logger)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                SeedDb(scope.ServiceProvider.GetRequiredService<BotDbContext>(), logger);
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
            }
            app.UseCors(cors => cors.AllowAnyMethod().AllowAnyOrigin().AllowAnyHeader());
            app.UseRouting();
            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void SeedDb(BotDbContext botDbContext, ILogger<Startup> logger)
        {
            var fileWithDump = "dbExport.json";
            if (!File.Exists(fileWithDump))
            {
                logger.LogWarning($"There is no dump file '{fileWithDump}'");
                return;
            }
            var dumpText = File.ReadAllText(fileWithDump);
            List<BotUser> users;
            try
            {
                users = JsonConvert.DeserializeObject<List<BotUser>>(dumpText);
            }
            catch
            {
                logger.LogError($"File '{fileWithDump}' contains incorrect data");
                return;
            }
            botDbContext.Users.AddRange(users);
            botDbContext.SaveChanges();
            logger.LogInformation($"Data from '{fileWithDump}' was successfully restored");
        }
    }
}
