using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Shared.Infrastructure.ToxiProxyTestContainer;
using Testcontainers.Toxiproxy;
using Toxiproxy.Net;
using Toxiproxy.Net.Toxics;


namespace ToxiProxyWrapper;

public sealed class ToxiProxyContainer : IAsyncDisposable
{
    private readonly IContainer _container;

    private Connection _connection = null!;
    private Client _client = null!;

    private ushort _nextListenPort = ToxiproxyBuilder.FirstProxiedPort;
    private const string _defaultImage = "ghcr.io/shopify/toxiproxy:2.5.0";

    public ToxiProxyContainer(string? image=null, INetwork? network = null, string? networkAlias=null)
    {
        ToxiproxyBuilder? builder = new ToxiproxyBuilder().WithImage(image??_defaultImage);
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
            name: name,
            dockerPort: listenPort,
            mappedPort:_container.GetMappedPublicPort(listenPort),
            mappedHost: _container.Hostname,
            disable: () => setEnabledAsync(name, enabled: false),
            enable: () => setEnabledAsync(name, enabled: true),
            addLatency: (toxicName, latencyMs, jitterMs, direction) =>
                addLatencyAsync(name, toxicName, latencyMs, jitterMs, direction),
            addTimeout: (toxicName, timeoutMs, direction) => addTimeoutAsync(name, toxicName, timeoutMs, direction),
            removeToxic: (toxicName) => removeToxicAsync(name, toxicName)
        );
    }

    public async Task RestoreAllAsync()
    {
        using HttpClient http = new ();
        http.BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort()}/");

        using HttpResponseMessage httpResponseMessage = await http.PostAsync("reset", content: null);
        httpResponseMessage.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await RestoreAllAsync();
            _connection.Dispose();
        }
        catch
        {
            /* ignore */
        }

        await _container.DisposeAsync();
    }

    private async Task setEnabledAsync(string proxyName, bool enabled)
    {
        Proxy proxy = await _client.FindProxyAsync(proxyName);
        proxy.Enabled = enabled;
        await _client.UpdateAsync(proxy);
    }

    private async Task addLatencyAsync(string proxyName, string toxicName, int latencyMs, int jitterMs,
        ToxicDirection direction)
    {
        Proxy proxy = await _client.FindProxyAsync(proxyName);

        await proxy.AddAsync(new LatencyToxic
        {
            Name = toxicName,
            Stream = direction,
            Toxicity = 1,
            Attributes = { Latency = latencyMs, Jitter = jitterMs }
        });
    }

    private async Task addTimeoutAsync(string proxyName, string toxicName, int timeoutMs, ToxicDirection direction)
    {
        Proxy proxy = await _client.FindProxyAsync(proxyName);
        await proxy.AddAsync(new TimeoutToxic
        {
            Name = toxicName, Stream = direction, Toxicity = 1, Attributes = { Timeout = timeoutMs }
        });
    }

    private async Task removeToxicAsync(string proxyName, string toxicName)
    {
        Proxy proxy = await _client.FindProxyAsync(proxyName);
        await proxy.RemoveToxicAsync(toxicName);
    }
}