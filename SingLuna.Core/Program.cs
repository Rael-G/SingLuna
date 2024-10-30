using System.Reflection;
using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using YoutubeExplode;
using YoutubeExplode.Search;

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

public class MusicModule : ModuleBase<SocketCommandContext>
{
    private readonly YoutubeClient _youtubeClient = new YoutubeClient();

    [Command("play", RunMode = RunMode.Async)]
    [Summary("Play")]
    public async Task PlayAsync([Remainder] string query)
    {
        var voiceChannel = (Context.User as IVoiceState)?.VoiceChannel;
        if (voiceChannel == null)
        {
            await ReplyAsync("You need to be in a voice channel");
            return;
        }

        await ReplyAsync($"Pesquisando por: {query}");
        var video = await SearchYouTube(query);
        if (video == null)
        {
            await ReplyAsync("Não encontrei nenhum resultado.");
            return;
        }

        await ReplyAsync($"Tocando: {video.Title}");

        var audioStream = await DownloadAudioAsync(video.Url);
        if (audioStream == null)
        {
            await ReplyAsync("Falha ao baixar o áudio.");
            return;
        }

        var audioClient = await voiceChannel.ConnectAsync();

        await SendAudioAsync(audioClient, audioStream);
    }

    private async Task<VideoSearchResult?> SearchYouTube(string query)
    {
        var videos = _youtubeClient.Search.GetVideosAsync(query);
        return await videos.FirstOrDefaultAsync();
    }

    private async Task<Stream?> DownloadAudioAsync(string url)
    {
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(url);
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().FirstOrDefault();
        
        if (audioStreamInfo == null)
            return null;

        return await _youtubeClient.Videos.Streams.GetAsync(audioStreamInfo);
    }

    private static async Task SendAudioAsync(IAudioClient audioClient, Stream audioStream)
    {
        var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");
        MemoryStream ffmpegStream = new();
        await Cli.Wrap(ffmpegPath)
            .WithArguments(" -hide_banner -loglevel verbose -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
            .WithStandardInputPipe(PipeSource.FromStream(audioStream))
            .WithStandardOutputPipe(PipeTarget.ToStream(ffmpegStream))
            .ExecuteAsync();

        using AudioOutStream discordStream = audioClient.CreatePCMStream(AudioApplication.Mixed);
        try { await discordStream.WriteAsync(ffmpegStream.ToArray(), 0, (int)ffmpegStream.Length); }
        finally { await discordStream.FlushAsync(); }
    }

}
