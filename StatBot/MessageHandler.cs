﻿using CSharpVerbalExpressions;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StatBot
{
    public class MessageHandler
    {
        private static readonly string commandPrefix = Bot.Default.CommandPrefix;
        //example: <:phew:19095755581184>
        private readonly VerbalExpressions emojiExpression = new VerbalExpressions().StartOfLine().Anything().Then("<:").Anything().Then(":").Anything();
        //example: <@19095755581184>
        private readonly VerbalExpressions userMentionExpression = new VerbalExpressions().StartOfLine().Anything().Then("<@").Anything().Then(">").Anything();
        //example: <#19095755581184>
        private readonly VerbalExpressions channelExpression = new VerbalExpressions().StartOfLine().Anything().Then("<#").Anything().Then(">").Anything();
        //example: !command
        private readonly VerbalExpressions commandExpression = new VerbalExpressions().StartOfLine().Then(commandPrefix).Anything();
        internal DiscordSocketClient _client;

        public MessageHandler(DiscordSocketClient client)
        {
            _client = client;
        }

        public string HandleMessage(string message, string userName)
        {
            message = Regex.Replace(message, @"\r\n?|\n", " ");
            StringBuilder returnMessage = new StringBuilder();
            if (emojiExpression.IsMatch(message) ||
                userMentionExpression.IsMatch(message) ||
                channelExpression.IsMatch(message) ||
                commandExpression.IsMatch(message))
            {
                string[] messageParts = message.Split(' ');
                bool firstPart = true;
                foreach (string messagePart in messageParts)
                {
                    if (firstPart &&
                        commandExpression.IsMatch(messagePart))
                    {
                        CommandHandler.HandleCommand(messagePart, userName);
                    }
                    if (emojiExpression.IsMatch(messagePart))
                    {
                        string[] splitString = messagePart.Split('>');
                        returnMessage.Append($":{splitString[0].Split(':')[1]}: ");
                        if (splitString.Length > 1)
                        {
                            returnMessage.Append(splitString[1]);
                        }                        
                    }
                    else if (userMentionExpression.IsMatch(messagePart))
                    {
                        returnMessage.Append(ResolveUserMention(messagePart));
                    }
                    else if (channelExpression.IsMatch(messagePart))
                    {
                        returnMessage.Append(ResolveChannelName(messagePart));
                    }
                    else
                    {
                        returnMessage.Append($"{messagePart} ");
                    }
                    firstPart = false;
                }
            }
            return returnMessage.Length > 0 ? returnMessage.ToString() : message;
        }

        private StringBuilder ResolveUserMention(string messagePart)
        {
            StringBuilder returnMessage = new StringBuilder();
            string[] messagePartPart = messagePart.Split('>');
            ulong userId = 0;
            int minusLength = 3;
            string substr = string.Empty;
            if (messagePartPart.Length >= 2 &&
                !string.IsNullOrEmpty(messagePartPart[1]))
            {
                minusLength++;
            }
            else
            {
                substr = messagePartPart[0].Substring(2, messagePart.Length - minusLength);
                ulong.TryParse(substr, out userId);
            }

            if (userId == 0)
            {
                minusLength++;
                substr = messagePartPart[0].Substring(3, messagePart.Length - minusLength);
                ulong.TryParse(substr, out userId);
            }
            if (userId > 0)
            {
                SocketUser user = ResolveUser(userId);
                returnMessage.Append($"@{user.Username}#{user.Discriminator} ");
                if (messagePartPart.Length >= 2 &&
                !string.IsNullOrEmpty(messagePartPart[1]))
                {
                    returnMessage.Append(messagePartPart[1]);
                }
            }
            else
            {
                returnMessage.Append($"{messagePart} ");
            }
            return returnMessage;
        }

        private StringBuilder ResolveChannelName(string messagePart)
        {
            StringBuilder returnMessage = new StringBuilder();
            string[] channelPartPart = messagePart.Split('>');
            ulong channelId = 0;
            int minusLength = 3;
            string substr = string.Empty;
            if (channelPartPart.Length >= 2 &&
                !string.IsNullOrEmpty(channelPartPart[1]))
            {
                minusLength++;
            }
            else
            {
                substr = channelPartPart[0].Substring(2, messagePart.Length - minusLength);
                ulong.TryParse(substr, out channelId);
            }

            if (channelId == 0)
            {
                substr = channelPartPart[0].Substring(2, messagePart.Length - minusLength);
                ulong.TryParse(substr, out channelId);
            }
            if (channelId > 0)
            {
                var channel = _client.GetChannel(channelId);
                returnMessage.Append($"#{channel} ");
                if (channelPartPart.Length >= 2 &&
                !string.IsNullOrEmpty(channelPartPart[1]))
                {
                    returnMessage.Append(channelPartPart[1]);
                }
            }
            else
            {
                returnMessage.Append($"{messagePart} ");
            }
            return returnMessage;
        }

        private SocketUser ResolveUser(ulong userId)
        {
            var user = _client.GetUser(userId);
            //Search in the guild if it cannot be found.
            if (user == null)
            {
                List<SocketGuildUser> users = new List<SocketGuildUser>();
                foreach (SocketGuild guild in _client.Guilds)
                {
                    users.AddRange(guild.Users);
                }

                user = users.FirstOrDefault(x => x.Id == userId);
            }
            return user;
        }
    }
}
