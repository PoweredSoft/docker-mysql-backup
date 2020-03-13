using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Teams.Notifications;

namespace PoweredSoft.Docker.MysqlBackup.Notifications.Teams
{
    public class TeamsNotificationService : INotificationService
    {
        private readonly TeamsConfiguration configuration;

        public TeamsNotificationService(TeamsConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendNotification(string title, string message, Dictionary<string, string> facts = null, string color = null)
        {
            if (!configuration.Enabled)
                return;

            using var client = new TeamsNotificationClient(configuration.Webhook);
            
            var messageCard = new MessageCard
            {
                Title = title,
                Text = message,
                Color = color
            };

            if (facts != null)
            {
                messageCard.Sections = new List<MessageSection>()
                {
                    new MessageSection
                    {
                        Facts = facts.Select(t => new MessageFact
                        {
                            Name = t.Key,
                            Value = t.Value
                        }).ToList()
                    }
                };
            }

            await client.PostMessage(messageCard);
        }
    }
}
