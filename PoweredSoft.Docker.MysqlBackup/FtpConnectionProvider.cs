using FluentFTP;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup
{
    public class FtpConnectionProvider : IFtpConnectionProvider
    {
        private readonly IConfiguration configuration;

        public FtpConnectionProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<FtpClient> GetConnectedClient()
        {
            var host = configuration["FTP:Host"];
            var port = configuration["FTP:Port"];
            var username = configuration["FTP:User"];
            var pw = configuration["FTP:Password"];

            var client = new FtpClient(host, int.Parse(port), username, pw);

            await client.ConnectAsync();

            return client;
        }
    }
}
