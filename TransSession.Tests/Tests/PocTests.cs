using System.Net.Http.Json;
using Shared.Infrastructure.Contracts.Dtos;
using Shared.Infrastructure.Database.Entities;
using Shared.Infrastructure.GateManager;
using Shouldly;
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

        _fixture.PocDbContext.LogEntries.FirstOrDefault(entry => entry.Description == "Controller - Completed")
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_LeaveDbClean_OnExternalWebSiteFailure()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        string guidTxt = Guid.NewGuid().ToString();
        _outputHelper.WriteLine($"Guid: {guidTxt}");

        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.TestStarted, guidTxt);

        FirstApiCanFailDto dto = new(guidTxt);
        Task<HttpResponseMessage> firstApiRestRequestTask =
            _fixture.FirstWafClient.PostAsJsonAsync("Test/CanFail", dto, cancellationToken);

        await _fixture.MultiGateManager.WaitUntilReached(IGateManager.BeforeDoingWorkGate, cancellationToken);
        await _fixture.PocLogEntryRepository.AddEntry(LogEntryType.TestWaitUntilReachedComplete,
            IGateManager.BeforeDoingWorkGate);

        await _fixture.NginxProxy.DisableAsync();
        
        _fixture.MultiGateManager.ReleaseGate(IGateManager.BeforeDoingWorkGate);

        HttpResponseMessage restResponse = await firstApiRestRequestTask;
        restResponse.IsSuccessStatusCode.ShouldBeFalse();

        _fixture.PocDbContext.LogEntries.FirstOrDefault(entry => entry.EntryType == LogEntryType.RestCallReceived)
            .ShouldBeNull();

        _fixture.PocDbContext.LogEntries.FirstOrDefault(entry => entry.EntryType == LogEntryType.RestCallCompleted)
            .ShouldBeNull();
    }

    public async ValueTask DisposeAsync() => await _fixture.RestoreAllProxiesAsync();
    public async ValueTask InitializeAsync() => await _fixture.RestoreAllProxiesAsync();
}