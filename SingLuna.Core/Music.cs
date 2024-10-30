using Discord.Audio;
using YoutubeExplode.Search;

namespace SingLuna.Core;

public class Music(VideoSearchResult video)
{
    public VideoSearchResult Video { get; set; } = video;

    private readonly CancellationTokenSource _cancelationToken = new();

    public async Task Play(IAudioClient client)
    {
        var audioStream = await AudioService.DownloadAudioAsync(Video.Url, _cancelationToken.Token);
        await AudioService.SendAudioAsync(client, audioStream, _cancelationToken.Token);
    }

    public void Stop()
    {
        _cancelationToken.Cancel();
    }
}
