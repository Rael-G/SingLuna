using CliWrap;
using Discord;
using Discord.Commands;

namespace SingLuna.Core;

public class CommandsModule : ModuleBase<SocketCommandContext>
{
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

        var video = await AudioService.SearchYouTube(query);

        await ReplyAsync($"Playing {video.Title}");

        var audioStream = await AudioService.DownloadAudioAsync(video.Url);

        var audioClient = await voiceChannel.ConnectAsync();

        await AudioService.SendAudioAsync(audioClient, audioStream);
    }
}
