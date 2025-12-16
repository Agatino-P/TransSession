
namespace Second.Contracts.NServiceBus.Commands;

public record SecondCommand(string Text, int Number)
{
    public const string Endpoint = "Second.Api";
};