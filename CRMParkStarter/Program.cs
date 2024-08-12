using Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CRMParkStarter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<IKernel, FnsKernel.FnsKernel>();
                    services.AddSingleton<CRMParkTest.Program>();
                })
                .Build();
            await (host.Services.GetService(typeof(CRMParkTest.Program)) as CRMParkTest.Program).Main(args);
        }
    }
}
