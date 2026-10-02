using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace AwesomeApi;

public class ShortLivedTokenStore
{
    private readonly ConcurrentDictionary<string, string> _tokenStore = [];
    private readonly TimeSpan _lifeTime = TimeSpan.FromSeconds(10);

    public string GetToken(string userId)
    {
        var token = Guid.NewGuid().ToString();
        _tokenStore.TryAdd(token, userId);

        _ = Task.Run(async () =>
        {
            await Task.Delay(_lifeTime);
            _tokenStore.TryRemove(token, out _);
        });

        return token;
    }

    public bool TryConsume(string token, [NotNullWhen(true)] out string? userId) => _tokenStore.TryRemove(token, out userId);
}