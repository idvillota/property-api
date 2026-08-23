namespace Property.Application.Common.Interfaces;

public interface IOutboxMessageHandler
{
    string MessageType { get; }

    Task HandleAsync(string payloadJson, CancellationToken cancellationToken = default);
}
