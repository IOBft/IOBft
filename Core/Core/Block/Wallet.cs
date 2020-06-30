using System;
using System.Collections.Generic;
using System.IO;

namespace ETModel
{

    public class WalletKey
    {
        public byte[] privatekey = new byte[64];
        public byte[] publickey  = new byte[32];
        public byte[] random;

        public string ToAddress()
        {
            return Wallet.ToAddress(publickey);
        }
    }

    public class Wallet
    {
        static public Wallet Inst = null;
        string passwords;
        public List<WalletKey> keys = new List<WalletKey>();
        public int curIndex = 0;
        public string walletFile;

        public WalletKey GetCurWallet()
        {
            return keys[curIndex];
        }

        public void SetCurWallet(string address)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].ToAddress() == address)
                {
                    curIndex = i;
                }
            }
        }

        public static bool CheckAddress(string address)
        {
            return address.Base58CheckDecode2();
        }

        public static string ToAddress(byte[] publickey)
        {
            //byte[] hash = publickey.Sha256().RIPEMD160();
            byte[] hash = CryptoHelper.Sha256(publickey.ToHexString()).ToByteArray().RIPEMD160();
            byte[] data = new byte[21];
            data[0] = 1;
            Buffer.BlockCopy(hash, 0, data, 1, 20);
            return data.Base58CheckEncode();
        }

        static public byte[] Seek()
        {
            byte[] seed = new byte[32];
            ed25519.ed25519_create_seed(seed);
            return seed;
        }

        public WalletKey Create()
        {
            WalletKey walletKey = new WalletKey();
            walletKey.random = Seek();
            string seed = CryptoHelper.Sha256(walletKey.random.ToHexString() + "#" + passwords);
            ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, seed.HexToBytes());
            this.keys.Add(walletKey);
            return walletKey;
        }

        static public Wallet GetWallet(string walletFile= "./Data/wallet.dat")
        {
            if (Inst != null)
                return Inst;
            Wallet wallet = new Wallet();
            wallet.walletFile = walletFile;
            //string input = "123";
            string input = wallet.Input(false);
            int ret = wallet.OpenWallet(input);
            if (ret == -1)
            {
                string input2 = wallet.Input(true);
                if (input == input2)
                {
                    wallet = wallet.NewWallet(input);
                    wallet.SaveWallet();
                }
            }
            else
            if (ret == -2)
            {
                Log.Info($"passwords error!");
                return null;
            }
            Inst = wallet;
            return wallet;
        }

        int OpenWallet(string password)
        {
            passwords = password;
            try
            {
                string[] lines = File.ReadAllLines(walletFile, System.Text.Encoding.Default);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] array = lines[i].Split(',');
                    if (array.Length == 2)
                    {
                        string seed = CryptoHelper.Sha256(array[0] + "#" + password);
                        WalletKey walletKey = new WalletKey();
                        ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, seed.HexToBytes());

                        if (walletKey.ToAddress() != array[1])
                        {
                            return -2;
                        }

                        walletKey.random = array[0].HexToBytes();
                        keys.Add(walletKey);
                    }
                    else
                    if (array.Length == 1)
                    {
                        curIndex = int.Parse(array[0]);
                    }
                }
            }
            catch (Exception )
            {
                return -1;
            }
            return 1;
        }

        public void SaveWallet()
        {
            string lines = "";
            for (int i = 0; i < keys.Count; i++)
            {
                lines += $"{keys[i].random.ToHexString()},{keys[i].ToAddress()}";
                lines += "\n";
            }

            lines += $"{curIndex}";

            File.WriteAllText(walletFile, lines);
        }

        public Wallet NewWallet(string passwords)
        {
            Wallet wallet = this;
            wallet.passwords = passwords;
            wallet.Create();
            return wallet;
        }

        static public byte[] Sign(byte[] data, WalletKey key)
        {
            byte[] signature = new byte[64];
            byte[] sign      = new byte[64+32];
            ed25519.ed25519_sign(signature, data, data.Length, key.publickey, key.privatekey);
            Buffer.BlockCopy(signature, 0, sign, 0, signature.Length);
            Buffer.BlockCopy(key.publickey, 0, sign, signature.Length, key.publickey.Length);
            return sign;
        }

        static public bool Verify(byte[] sign, string data, string address)
        {
            return Verify(sign, data.HexToBytes(), address);
        }

        static byte[] signature = new byte[64];
        static byte[] publickey = new byte[32];
        static public bool Verify(byte[] sign, byte[] data, string address)
        {
            if (!CheckAddress(address))
                return false;

            lock (signature)
            {
                Buffer.BlockCopy(sign, 0, signature, 0, signature.Length);
                Buffer.BlockCopy(sign, signature.Length, publickey, 0, publickey.Length);

                if (ed25519.ed25519_verify(signature, data, data.Length, publickey) != 1)
                    return false;

                if (ToAddress(publickey) != address)
                    return false;

                return true;
            }
        }

        // 联合签名验签
        static public bool VerifyCo(byte[] sign, byte[] data, string address)
        {
            if (!CheckAddress(address))
                return false;

            List<byte[]> signatures = new List<byte[]>();
            List<byte[]> publickeys = new List<byte[]>();
            int signLen = (64 + 32);
            byte[] publickeyCo = new byte[(int)(sign.Length / signLen) * 32 + (sign.Length-signLen * (int)(sign.Length / signLen))];
            for (int ii = 0; ii < sign.Length/ signLen; ii++)
            {
                byte[] signature = new byte[64];
                byte[] publickey = new byte[32];
                Buffer.BlockCopy(sign, ii*signLen, signature, 0, signature.Length);
                Buffer.BlockCopy(sign, ii * signLen + signature.Length, publickey, 0, publickey.Length);
                Buffer.BlockCopy(sign, ii * signLen + signature.Length, publickeyCo, ii* publickey.Length, publickey.Length);

                if (ed25519.ed25519_verify(signature, data, data.Length, publickey) != 1)
                    return false;
            }

            Buffer.BlockCopy(sign, signLen * (int)(sign.Length / signLen), publickeyCo, (int)(sign.Length / signLen) * 32, (sign.Length - signLen * (int)(sign.Length / signLen)));

            if(ToAddress(publickeyCo)!= address)
                return false;

            return true;
        }

        public string Input(bool again)
        {
            //定义一个字符串接收用户输入的内容
            string input = "";

            if(again)
                Console.Write("Please enter your passwords again: ");
            else
                Console.Write("Please enter passwords: ");

            while (true)
            {
                //存储用户输入的按键，并且在输入的位置不显示字符
                ConsoleKeyInfo ck = Console.ReadKey(true);

                //判断用户是否按下的Enter键
                if (ck.Key != ConsoleKey.Enter)
                {
                    if (ck.Key != ConsoleKey.Backspace)
                    {
                        //将用户输入的字符存入字符串中
                        input += ck.KeyChar.ToString();
                        //将用户输入的字符替换为*
                        Console.Write("*");
                    }
                    else
                    {
                        //删除错误的字符
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    Console.WriteLine();

                    break;
                }
            }
            return input;
        }

        static public void Test()
        {
            string info = "";

            for (int i = 0; i < 100; i++)
            {
                WalletKey walletKey = new WalletKey();
                walletKey.random = Seek();
                string seed = CryptoHelper.Sha256(walletKey.random.ToHexString() + "#" + "123");
                ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, seed.HexToBytes());

                string address = Wallet.ToAddress(walletKey.publickey);
                info += address + "\n";

            }
            Log.Info("Address    \n" + info);
        }

        static public void Test2()
        {
            WalletKey walletKey = new WalletKey();

            byte[] byteArray = "aa306f7fad8f12dad3e7b90ee15af0b39e9eccd1aad2e757de2d5ad74b42b67a".HexToBytes();
            byte[] seed32 = new byte[32];
            Buffer.BlockCopy(byteArray, 0, seed32, 0, seed32.Length);
            ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, seed32);


            string address = Wallet.ToAddress(walletKey.publickey);
            Log.Info("publickey  \n" + walletKey.publickey.ToHexString());
            Log.Info("privatekey \n" + walletKey.privatekey.ToHexString());
            Log.Info("Address    \n" + address);


            byte[] data = "e33b68cd7ad3dc29e623e399a46956d54c1861c5cd1e5039b875811d2ca4447d".HexToBytes();
            byte[] sign    = Wallet.Sign(data, walletKey);

            Log.Info("sign \n" + sign.ToHexString());

            if (Wallet.Verify(sign, data, address))
            {
                Log.Info("Verify ok ");
            }
        }

    }

}