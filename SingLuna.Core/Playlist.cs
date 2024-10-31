using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace SingLuna.Core;

public class Playlist(IAudioClient client, ITextChannel channel, SocketGuild guild, Action<SocketGuild> playlistEnded)
{
    private Music? _actual;
    private readonly Queue<Music> _queue = [];

    private readonly IAudioClient _client = client;
    private readonly ITextChannel _channel = channel;
    private readonly SocketGuild _guild = guild;

    private event Action<SocketGuild> PlaylistEnded = playlistEnded;

    public async Task Add(Music music)
    {
        _queue.Enqueue(music);
        if (_actual is null)
        {
            await Play();
        }
        else
        {
            await ReplyAsync($"Adding \"{music.Video.Title}\" to the Playlist");
        }
    }

    public async Task Play()
    {
        if (!_queue.TryDequeue(out var music))
        {
            await EndPlaylist();
            return;
        }
        _actual = music;
        await ReplyAsync($"Playing \"{_actual!.Video.Title}\" - {_actual.Video.Duration}");
        await _actual.Play(_client, OnMusicEnd);
    }

    public async Task Skip()
    {
        if (_actual is not null)
        {
            await ReplyAsync($"Skiping \"{_actual.Video.Title}\"");
            _actual.Stop();
        }
    }

    public async Task Stop()
    {
        await ReplyAsync("Stopping");
        _actual?.Stop();
        try { await _client.StopAsync(); }
        catch(OperationCanceledException e)
        { Console.WriteLine(e.Message); }
        finally
        { PlaylistEnded(_guild); }
    }

    public async Task EndPlaylist()
    {
        await ReplyAsync("There is no more musics on the playlist");
        await Stop();
    }

    private async void OnMusicEnd()
    {
        await Play();
    }

    private async Task ReplyAsync(string message)
        => await _channel.SendMessageAsync(message);
    
}
