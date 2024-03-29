﻿using Ionic.Zip;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using PoweredSoft.Docker.MysqlBackup.Notifications;
using PoweredSoft.Storage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup.Backup
{
    public class BackupTask : ITask
    {
        private readonly INotifyService notifyService;
        private readonly IStorageProvider storageProvider;
        private readonly IConfiguration configuration;
        private readonly BackupOptions backupOptions;
        private readonly MySqlOptions mySqlOptions;

        public BackupTask(INotifyService notifyService, IStorageProvider storageProvider, IConfiguration configuration, BackupOptions backupOptions, MySqlOptions mySqlOptions)
        {
            this.notifyService = notifyService;
            this.storageProvider = storageProvider;
            this.configuration = configuration;
            this.backupOptions = backupOptions;
            this.mySqlOptions = mySqlOptions;
        }

        public int Priority { get; } = 1;
        public string Name { get; } = "MySQL Database backup task.";

        protected virtual MySqlConnection GetDatabaseConnection()
        {
            var connectionStringConfig = mySqlOptions.ConnectionString;
            var connectionString = connectionStringConfig.TrimEnd(';');
            connectionString += ";charset=utf8;convertzerodatetime=true;";
            var ret = new MySqlConnection(connectionString);
            return ret;
        }

        protected virtual List<string> GetDatabaseNames(MySqlConnection connection)
        {
            var ret = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "show databases;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        ret.Add((string)reader[0]);
                }
            }

            var systemTables = new List<string>()
            {
                "sys", "information_schema", "performance_schema", "mysql"
            };

            ret.RemoveAll(t => systemTables.Contains(t));
            return ret;
        }

        public async Task<int> RunAsync()
        {
            // get the mysql connection
            Console.WriteLine("Attempting connection to database...");
            using (var connection = GetDatabaseConnection())
            {
                await connection.OpenAsync();
                Console.WriteLine("Connected to database...");
                Console.WriteLine("Fetching database names to backup...");
                var databaseNames = GetDatabaseNames(connection);

                foreach (var databaseName in databaseNames)
                {
                    if (backupOptions.Databases != "*")
                    {
                        var databasesToBackup = backupOptions.Databases.Split(',');
                        if (!databasesToBackup.Any(t => t.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            Console.WriteLine($"Skipping {databaseName} not part of {backupOptions.Databases}");
                            continue;
                        }
                    }

                    Console.WriteLine($"Currently on database {databaseName}");
                    await BackupUsingMysqlDump(databaseName);
                }
            }

            if (this.backupOptions.NotifySuccess)
            {
                await notifyService.SendNotification("Backup Success", $"The backup of the server {GetHostName(this.mySqlOptions)} is successful.", facts: new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Completed At", $"{DateTimeOffset.UtcNow}" }
                });
            }

            return 0;
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

        private void ExecuteLinux(string databaseName, string tempFile)
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder(mySqlOptions.ConnectionString);
            var hostname = connectionStringBuilder.Server;
            var port = connectionStringBuilder.Port;
            var username = connectionStringBuilder.UserID;
            var password = connectionStringBuilder.Password;

            var dumpExe = mySqlOptions.MySqlDumpPath ?? "mysqldump";
            var command = $"{dumpExe} -h {hostname} -u {username} -p{password} {databaseName} --port {port} > {tempFile}";
            var result = "";
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "/bin/bash";
                proc.StartInfo.Arguments = "-c \" " + command + " \"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();

                result += proc.StandardOutput.ReadToEnd();
                result += proc.StandardError.ReadToEnd();

                Console.WriteLine(result);
                proc.WaitForExit();
            }
        }

        private void ExecuteWindows(string databaseName, string tempFile)
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder(mySqlOptions.ConnectionString);
            var hostname = connectionStringBuilder.Server;
            var port = connectionStringBuilder.Port;
            var username = connectionStringBuilder.UserID;
            var password = connectionStringBuilder.Password;

            var dumpExe = mySqlOptions.MySqlDumpPath ?? "/mysqldump";


            // starting process to backup file.
            Console.WriteLine($"Starting backup with mysql dump on database {databaseName} on server {hostname}");
            var command = $"\"{dumpExe}\" --column-statistics=0 -h {hostname} -u {username} -p{password} {databaseName} --port {port} > {tempFile}";

            var batFilePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid() + ".bat");

            File.WriteAllText(
                    batFilePath,
                    command,
                    Encoding.ASCII);

            if (File.Exists(tempFile))
                File.Delete(tempFile);

            try
            {
                var oInfo = new ProcessStartInfo(batFilePath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(oInfo))
                {
                    if (proc == null) return;
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        Console.WriteLine($"mysqldump exited with code {proc.ExitCode} for database {databaseName}");
                        throw new Exception($"mysqldump exited with code {proc.ExitCode} for database {databaseName}");
                    }

                    proc.Close();


                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                File.Delete(batFilePath);
            }
        }

        private async Task BackupUsingMysqlDump(string databaseName)
        {
            

            // file names.
            var tempFile = Path.GetTempFileName();
            var zippedTempFile = Path.GetTempFileName();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ExecuteWindows(databaseName, tempFile);
            }
            else
            {
                ExecuteLinux(databaseName, tempFile);
            }

           
            Console.WriteLine($"Starting to compress backup...");
            using (var zip = new ZipFile())
            {
                zip.AddFile(tempFile, "").FileName = $"{databaseName}.sql";
                zip.Save(zippedTempFile);

                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not clean up temp file {tempFile} {ex.Message}");
                }

                Console.WriteLine("Succesfully created compressed mysql backup file.");
            }

            var destination = $"{backupOptions.BasePath}/{databaseName}_{DateTime.Now:yyyyMMdd_hhmmss_fff}.sql.zip";
            using (var fs = new FileStream(zippedTempFile, FileMode.Open, FileAccess.Read))
            {
                await storageProvider.WriteFileAsync(fs, destination);
                Console.WriteLine("Succesfully transfered backup to storage");
            }

            try
            {
                File.Delete(zippedTempFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not clean up temp file {zippedTempFile} {ex.Message}");
            }
        }
    }
}
