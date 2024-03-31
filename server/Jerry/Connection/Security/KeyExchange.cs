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
    private static readonly int AGREEMENT_SIZE = 32;

    private static readonly int STRENGTH = AGREEMENT_SIZE * 8;

    private readonly X25519KeyPairGenerator generator;

    public KeyExchange()
    {
        generator = new X25519KeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), STRENGTH));
    }

    private static byte[] ExchangeBytes(Stream stream, byte[] pub_key)
    {
        if (pub_key.Length != AGREEMENT_SIZE)
            throw new ArgumentException($"Public key has {AGREEMENT_SIZE} Bytes");
        var alice_pub = new byte[AGREEMENT_SIZE];
        stream.ReadTimeout = 50;
        stream.Write(pub_key);
        stream.Flush();
        stream.Read(alice_pub, 0, AGREEMENT_SIZE);
        stream.ReadTimeout = Int32.MaxValue;
        return alice_pub;
    }

    private byte[] SecretAgreement(Stream stream)
    {
        var bob = generator.GenerateKeyPair();
        var bob_pub = ((X25519PublicKeyParameters)bob.Public);
        var their_pub = ExchangeBytes(stream, bob_pub.GetEncoded());

        var ag = new X25519Agreement();
        ag.Init(bob.Private);
        var sharedKey1 = new byte[ag.AgreementSize];
        ag.CalculateAgreement(new X25519PublicKeyParameters(their_pub), sharedKey1, 0);
        return sharedKey1;
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
            var shared_agreement = new Agreement
            {
                Key = SecretAgreement(stream),
                IV = SecretAgreement(stream)
            };
            return shared_agreement;
        }
        catch (Exception e)
        {
            throw new KeyExchangeException("Key exchange failed", e);
        }
    }

    public static void GetMasterDebugAgreement()
    {
        //var key_m = new byte[] { 57, 206, 22, 169, 151, 231, 235, 74, 114, 176, 26, 172, 245, 43, 121, 77, 173, 250, 244, 239, 225, 175, 11, 11, 55, 21, 147, 168, 245, 15, 170, 53 };
        //var nonce_m = new byte[] { 178, 9, 41, 7, 32, 205, 169, 198, 8, 141, 3, 165, 244, 177, 119, 239, 149, 107, 81, 87, 204, 140, 159, 200, 116, 252, 204, 20, 251, 239, 205, 27 };
        //var key_s = new byte[] { 43, 121, 77, 169, 151, 231, 235, 74, 114, 176, 26, 172, 245, 43, 121, 77, 173, 250, 244, 239, 225, 175, 11, 11, 55, 21, 147, 168, 245, 15, 170, 53 };
        //var nonce_s = new byte[] { 179, 10, 42, 7, 32, 205, 169, 198, 8, 141, 3, 165, 244, 177, 119, 239, 149, 107, 81, 87, 204, 140, 159, 200, 116, 252, 204, 20, 251, 239, 205, 27 };
    }
}