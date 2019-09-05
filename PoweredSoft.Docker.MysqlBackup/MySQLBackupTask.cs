using FluentFTP;
using Ionic.Zip;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup
{
    public class MySQLBackupTask : ITask
    {
        private readonly IConfiguration configuration;
        private readonly IFtpConnectionProvider ftpConnectionProvider;

        public MySQLBackupTask(IConfiguration configuration, IFtpConnectionProvider ftpConnectionProvider)
        {
            this.configuration = configuration;
            this.ftpConnectionProvider = ftpConnectionProvider;
        }

        public int Priority => 1;
        public string Name => "Backup Databases";

        protected virtual MySqlConnection GetDatabaseConnection()
        {
            var connectionStringConfig = configuration["MySQL:ConnectionString"];
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
            var ftpBackupDir = configuration["FTP:BackupDestination"];
            Console.WriteLine($"FTP Destination {ftpBackupDir}");

            Console.WriteLine("Connecting to FTP Server...");
            var ftp = await ftpConnectionProvider.GetConnectedClient();

            // get the mysql connection
            Console.WriteLine("Attempting connection to database...");
            using (var connection = GetDatabaseConnection())
            {
                await connection.OpenAsync();
                Console.WriteLine("Connected to database...");
                Console.WriteLine("Fetching database names to backup...");
                var databaseNames = GetDatabaseNames(connection);

                foreach(var databaseName in databaseNames)
                {
                    var tempFile = Path.GetTempFileName();
                    var zippedTempFile = Path.GetTempFileName();

                    // switch to this database.
                    using (var command = connection.CreateCommand())
                    {
                        // switch to the current database.
                        Console.WriteLine($"Switching to database: {databaseName}");
                        command.CommandText = $"use `{databaseName}`;";
                        command.ExecuteNonQuery();
                        Console.WriteLine($"Switched on {databaseName}");

                        using (MySqlBackup mb = new MySqlBackup(command))
                        {
                            Console.WriteLine($"Exporting backup to temp file {tempFile}");
                            mb.ExportToFile(tempFile);
                            Console.WriteLine($"Exported backup to temp file {tempFile}");

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
                        }

                      
                        var destination = $"{ftpBackupDir}/{databaseName}/{databaseName}_{DateTime.Now:yyyyMMdd_hhmmss_fff}.sql.zip";
                        using (var fs = new FileStream(zippedTempFile, FileMode.Open, FileAccess.Read))
                        {
                            var result = await ftp.UploadAsync(fs, destination, existsMode: FtpExists.Overwrite, createRemoteDir: true);
                            Console.WriteLine("Succesfully transfered backup to FTP host.");
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
                };
            }

            await ftp.DisconnectAsync();
            return 0;
        }
    }
}
