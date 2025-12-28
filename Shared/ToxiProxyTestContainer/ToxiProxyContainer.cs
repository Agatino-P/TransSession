using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.Toxiproxy;
using Toxiproxy.Net;
using Toxiproxy.Net.Toxics;

namespace Shared.ToxiProxyTestContainer;

public sealed class ToxiProxyContainer : IAsyncDisposable
{
    private readonly IContainer _container;

    private Connection _connection = null!;
    private Client _client = null!;

    private ushort _nextListenPort = ToxiproxyBuilder.FirstProxiedPort;
    private const string _defaultImage = "ghcr.io/shopify/toxiproxy:2.5.0";

    public ToxiProxyContainer(string? image = null, INetwork? network = null, string? networkAlias = null)
    {
        ToxiproxyBuilder? builder = new ToxiproxyBuilder().WithImage(image ?? _defaultImage);
        if (network is not null)
        {
            builder = builder.WithNetwork(network);
        }

        if (networkAlias is not null)
        {
            builder = builder.WithNetworkAliases(networkAlias);
        }


        _container = builder.Build();
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _container.StartAsync(ct);

        _connection = new Connection(_container.Hostname, _container.GetMappedPublicPort());
        _client = _connection.Client();
    }

    public async Task StopAsync(CancellationToken ct = default)
        => await _container.StopAsync(ct);

    public async Task<ToxiProxyEndpoint> CreateProxyAsync(
        string name,
        string proxiedHost,
        int proxiedPort,
        ushort? listenPortInContainer = null, // optional; otherwise auto-assigned
        CancellationToken cancellationToken = default)
    {
        ushort listenPort = listenPortInContainer ?? _nextListenPort++;

        await _client.AddAsync(new Proxy
        {
            Name = name, Enabled = true, Listen = $"0.0.0.0:{listenPort}", Upstream = $"{proxiedHost}:{proxiedPort}"
        });

        return new ToxiProxyEndpoint(
            toxiProxyContainer: this,
            name: name,
            dockerPort: listenPort,
            mappedPort: _container.GetMappedPublicPort(listenPort),
            mappedHost: _container.Hostname
        );
    }


    public async ValueTask DisposeAsync()
    {
        try { _connection.Dispose(); }
        // ReSharper disable once EmptyGeneralCatchClause
        catch { }

        await _container.DisposeAsync();
    }

    public async Task SetEnabled(string proxyName, bool enabled)
    {
        //This can generate race condition as it is reported to return before the proxy is effectively enabled/disabled
        //use with caution

        Proxy proxy = await _client.FindProxyAsync(proxyName);
        proxy.Enabled = enabled;
        await _client.UpdateAsync(proxy);
    }

    public async Task AddLatencyAsync(string proxyName, string toxicName, int latencyMs, int jitterMs,
        ToxicDirection direction)
    {
        Proxy proxy = await FindProxyByName(proxyName);

        await proxy.AddAsync(new LatencyToxic
        {
            Name = toxicName,
            Stream = direction,
            Toxicity = 1,
            Attributes = { Latency = latencyMs, Jitter = jitterMs }
        });
    }

    public async Task<Proxy> FindProxyByName(string proxyName)
    {
        return await _client.FindProxyAsync(proxyName);
    }

    public async Task AddTimeoutAsync(string proxyName, string toxicName, int timeoutMs, ToxicDirection direction)
    {
        Proxy proxy = await FindProxyByName(proxyName);
        await proxy.AddAsync(new TimeoutToxic
        {
            Name = toxicName, Stream = direction, Toxicity = 1, Attributes = { Timeout = timeoutMs }
        });
    }

    public async Task AddResetPeerAsync(string proxyName, string toxicName, ToxicDirection direction)
    {
        Proxy proxy = await FindProxyByName(proxyName);
        await proxy.AddAsync(new ResetPeerToxic { Name = toxicName, Stream = direction, Toxicity = 1 });
    }


    public async Task RemoveToxicAsync(string proxyName, string toxicName)
    {
        Proxy proxy = await FindProxyByName(proxyName);
        await proxy.RemoveToxicAsync(toxicName);
    }
}