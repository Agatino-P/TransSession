using Toxiproxy.Net.Toxics;

namespace Shared.ToxiProxyTestContainer;

public sealed class ToxiProxyEndpoint
{
    public string Name { get; private set; }
    public string MappedHost { get; private set; }
    public int DockerPort { get; private set; }
    public int MappedPort { get; private set; }

    private ToxiProxyContainer _toxiProxyContainer;

    internal ToxiProxyEndpoint(
        ToxiProxyContainer toxiProxyContainer,
        string name,
        int dockerPort,
        string mappedHost,
        int mappedPort
    )
    {
        _toxiProxyContainer = toxiProxyContainer;

        Name = name;
        MappedHost = mappedHost;
        DockerPort = dockerPort;
        MappedPort = mappedPort;
    }

    public async Task EnableAsync() => await _toxiProxyContainer.SetEnabled(Name, true);

    public async Task DisableAsync() => await _toxiProxyContainer.SetEnabled(Name, false);

    public async Task AddLatencyAsync(
        int latencyMs,
        int jitterMs = 0,
        string toxicName = "latency",
        ToxicDirection direction = ToxicDirection.DownStream) =>
        await _toxiProxyContainer.AddLatencyAsync(Name, toxicName, latencyMs, jitterMs, direction);

    public async Task AddTimeoutAsync(
        int timeoutMs,
        string toxicName = "timeout",
        ToxicDirection direction = ToxicDirection.DownStream)
        => await _toxiProxyContainer.AddTimeoutAsync(Name, toxicName, timeoutMs, direction);

    public async Task AddResetPeerAsync(
        string toxicName = "reset",
        ToxicDirection direction = ToxicDirection.DownStream)
        => await _toxiProxyContainer.AddResetPeerAsync(Name, toxicName, direction);

    public async Task RemoveToxicAsync(string toxicName) =>
        await _toxiProxyContainer.RemoveToxicAsync(Name, toxicName);
}