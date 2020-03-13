using Slack.Webhooks;
using Slack.Webhooks.Blocks;
using Slack.Webhooks.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MysqlBackup.Notifications.Slack
{
    class SlackNotificationService : INotificationService
    {
        private readonly SlackConfiguration configuration;

        public SlackNotificationService(SlackConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendNotification(string title, string message, Dictionary<string, string> facts = null, string color = null)
        {
            if (!configuration.Enabled)
                return;

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


            var blocks = new List<Block>();

            blocks.Add(new Section
            {
                Text = new TextObject($"*{title}*")
                {
                    Type = TextObject.TextType.Markdown
                },
                Fields = new List<TextObject>
                {
                    new TextObject { Text = message }
                }
            });

            if (facts != null)
            {
                var factMarkDown = string.Join("\n", facts.Select(t => $"> *{t.Key}* : {t.Value}"));
                blocks.Add(new Section
                {
                    Text = new TextObject(factMarkDown)
                    {
                        Type = TextObject.TextType.Markdown
                    }
                });
            }

            var slackMessage = new SlackMessage
            {
                IconEmoji = Emoji.Ghost,
                Username = "MySQLBackup",
                Text = title,
                Blocks = blocks
            };

            var slackClient = new SlackClient(configuration.Webhook);
            await slackClient.PostAsync(slackMessage);
        }
    }
}
