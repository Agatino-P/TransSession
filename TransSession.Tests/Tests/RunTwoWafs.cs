using System.Net.Http.Json;
using First.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
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
        SecondCommandDto secondCommandDto=new(guid.ToString(),1);
        
        var response = await _fixture.FirstWafClient.PostAsJsonAsync("Test/Command",secondCommandDto, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        System.Diagnostics.Debugger.Break();

    }
}