using Discord.Audio;

namespace SingLuna.Core;

public class Playlist(IAudioClient client)
{
    private Music? _actual;

    private readonly Queue<Music> _queue = [];

    private readonly IAudioClient _client = client;

    public async Task Add(Music music)
    {
        _queue.Enqueue(music);

        if (_actual is null)
        {
            _actual = _queue.Dequeue();
            await _actual.Play(_client);
        }
    }

    public async Task Skip()
    {
        if (!_queue.TryDequeue(out var first))
            return;

        _actual?.Stop();
        _actual = first;
        await _actual.Play(_client);
    }

    public void Stop()
    {
        _actual?.Stop();
    }
}
