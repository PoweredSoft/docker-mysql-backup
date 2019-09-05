using FluentFTP;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup
{
    public interface IFtpConnectionProvider
    {
        Task<FtpClient> GetConnectedClient();
    }
}
