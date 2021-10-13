using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup.Notifications
{
    class StdErrNotificationService : INotificationService
    {
        public Task SendNotification(string title, string message, Dictionary<string, string> facts = null, string color = null)
        {
            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine($"{title} - {message}");
            if (facts != null)
            {
                foreach (var fact in facts)
                {
                    Console.Write($"{fact.Key}: {fact.Value}");
                }
            }
            Console.WriteLine("------------------------------------------------------------------------------------------");
            return Task.CompletedTask;
        }
    }
}
