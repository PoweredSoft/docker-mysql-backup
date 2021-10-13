using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.Docker.MysqlBackup.Backup
{
    public class BackupOptions
    {
        public bool NotifySuccess { get; set; } = false;
        public string BasePath { get; set; } = "mysql_backups";
        public string Databases { get; set; } = "*";
    }
}
