using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace ETModel
{

    public class Transfer
    {
        public string hash;
        public string type;
        public long   notice;
        public string addressIn;
        public string addressOut;
        public string amount;
        public string data;
        public byte[] sign;
        public string depend;
        public long   height; // debug

        public override string ToString()
        {
            return $"{type}#{notice}#{addressIn}#{addressOut}#{amount}#{data}#{depend}#{sign.ToHex()}";
        }

        public string ToHash()
        {
            string temp = $"{type}#{notice}#{addressIn}#{addressOut}#{amount}#{data}#{depend}";
            return CryptoHelper.Sha256(temp);
        }

        public byte[] ToSign(WalletKey key)
        {
            return Wallet.Sign(hash.HexToBytes(), key);
        }

        public bool CheckSign()
        {
            return Wallet.Verify(sign, hash.HexToBytes(), addressIn);
        }

    }

    public class Account
    {
        public string address;
        public string amount;
        public long notice;
        public long index;  // transferIndex
    }

}