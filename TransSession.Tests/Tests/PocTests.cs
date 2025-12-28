using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using Shared.Contracts.Dtos;
using Shared.Database.Entities;
using Shared.GateManager;
using Shouldly;
using Toxiproxy.Net.Toxics;
using TransSession.Tests.WAFs;

namespace TransSession.Tests.Tests;

public class PocTests : IClassFixture<DualApiFixture>, IAsyncLifetime
{
    private readonly DualApiFixture _fixture;
    private readonly ITestOutputHelper _outputHelper;

    public PocTests(DualApiFixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task Should_SendAndReceive_Command()
    {
        Guid guid = Guid.NewGuid();
        _outputHelper.WriteLine($"Guid: {guid}");
        FirstApiSendCommandDto firstApiSendCommandDto = new(guid.ToString(), 1);

        HttpResponseMessage response = await _fixture.FirstWafClient.PostAsJsonAsync("Test/Command",
            firstApiSendCommandDto,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Should_PauseAndWaitForGateManager()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - Started");
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - Sending rest request");

        Guid guid = Guid.NewGuid();
        _outputHelper.WriteLine($"Guid: {guid}");
        FirstApiPauseDto firstApiPauseDto = new(guid.ToString());
        Task<HttpResponseMessage> restRequestTask =
            _fixture.FirstWafClient.PostAsJsonAsync("Test/Pause", firstApiPauseDto, cancellationToken);

        await _fixture.MultiGateManager.WaitUntilReached(IGateManager.BeforeDoingWorkGate, cancellationToken);
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - Gate reached");

        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - Releasing gate");
        _fixture.MultiGateManager.ReleaseGate(IGateManager.BeforeDoingWorkGate);

        HttpResponseMessage restResponse = await restRequestTask;
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - Rest request completes");

        restResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Should_PauseAndWait_ForBothWaf()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - Started");

        Task<HttpResponseMessage> firstApiRestRequestTask =
            _fixture.FirstWafClient.GetAsync("Test/SayWhenDone", cancellationToken);
        Task<HttpResponseMessage> secondApiRestRequestTask =
            _fixture.SecondWafClient.GetAsync("Test/SayWhenDone", cancellationToken);

        await _fixture.MultiGateManager.WaitUntilReached(IGateManager.SecondApiSayWhenDoneGate, cancellationToken);
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - SecondApi Gate reached");
        _fixture.MultiGateManager.ReleaseGate(IGateManager.SecondApiSayWhenDoneGate);

        await _fixture.MultiGateManager.WaitUntilReached(IGateManager.FistApiSayWhenDoneGate, cancellationToken);
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - FirstApi Gate reached");
        _fixture.MultiGateManager.ReleaseGate(IGateManager.FistApiSayWhenDoneGate);

        HttpResponseMessage firstRestResponse = await firstApiRestRequestTask;
        firstRestResponse.EnsureSuccessStatusCode();

        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - First Rest request completes");

        HttpResponseMessage secondRestResponse = await secondApiRestRequestTask;
        secondRestResponse.EnsureSuccessStatusCode();

        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - Second Rest request completes");
    }

    [Fact]
    public async Task Should_Call_ExternalWebSite()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - Started");

        string guidTxt = Guid.NewGuid().ToString();
        _outputHelper.WriteLine($"Guid: {guidTxt}");
        FirstApiCanFailDto dto = new(guidTxt);
        Task<HttpResponseMessage> firstApiRestRequestTask =
            _fixture.FirstWafClient.PostAsJsonAsync("Test/CanFail", dto, cancellationToken);

        await _fixture.MultiGateManager.WaitUntilReached(IGateManager.BeforeDoingWorkGate, cancellationToken);
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Test - FirstApi Gate reached");
        _fixture.MultiGateManager.ReleaseGate(IGateManager.BeforeDoingWorkGate);

        HttpResponseMessage restResponse = await firstApiRestRequestTask;
        restResponse.EnsureSuccessStatusCode();

        _fixture.PocDbContext.LogEntries.AsNoTracking().FirstOrDefault(entry => entry.EntryType ==LogEntryType.RestCallCompleted)
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_LeaveDbClean_OnExternalWebSiteFailure()
    {
        var ct = TestContext.Current.CancellationToken;
        var toxicName = Guid.NewGuid().ToString();

        var dto = new FirstApiCanFailDto(toxicName);
        var task = _fixture.FirstWafClient.PostAsJsonAsync("Test/CanFail", dto, ct);

        await _fixture.MultiGateManager.WaitUntilReached(IGateManager.BeforeDoingWorkGate, ct);

        await _fixture.NginxProxy.AddResetPeerAsync(toxicName+"-up", ToxicDirection.UpStream);
        await _fixture.NginxProxy.AddResetPeerAsync(toxicName + "-down", ToxicDirection.DownStream);

        try
        {
            _fixture.MultiGateManager.ReleaseGate(IGateManager.BeforeDoingWorkGate);

            var resp = await task;
            resp.IsSuccessStatusCode.ShouldBeFalse();

            _fixture.PocDbContext.LogEntries.AsNoTracking()
                .FirstOrDefault(e => e.EntryType == LogEntryType.RestCallReceived)
                .ShouldBeNull();

            _fixture.PocDbContext.LogEntries.AsNoTracking()
                .FirstOrDefault(e => e.EntryType == LogEntryType.RestCallCompleted)
                .ShouldBeNull();
        }
        finally
        {
            // Always cleanup, even if assertions fail
            try { await _fixture.NginxProxy.RemoveToxicAsync(toxicName + "-up"); } catch { }
            try { await _fixture.NginxProxy.RemoveToxicAsync(toxicName + "-down"); } catch { }
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.PocDbContext.LogEntries.ExecuteDeleteAsync();
        _fixture.PocDbContext.ChangeTracker.Clear();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _fixture.PocDbContext.LogEntries.ExecuteDeleteAsync();
        _fixture.PocDbContext.ChangeTracker.Clear();
    }
}