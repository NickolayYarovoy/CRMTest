using CRMParkTest.Configurations;
using CRMParkTest.DatabaseContext;
using CRMParkTest.Services;
using Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Reflection;
using Telegram.Bot;

namespace CRMParkTest
{
    public class Program
    {
        private IKernel kernel;
        public Program(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public async Task Main(string[] args)
        {

            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configBuilder =>
                {
                    configBuilder.AddUserSecrets(Assembly.GetEntryAssembly());
                    configBuilder.AddJsonFile("appsettings.json");
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));
                    services.Configure<ConnectionStringsConfiguration>(context.Configuration.GetSection("ConnectionStrings"));

                    services.AddHttpClient("telegram_bot_client").RemoveAllLoggers()
                            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                            {
                                BotConfiguration? botConfiguration = sp.GetService<IOptions<BotConfiguration>>()?.Value;
                                ArgumentNullException.ThrowIfNull(botConfiguration);
                                TelegramBotClientOptions options = new(botConfiguration.BotToken);
                                return new TelegramBotClient(options, httpClient);
                            });

                    services.AddDbContext<DataBaseContext>((sp, opt) =>
                    {
                        ConnectionStringsConfiguration? conStrs = sp.GetService<IOptions<ConnectionStringsConfiguration>>()?.Value;
                        ArgumentNullException.ThrowIfNull(conStrs);
                        opt.UseSqlite(conStrs.DbConnection);
                    });

                    services.AddSingleton(kernel);
                    services.AddHostedService<UpdateHandler>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
