using Discord.Audio;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Converter;
using NAudio.Wave;

namespace SingLuna.Core;

internal static class AudioService
{
    private static readonly YoutubeClient _youtubeClient = new();

    public static async Task<VideoSearchResult> SearchYouTube(string query, CancellationToken cancellationToken = default)
    {
        var videos = _youtubeClient.Search.GetVideosAsync(query, cancellationToken);
        return await videos.FirstAsync(cancellationToken);
    }

    public static async Task<string> DownloadAudioAsync(string url, CancellationToken cancellationToken = default)
    {
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(url, cancellationToken);
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().First();
        var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg");
        var videoPath = Path.GetTempFileName();
        
        await _youtubeClient.Videos.DownloadAsync(
            url,
            videoPath,
            o => o.SetContainer("wav")
                .SetPreset(ConversionPreset.UltraFast)
                .SetFFmpegPath(ffmpegPath),
            null,
            cancellationToken
        );

        return videoPath;
    }

    public static async Task SendAudioAsync(IAudioClient audioClient, Stream audioStream, float volume = 1f, CancellationToken cancellationToken = default)
    {
        using var discordStream = audioClient.CreatePCMStream(AudioApplication.Mixed);

        try
        {
            using var waveReader = new WaveFileReader(audioStream);
            var volumeProvider = new VolumeWaveProvider16(waveReader) { Volume = Math.Clamp(volume, 0f, 1f) };
            
            byte[] buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = volumeProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                await discordStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken);
            }
        }
        finally
        {
            await discordStream.FlushAsync(cancellationToken);
        }
    }
}