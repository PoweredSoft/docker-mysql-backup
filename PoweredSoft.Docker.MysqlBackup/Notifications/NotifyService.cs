using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup.Notifications
{
    public class NotifyService : INotifyService
    {
        private readonly IEnumerable<INotificationService> notificationServices;

        public NotifyService(IEnumerable<INotificationService> notificationServices)
        {
            this.notificationServices = notificationServices;
        }

        public async Task SendNotification(string title, string message, Dictionary<string, string> facts = null, string color = null)
        {
            foreach(var notificationService in notificationServices)
                await notificationService.SendNotification(title, message, facts, color);
        }
    }
}
