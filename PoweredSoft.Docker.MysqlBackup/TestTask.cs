using PoweredSoft.Docker.MysqlBackup.Notifications;
using PoweredSoft.Docker.MysqlBackup.Notifications.Teams;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup
{
    internal class TestTask : ITask
    {
        private readonly INotifyService notifyService;

        public TestTask(INotifyService notifyService)
        {
            this.notifyService = notifyService;
        }

        public int Priority { get; }
        public string Name => "Test Task";

        public async Task<int> RunAsync()
        {
            await notifyService.SendNotification("Backup of XXXXXXXXXXXXXXX", "Successfully transfered to s3", new Dictionary<string, string>
            {
                { "Database", "Available" },
                { "Something else", "Not Available" }
            });

            return 0;
        }
    }
}