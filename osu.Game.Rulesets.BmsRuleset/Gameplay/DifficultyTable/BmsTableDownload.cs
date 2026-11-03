using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;

internal static class BmsTableDownload
{
    // Retain HttpClient's former 100-second allowance, now independently cancellable
    // and diagnosable for each page, header and data request.
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(100);

    internal static async Task<string> GetStringAsync(HttpClient client, string url, TimeSpan timeout, CancellationToken cancellationToken)
        => (await GetAsync(client, url, timeout, cancellationToken).ConfigureAwait(false)).Content;

    internal static async Task<(string Content, Uri Uri)> GetAsync(HttpClient client, string url, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadAsStringAsync(deadline.Token).ConfigureAwait(false), response.RequestMessage?.RequestUri ?? new Uri(url));
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Difficulty table download timed out.", e);
        }
    }
}

internal sealed class BmsTableDownloadException(bool chartData, HttpRequestException cause)
    : HttpRequestException(cause.Message, cause, cause.StatusCode)
{
    internal bool ChartData { get; } = chartData;
}
