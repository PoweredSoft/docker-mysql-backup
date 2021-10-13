namespace PoweredSoft.Docker.MysqlBackup.Backup
{
    public class MySqlOptions
    {
        public string ConnectionString { get; set; }
        public bool UseMySqlDump { get; set; }
        public string MySqlDumpPath { get; set; }
    }
}
