using Docker.DotNet.Models;
using Toxiproxy.Net.Toxics;

namespace Shared.Infrastructure.ToxiProxyTestContainer;

public sealed class ToxiProxyEndpoint
{
    public string Name { get; private set; }
    public string MappedHost { get; private set; }
    public int DockerPort { get; private set; }
    public int MappedPort { get; private set; }

    private readonly Func<Task> _disable;
    private readonly Func<Task> _enable;
    private readonly Func<string, int, int, ToxicDirection, Task> _addLatency;
    private readonly Func<string, int, ToxicDirection, Task> _addTimeout;
    private readonly Func<string, Task> _removeToxic;

    internal ToxiProxyEndpoint(
        string name,
        int dockerPort,
        string mappedHost,
        int mappedPort,
        Func<Task> disable,
        Func<Task> enable,
        Func<string, int, int, ToxicDirection, Task> addLatency,
        Func<string, int, ToxicDirection, Task> addTimeout,
        Func<string, Task> removeToxic)
    {
        Name = name;
        MappedHost = mappedHost;
        DockerPort = dockerPort;
        MappedPort = mappedPort;
        _disable = disable;
        _enable = enable;
        _addLatency = addLatency;
        _addTimeout = addTimeout;
        _removeToxic = removeToxic;
    }

    public Task DisableAsync() => _disable();
    public Task EnableAsync() => _enable();

    public Task AddLatencyAsync(
        int latencyMs,
        int jitterMs = 0,
        string toxicName = "latency",
        ToxicDirection direction = ToxicDirection.DownStream)
        => _addLatency(toxicName, latencyMs, jitterMs, direction);

    public Task AddTimeoutAsync(
        int timeoutMs,
        string toxicName = "timeout",
        ToxicDirection direction = ToxicDirection.DownStream)
        => _addTimeout(toxicName, timeoutMs, direction);

    public Task RemoveToxicAsync(string toxicName) => _removeToxic(toxicName);
}