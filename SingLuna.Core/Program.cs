using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private IServiceProvider _services;

    public static async Task Main(string[] args) => await new Program().MainAsync();

    public Program()
    {
        _client = new DiscordSocketClient
        (
            new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                LogLevel = LogSeverity.Info,
            }
        );

        _commands = new CommandService();

        _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();
    }

    private async Task MainAsync()
    {
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += HandleCommandAsync;

        var token = Environment.GetEnvironmentVariable("DISCORD_API_TOKEN")??
            throw new NullReferenceException("The discord api token is null");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await RegisterCommandsAsync();
        
        await Task.Delay(-1);
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
            Console.WriteLine($"Command received: {message?.Content}");
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
                Console.WriteLine("Error: " + result.ErrorReason);
        }
    }
}