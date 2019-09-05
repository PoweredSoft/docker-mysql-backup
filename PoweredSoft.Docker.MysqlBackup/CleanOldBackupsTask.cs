using FluentFTP;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup
{
    public class CleanOldBackupsTask : ITask
    {
        private readonly IConfiguration configuration;
        private readonly IFtpConnectionProvider ftpConnectionProvider;

        public CleanOldBackupsTask(IConfiguration configuration, IFtpConnectionProvider ftpConnectionProvider)
        {
            this.configuration = configuration;
            this.ftpConnectionProvider = ftpConnectionProvider;
        }

        public int Priority => 2;
        public string Name => "Clean older backup task";

        public async Task<int> RunAsync()
        {
            var ftpBackupDir = configuration["FTP:BackupDestination"];
            Console.WriteLine($"FTP Destination {ftpBackupDir}");

            var isUtc = bool.Parse(configuration["FTP:IsUtc"]);
            DateTime now;
            if (isUtc)
            {
                Console.WriteLine("Ftp is using UTC Time");
                now = DateTime.UtcNow;
            }
            else
            {
                Console.WriteLine("Ftp is using local time.");
                now = DateTime.Now;
            }

            var daysRaw = configuration["Retention:Days"];
            var monthsRaw = configuration["Retention:Months"];
            var days = int.Parse(daysRaw ?? "0");
            var months = int.Parse(monthsRaw ?? "0");

            if (days == 0 && months == 0)
            {
                Console.WriteLine($"must set retention period of days or month at least x > 0");
                return 1;
            }

            var timeSpan = TimeSpan.FromDays(days).Add(TimeSpan.FromDays(months * 31));

#if DEBUG
            timeSpan = TimeSpan.FromMinutes(2);
#endif

            Console.WriteLine($"Retention period {timeSpan}");

            Console.WriteLine("Connecting to ftp server...");
            var ftp = await ftpConnectionProvider.GetConnectedClient();

            var retentionDate = now.Subtract(timeSpan);
            Console.WriteLine($"Attempting to apply rentetion period.. anything older than {retentionDate}");

            var items = await ftp.GetListingAsync(ftpBackupDir, FtpListOption.Recursive);
            foreach (var item in items)
            {
                if (item.FullName.EndsWith(".sql.zip") && item.Modified < retentionDate)
                {
                    Console.WriteLine( $"Attempting to delete file {item.FullName} because {item.Modified} < {retentionDate}");
                    await ftp.DeleteFileAsync(item.FullName);
                }
            }

            await ftp.DisconnectAsync();
            return 0;
        }
    }
}
