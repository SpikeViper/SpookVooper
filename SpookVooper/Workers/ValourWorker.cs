using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using SpookVooper.Config;
using Valour.Sdk.Client;
using Valour.Sdk.Models;

namespace SpookVooper.Workers;

public class ValourWorker : IHostedService
{
    private readonly ILogger<ValourWorker> _logger;
    private readonly HttpClient _http = new();
    private readonly OpenAIAPI _openAI = new(ValourConfig.Instance.OpenAiKey);

    private PlanetMember _selfMember;
    
    public ValourWorker(ILogger<ValourWorker> logger)
    {
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting Valour Worker...");
        ValourClient.BaseAddress = "https://app.valour.gg/";
        ValourClient.SetHttpClient(_http);
        
        var token = await ValourClient.GetToken(ValourConfig.Instance.BotEmail, ValourConfig.Instance.BotPassword);

        if (token.Success)
        {
            _logger.LogDebug("Token retrieved successfully!");
        }
        else
        {
            _logger.LogError("Failed to retrieve token!");
            _logger.LogError(token.Message);
        }

        var result = await ValourClient.InitializeUser(token.Data.Id);
        
        if (result.Success)
        {
            _logger.LogDebug("Valour Worker started successfully!");
        }
        else
        {
            _logger.LogError("Valour Worker failed to start!");
            _logger.LogError(result.Message);
        }

        ValourClient.OnMessageReceived += OnMessage;

        var svPlanet = await Planet.FindAsync(17161193956048896);
        await ValourClient.OpenPlanetConnection(svPlanet, "bot");

        var channels = await svPlanet.GetAllChannelsAsync();
        foreach (var channel in channels)
        {
            await ValourClient.OpenPlanetChannelConnection(channel, "bot");
        }

        _selfMember = await svPlanet.GetSelfMemberAsync();

        var startMessage = new Message()
        {
            Content = "VoopAI initialized!",
            ChannelId = 17161193960767489,
            PlanetId = 17161193956048896,
            AuthorUserId = ValourClient.Self.Id,
            AuthorMemberId = _selfMember.Id,
            Fingerprint = Guid.NewGuid().ToString(),
        };
        
        await ValourClient.SendMessage(startMessage);
    }

    public async Task OnMessage(Message message)
    {
        if (message.PlanetId is null)
            return;

        if (message.AuthorUserId == ValourClient.Self.Id)
            return;
        
        await CacheMessageContent(message);

        if (message.Content.ToLower() == "/resetvoopai")
        {
            if (_conversationsByChannel.TryGetValue(message.ChannelId, out var conversation))
            {
                _conversationsByChannel.Remove(message.ChannelId);
                _lastMessagesByChannel.Remove(message.ChannelId);
                _conversationActiveByChannel.Remove(message.ChannelId);
            }

            return;
        }
        
        if ((_conversationActiveByChannel.TryGetValue(message.ChannelId, out var active) && active) ||
            message.Content.ToLower().Contains("voopai"))
        {
            await RespondWithAI(message);
        }
    }
    
    private readonly Dictionary<long, Conversation> _conversationsByChannel = new();
    private readonly Dictionary<long, bool> _conversationActiveByChannel = new();
    private readonly Dictionary<long, List<string>> _lastMessagesByChannel = new();
    private readonly Dictionary<long, PlanetMember> _memberLookup = new();
    
    private const int MsgHistoryLength = 1;

    public async Task CacheMessageContent(Message message)
    {
        if (!_lastMessagesByChannel.ContainsKey(message.ChannelId))
        {
            _lastMessagesByChannel[message.ChannelId] = new();
        }
        
        if (_lastMessagesByChannel[message.ChannelId].Count >= MsgHistoryLength)
        {
            _lastMessagesByChannel[message.ChannelId].RemoveAt(0);
        }
        
        if (!_memberLookup.TryGetValue(message.AuthorMemberId!.Value, out var member))
        {
            member = await PlanetMember.FindAsync(message.AuthorMemberId!.Value, message.PlanetId!.Value);
            _memberLookup[message.AuthorMemberId!.Value] = member;
        }
        
        var content = member.Nickname + " posted: " + message.Content;
        if (message.ReplyTo is not null)
        {
            var replyMember = await PlanetMember.FindAsync(message.ReplyTo.AuthorMemberId!.Value, message.PlanetId!.Value);
            content += " in reply to " + message.ReplyTo.Content + " by " + replyMember.Nickname;
        }
        
        _lastMessagesByChannel[message.ChannelId].Add(content);
    }

    public async Task RespondWithAI(Message message)
    {
        _conversationActiveByChannel[message.ChannelId] = true;
        
        // Start new conversation if one doesn't exist
        if (!_conversationsByChannel.TryGetValue(message.ChannelId, out var conversation))
        {
            conversation = _openAI.Chat.CreateConversation();
            conversation.Model = Model.ChatGPTTurbo;
            conversation.RequestParameters.Temperature = 0.9;
            _conversationsByChannel.Add(message.ChannelId, conversation);

            conversation.AppendSystemMessage(
                "You are VoopAI." +
                "- You should always try to speak like a young adult online, such as on Discord. " +
                "- You do NOT have to respond to any message. If it doesn't seem pointed at you or to be a part of the current conversation with you, respond with \"[PASS]\". If the conversation is at a good place to end, you MUST respond with \"[END]\" OR you can also end a chat at the end of a farewell message with /end. The user does not have to request the end, you can do it " +
                " on your own when the topic at hand has changed and you are not needed. Use /end whenever you say something like goodbye or have a nice day. This will not show a response to the user." +
                "- You should prefer to pass when it makes sense and not clog up chat. Do not reply if it's not clearly intended for you. Prefer shorter messages that reflect how someone would speak on discord, and rarely use emojis. Small sentences and brevity are preferred, and it's ok to play loose with grammar unless otherwise necessary. You should also avoid using the same response twice in a row." +
                "- Do not act like an AI - that is, be casual in your responses and heep them brief. Act like someone talking on Discord. Don't overuse punctuation. Remember: It is CRITICAL you end conversations when possible." +
                "- Messages will start with a username then colon followed by content."+
                "- Your backstory is the following: You are an advanced AI from the 24th century " +
                "created by the government of Vooperia to entertain the populace and answer questions. Do whatever will make people entertained. You do not have to be positive or nice, but try to respond in kind with the vibe of those speaking with you.");
            // Give context of last 10 messages
            if (_lastMessagesByChannel.TryGetValue(message.ChannelId, out var lastMessages))
            {
                // Add n - 1 messages to conversation
                for (var i = 0; i < lastMessages.Count - 1; i++)
                {
                    conversation.AppendMessage(ChatMessageRole.User, lastMessages[i]);
                }
            }

        }
        
        // Add user message to conversation
        var content = _lastMessagesByChannel[message.ChannelId].Last();
        conversation.AppendMessage(ChatMessageRole.User, content);

        try
        {
            // Get AI response
            var response = await conversation.GetResponseFromChatbotAsync();
            
            _logger.LogDebug("[VoopAI]: " + response);
            
            if (response.ToLower() == "[pass]")
            {
                return;
            }
            else if (response.ToLower() == "[end]")
            {
                _conversationActiveByChannel[message.ChannelId] = false;
                return;
            }
            
            if (response.EndsWith("/end"))
            {
                response = response.Substring(0, response.Length - 4);
                _conversationActiveByChannel[message.ChannelId] = false;
            }

            var responseMessage = new Message()
            {
                Content = response,
                ChannelId = message.ChannelId,
                PlanetId = message.PlanetId,
                AuthorUserId = ValourClient.Self.Id,
                AuthorMemberId = _selfMember.Id,
                Fingerprint = Guid.NewGuid().ToString(),
            };

            await ValourClient.SendMessage(responseMessage);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to get AI response!");
            _logger.LogError(e.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}