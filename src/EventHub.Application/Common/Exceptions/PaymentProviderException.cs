namespace EventHub.Application.Common.Exceptions;

public sealed class PaymentProviderException : Exception
{
    public PaymentProviderException(string message) : base(message)
    {
    }
}
