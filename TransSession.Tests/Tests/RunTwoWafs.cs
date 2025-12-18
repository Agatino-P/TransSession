using System.Net.Http.Json;
using First.Contracts.Dtos;
using TransSession.Tests.WAFs;

namespace TransSession.Tests.Tests;

public class RunTwoWafs : IClassFixture<DualApiFixture>
{
    private readonly DualApiFixture _fixture;
    private readonly ITestOutputHelper _outputHelper;
    private readonly HttpClient _firstClient;
    private readonly HttpClient _secondClient;
    
    public RunTwoWafs(DualApiFixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        _outputHelper = outputHelper;
        
     
        _firstClient = _fixture.FirstWaf.CreateClientWithXunitLogging(outputHelper);
        
   
        _secondClient = _fixture.SecondWaf.CreateClientWithXunitLogging(outputHelper);

    }
    
    [Fact]
    public async Task ShouldGenerateAnEvent()
    {
        Guid guid = Guid.NewGuid();
        _outputHelper.WriteLine($"Guid: {guid}");
        SecondCommandDto secondCommandDto=new(guid.ToString(),1);
        
        var response = await _firstClient.PostAsJsonAsync("Test/Command",secondCommandDto, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }
}