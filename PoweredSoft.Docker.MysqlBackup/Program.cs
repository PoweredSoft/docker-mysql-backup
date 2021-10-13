using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using PoweredSoft.Docker.MysqlBackup.Backup;
using PoweredSoft.Docker.MysqlBackup.Notifications;
using PoweredSoft.Docker.MysqlBackup.Notifications.Slack;
using PoweredSoft.Docker.MysqlBackup.Notifications.Teams;
using PoweredSoft.Docker.MysqlBackup.Retention;
using PoweredSoft.Docker.MysqlBackup.Storage.Azure;
using PoweredSoft.Docker.MysqlBackup.Storage.Physical;
using PoweredSoft.Docker.MysqlBackup.Storage.S3;
using PoweredSoft.Storage.Azure.Blob;
using PoweredSoft.Storage.Core;
using PoweredSoft.Storage.Physical;
using PoweredSoft.Storage.S3;
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
                var notifyService = serviceProvider.GetRequiredService<INotifyService>();
                var mysqlOptions = serviceProvider.GetRequiredService<MySqlOptions>();

                var taskByPriority = tasks.OrderBy(t => t.Priority);

                foreach (var task in taskByPriority)
                {
                    Console.WriteLine($"Starting task {task.Name} with priority {task.Priority}");

                    try
                    {
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
                    }
                    catch(Exception ex)
                    {
                        try
                        {
                            await notifyService.SendNotification($"MYSQL BACKUP FAILED {GetHostName(mysqlOptions)}", $"Task {task.Name} failed", new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "ExceptionMessage", ex.Message },
                                { "InnerExceptionMessage", ex.InnerException?.Message }
                            });
                        }
                        catch
                        {

                        }
                    }

                    Console.WriteLine("Waiting a litle between tasks..");
                    await Task.Delay(2000);
                }
            }
        }

        private static string GetHostName(MySqlOptions mysqlOptions)
        {
            try
            {
                var connectionStringBuilder = new MySqlConnectionStringBuilder(mysqlOptions.ConnectionString);
                return connectionStringBuilder.Server;
            }
            catch
            {
                return "CANNOTRESOLVEHOSTNAME";
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
            // configuration.
            AddConfigurationServices(services);

            // messaging.
            services.AddTransient<INotifyService, NotifyService>();
            services.AddTransient<INotificationService, TeamsNotificationService>();
            services.AddTransient<INotificationService, SlackNotificationService>();
            services.AddTransient<INotificationService, StdErrNotificationService>();

            // storage provider.
            ConfigureStorageProvider(services);

            // tasks.
            services.AddTransient<ITask, BackupTask>();
            services.AddTransient<ITask, RetentionTask>();
        }

        private static void ConfigureStorageProvider(IServiceCollection services)
        {
            services.AddTransient<IStorageProvider>(ctx =>
            {
                var azureCfg = ctx.GetRequiredService<AzureConfiguration>();
                var s3Cfg = ctx.GetRequiredService<S3Configuration>();
                var physicalCfg = ctx.GetRequiredService<PhysicalConfiguration>();

                if (azureCfg.Enabled)
                    return new AzureBlobStorageProvider(azureCfg.ConnectionString, azureCfg.ContainerName);
                if (s3Cfg.Enabled)
                {
                    var s3 = new S3StorageProvider(s3Cfg.Endpoint, s3Cfg.BucketName, s3Cfg.AccessKey, s3Cfg.Secret);
                    
                    if (s3Cfg.Minio)
                    {
                        s3.SetForcePathStyle(true);
                        s3.SetS3UsEast1RegionalEndpointValue(Amazon.Runtime.S3UsEast1RegionalEndpointValue.Legacy);
                    }

                    return s3;

                }
                if (physicalCfg.Enabled)
                    return new PhysicalStorageProvider();

                throw new Exception($"No storage provider was enabled.");
            });
        }

        private static void AddConfigurationServices(IServiceCollection services)
        {
            services.AddSingleton(configuration);
            services.AddSingleton<TeamsConfiguration>(ctx =>
            {
                var t = new TeamsConfiguration();
                configuration.Bind("Teams", t);
                return t;
            });

            services.AddSingleton<BackupOptions>(ctx =>
            {
                var t = new BackupOptions();
                configuration.Bind("Backup", t);
                return t;
            });

            services.AddSingleton<MySqlOptions>(ctx =>
            {
                var t = new MySqlOptions();
                configuration.Bind("MySql", t);
                return t;
            });

            services.AddSingleton<RetentionOptions>(ctx =>
            {
                var t = new RetentionOptions();
                configuration.Bind("Retention", t);
                return t;
            });

            services.AddSingleton<SlackConfiguration>(ctx =>
            {
                var t = new SlackConfiguration();
                configuration.Bind("Slack", t);
                return t;
            });

            services.AddSingleton<AzureConfiguration>(ctx =>
            {
                var t = new AzureConfiguration();
                configuration.Bind("Azure", t);
                return t;
            });

            services.AddSingleton<S3Configuration>(ctx =>
            {
                var t = new S3Configuration();
                configuration.Bind("S3", t);
                return t;
            });

            services.AddSingleton<PhysicalConfiguration>(ctx =>
            {
                var t = new PhysicalConfiguration();
                configuration.Bind("Physical", t);
                return t;
            });
        }
    }
}
