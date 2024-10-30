using System.Collections.Concurrent;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SingLuna.Core;

public class CommandsModule : ModuleBase<SocketCommandContext>
{
    private static ConcurrentDictionary<SocketGuild, Playlist> Guilds = [];

    [Command("play", RunMode = RunMode.Async)]
    [Summary("Search and play a music.")]
    public async Task PlayAsync([Remainder] string query)
    {
        if (!Guilds.TryGetValue(Context.Guild, out var playlist))
        {
            var channel = (Context.User as IVoiceState)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You need to be in a voice channel");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            playlist = new Playlist(audioClient);
            Guilds[Context.Guild] = playlist;
        }

        var video = await AudioService.SearchYouTube(query);
        var music = new Music(video);
        
        await ReplyAsync($"Playing {video.Title}");
        await playlist.Add(music);
    }

    [Command("skip", RunMode = RunMode.Async)]
    [Summary("Skip the current music.")]
    public async Task SkipAsync()
    {
        if (Guilds.TryGetValue(Context.Guild, out var playlist))
        {
            await playlist.Skip();
        }
    }

    [Command("stop", RunMode = RunMode.Async)]
    [Summary("Stop the Player.")]
    public Task StopAsync()
    {
        if (Guilds.TryGetValue(Context.Guild, out var playlist))
        {
            playlist.Stop();
            Guilds.TryRemove(Context.Guild, out _);
        }

        return Task.CompletedTask;
    }
}
