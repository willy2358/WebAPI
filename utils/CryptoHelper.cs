using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

class CryptoHelper
{
     private static RSACryptoServiceProvider _rsaProvider = null;

     private static RSACryptoServiceProvider GetRSAProvider()
     {
         if (null != _rsaProvider)
         {
             return _rsaProvider;
         }
        string priKeyFile = System.IO.Path.Combine(AppContext.BaseDirectory, "pri512.pem");
        System.Diagnostics.Debug.Assert(System.IO.File.Exists(priKeyFile), "private file not exists");

        using (TextReader privateKeyTextReader = new StringReader(File.ReadAllText(priKeyFile)))
        {
            RsaPrivateCrtKeyParameters privateKeyParams = (RsaPrivateCrtKeyParameters)new PemReader(privateKeyTextReader).ReadObject();
            // RsaPrivateCrtKeyParameters privateKeyParams = ((RsaPrivateCrtKeyParameters)readKeyPair.Private);
            RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider();
            RSAParameters parms = new RSAParameters();

            parms.Modulus = privateKeyParams.Modulus.ToByteArrayUnsigned();
            parms.P = privateKeyParams.P.ToByteArrayUnsigned();
            parms.Q = privateKeyParams.Q.ToByteArrayUnsigned();
            parms.DP = privateKeyParams.DP.ToByteArrayUnsigned();
            parms.DQ = privateKeyParams.DQ.ToByteArrayUnsigned();
            parms.InverseQ = privateKeyParams.QInv.ToByteArrayUnsigned();
            parms.D = privateKeyParams.Exponent.ToByteArrayUnsigned();
            parms.Exponent = privateKeyParams.PublicExponent.ToByteArrayUnsigned();

            cryptoServiceProvider.ImportParameters(parms);

            _rsaProvider = cryptoServiceProvider;
        }

        return _rsaProvider;
     }

     public static string SignDataToBase64Str(string inputData)
     {
         var rsa = GetRSAProvider();
         if (null != rsa)
         {
            var bytes = Encoding.UTF8.GetBytes(inputData);
            byte[] sig = rsa.SignData(bytes, "SHA1");
            return Convert.ToBase64String(sig);
         }
         else
         {
             return "Invalid signature";
         }
     }
}