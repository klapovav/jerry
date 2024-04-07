using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.IO;

namespace Jerry.Connection;

public struct Agreement
{
    public byte[] Key;
    public byte[] IV;
}

internal class KeyExchange
{
    //32B, 256bit
    private const int AGREEMENT_SIZE = 32;
    private const int STRENGTH = AGREEMENT_SIZE * 8;

    private readonly X25519KeyPairGenerator generator;

    public KeyExchange()
    {
        generator = new X25519KeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), STRENGTH));
    }

    private static byte[] ExchangeBytes(Stream stream, byte[] ourPublic)
    {
        if (ourPublic.Length != AGREEMENT_SIZE)
            throw new ArgumentException($"Public key has {AGREEMENT_SIZE} Bytes");
        var counterpartPublic = new byte[AGREEMENT_SIZE];
        stream.ReadTimeout = 50;
        stream.Write(ourPublic);
        stream.Flush();
        stream.Read(counterpartPublic, 0, AGREEMENT_SIZE);
        stream.ReadTimeout = Int32.MaxValue;
        return counterpartPublic;
    }

    private byte[] SecretAgreement(Stream stream)
    {
        var ourKey = generator.GenerateKeyPair();
        var ourPublic = ((X25519PublicKeyParameters)ourKey.Public);
        var counterpartPublic = ExchangeBytes(stream, ourPublic.GetEncoded());
        var ag = new X25519Agreement();
        ag.Init(ourKey.Private);
        var sharedSecret = new byte[ag.AgreementSize];
        ag.CalculateAgreement(new X25519PublicKeyParameters(counterpartPublic), sharedSecret, 0);
        return sharedSecret;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    /// <exception cref="KeyExchangeException"></exception>
    public Agreement GeAgreement(Stream stream)
    {
        try
        {
            var sharedSecret = new Agreement
            {
                Key = SecretAgreement(stream),
                IV = SecretAgreement(stream)
            };
            return sharedSecret;
        }
        catch (Exception e)
        {
            throw new KeyExchangeException("Key exchange failed", e);
        }
    }

    public static void GetMasterDebugAgreement()
    {
        //var ourKey = new byte[] { 57, 206, 22, 169, 151, 231, 235, 74, 114, 176, 26, 172, 245, 43, 121, 77, 173, 250, 244, 239, 225, 175, 11, 11, 55, 21, 147, 168, 245, 15, 170, 53 };
        //var ourNonce = new byte[] { 178, 9, 41, 7, 32, 205, 169, 198, 8, 141, 3, 165, 244, 177, 119, 239, 149, 107, 81, 87, 204, 140, 159, 200, 116, 252, 204, 20, 251, 239, 205, 27 };
        //var counterpartKey = new byte[] { 43, 121, 77, 169, 151, 231, 235, 74, 114, 176, 26, 172, 245, 43, 121, 77, 173, 250, 244, 239, 225, 175, 11, 11, 55, 21, 147, 168, 245, 15, 170, 53 };
        //var counterpartNonce = new byte[] { 179, 10, 42, 7, 32, 205, 169, 198, 8, 141, 3, 165, 244, 177, 119, 239, 149, 107, 81, 87, 204, 140, 159, 200, 116, 252, 204, 20, 251, 239, 205, 27 };
    }
}