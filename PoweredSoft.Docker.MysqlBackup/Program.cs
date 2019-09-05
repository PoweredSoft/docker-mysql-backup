using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup
{

    public class Program
    {
        static IServiceProvider serviceProvider;
        static IConfiguration configuration;

        static async Task Main(string[] args)
        {
            InitConfiguration();
            InitServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var tasks = serviceProvider.GetServices<ITask>();
                var taskByPriority = tasks.OrderBy(t => t.Priority);

                foreach (var task in taskByPriority)
                {
                    Console.WriteLine($"Starting task {task.Name} with priority {task.Priority}");

                    var returnCode = await task.RunAsync();
                    if (returnCode != 0)
                    {
                        Console.WriteLine($"Task {task.Name} Failed with returned code {returnCode}, exiting early...");
                        Environment.ExitCode = returnCode;
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"Completed task {task.Name} with return code {returnCode}");
                    }

                    Console.WriteLine("Waiting a litle between tasks..");
                    await Task.Delay(2000);
                }
            }
        }

        private static void InitServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private static void InitConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configurationBuilder.AddJsonFile("appsettings.json", true);
            configurationBuilder.AddEnvironmentVariables();
            configuration = configurationBuilder.Build();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(configuration);
            services.AddTransient<ITask, MySQLBackupTask>();
            services.AddTransient<ITask, CleanOldBackupsTask>();
            services.AddTransient<IFtpConnectionProvider, FtpConnectionProvider>();
        }
    }
}
