using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace SingLuna.Core;

public class BotService(DiscordSocketClient client, CommandService commands, IServiceProvider services) 
    : IHostedService
{
    private readonly DiscordSocketClient _client = client;
    private readonly CommandService _commands = commands;
    private readonly IServiceProvider _services = services;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += HandleCommandAsync;

        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ??
                    throw new NullReferenceException("The Discord API token environment variable is not defined.");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await RegisterCommandsAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log);
        return Task.CompletedTask;
    }

    private Task ReadyAsync()
    {
        Console.WriteLine($"Bot connected as {_client.CurrentUser}");
        return Task.CompletedTask;
    }

    public async Task RegisterCommandsAsync()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        var message = messageParam as SocketUserMessage;
        var context = new SocketCommandContext(_client, message);

        if (message == null || message.Author.IsBot) return;

        int argPos = 0;
        if (message.HasCharPrefix('/', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
        {
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
                Console.WriteLine("Error: " + result.ErrorReason);
        }
    }
}