using CliWrap;
using Discord.Audio;
using YoutubeExplode;
using YoutubeExplode.Search;

namespace SingLuna.Core;

internal static class AudioService
{
    private static readonly YoutubeClient _youtubeClient = new();

    public static async Task<VideoSearchResult> SearchYouTube(string query, CancellationToken cancelationToken = default)
    {
        var videos = _youtubeClient.Search.GetVideosAsync(query, cancelationToken);
        return await videos.FirstAsync(cancelationToken);
    }

    public static async Task<Stream> DownloadAudioAsync(string url, CancellationToken cancelationToken = default)
    {
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(url, cancelationToken);
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().First();

        return await _youtubeClient.Videos.Streams.GetAsync(audioStreamInfo, cancelationToken);
    }

    public static async Task SendAudioAsync(IAudioClient audioClient, Stream audioStream, CancellationToken cancelationToken = default)
    {
        var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");
        MemoryStream ffmpegStream = new();
        await Cli.Wrap(ffmpegPath)
            .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
            .WithStandardInputPipe(PipeSource.FromStream(audioStream))
            .WithStandardOutputPipe(PipeTarget.ToStream(ffmpegStream))
            .ExecuteAsync(cancelationToken);

        using AudioOutStream discordStream = audioClient.CreatePCMStream(AudioApplication.Mixed);
        try { await discordStream.WriteAsync(ffmpegStream.ToArray(), 0, (int)ffmpegStream.Length, cancelationToken); }
        finally { await discordStream.FlushAsync(cancelationToken); }
    }
}