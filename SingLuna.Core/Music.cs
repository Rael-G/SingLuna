using Discord.Audio;
using YoutubeExplode.Search;

namespace SingLuna.Core;

public class Music(VideoSearchResult video)
{
    public VideoSearchResult Video { get; set; } = video;

    private readonly CancellationTokenSource _cancellationToken = new();

    public async Task Play(IAudioClient client, Action musicEnded)
    {
        try
        {
            var audioStream = await AudioService.DownloadAudioAsync(Video.Url, _cancellationToken.Token);
            await AudioService.SendAudioAsync(client, audioStream, _cancellationToken.Token);
        }
        catch(OperationCanceledException e)
        {
            Console.WriteLine(e.Message);
        }

        musicEnded();
    }

    public void Stop()
    {
        _cancellationToken.Cancel();
    }
}
