using TransSession.Tests.WAFs;

namespace TransSession.Tests.Tests;

public class RunFirstWaf : IClassFixture<FirstWaf>
{
    private readonly FirstWaf _firstWaf;
    private readonly ITestOutputHelper _testOutputHelper;

    public RunFirstWaf(FirstWaf firstWaf, ITestOutputHelper  testOutputHelper)
    {
        _firstWaf = firstWaf;
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ShouldRunWaf()
    {
        var httpClient = _firstWaf.CreateClientWithXunitLogging(_testOutputHelper);
       var response = await httpClient.GetAsync("/test",TestContext.Current.CancellationToken);
        
    }
}