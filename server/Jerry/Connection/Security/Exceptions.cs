using System;
using System.Runtime.Serialization;

namespace Jerry.Connection;

public class KeyExchangeException : Exception
{
    public KeyExchangeException()
    {
    }

    public KeyExchangeException(string message) : base(message)
    {
    }

    public KeyExchangeException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected KeyExchangeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}