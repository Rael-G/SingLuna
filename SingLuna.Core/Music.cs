using Discord.Audio;
using YoutubeExplode.Search;

namespace SingLuna.Core;

public class Music(VideoSearchResult video)
{
    private const float DefaultVolume = 0.5f;
    public VideoSearchResult Video { get; set; } = video;

    private readonly CancellationTokenSource _cancellationToken = new();

    public async Task Play(IAudioClient client, Action musicEnded)
    {
        string audioPath = string.Empty;
        try
        {
            audioPath = await AudioService.DownloadAudioAsync(Video.Url, _cancellationToken.Token);
            using var audioStream = new FileStream(audioPath, FileMode.Open);
            await AudioService.SendAudioAsync(client, audioStream, DefaultVolume, _cancellationToken.Token);
        }
        finally
        {
            if (File.Exists(audioPath))
            {
                File.Delete(audioPath);
            }
        }

        musicEnded();
    }

    public void Stop()
    {
        _cancellationToken.Cancel();
    }
}
