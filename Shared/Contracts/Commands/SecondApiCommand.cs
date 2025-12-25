
namespace Shared.Infrastructure.Contracts.Commands;

public record SecondApiCommand(string Text, int Number)
{
    public const string Endpoint = "SecondApi";
};