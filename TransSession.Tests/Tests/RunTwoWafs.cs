using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Shared.Infrastructure.Contracts.Dtos;
using TransSession.Tests.WAFs;

namespace TransSession.Tests.Tests;

public class RunTwoWafs : IClassFixture<DualApiFixture>
{
    private readonly DualApiFixture _fixture;
    private readonly ITestOutputHelper _outputHelper;
    
    public RunTwoWafs(DualApiFixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        _outputHelper = outputHelper;
    }
    
    [Fact]
    public async Task Should_SendAndReceive_Command()
    {
        Guid guid = Guid.NewGuid();
        _outputHelper.WriteLine($"Guid: {guid}");
        FirstApiSendCommandDto firstApiSendCommandDto=new(guid.ToString(),1);
        
        var response = await _fixture.FirstWafClient.PostAsJsonAsync("Test/Command",firstApiSendCommandDto, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        System.Diagnostics.Debugger.Break();

    }
}