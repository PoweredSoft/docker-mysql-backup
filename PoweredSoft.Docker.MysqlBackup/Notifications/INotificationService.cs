using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup.Notifications
{
    public interface INotificationService
    {
        Task SendNotification(string title, string message, Dictionary<string, string> facts = null, string color = null);
    }
}
