namespace EventHub.Application.Common.Interfaces;

public interface IInvalidatesCache
{
    IReadOnlyCollection<string> CacheTags { get; }
}
