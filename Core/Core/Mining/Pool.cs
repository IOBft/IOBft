using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;

namespace ETModel
{
    public class Pool : Component
    {
        public class Miner
        {
            string address;
            Dictionary<string, MinerNumber> numbers;
        }

        public class MinerNumber
        {
            string number;
            string power;
            long   timestart;
            string state;

        }

        HttpPool httpPool = null;

        public override void Awake(JToken jd = null)
        {
            httpPool = Entity.Root.GetComponentInChild<HttpPool>();

        }

        public override void Start()
        {
            Run();
        }

        public CalculatePower calculatePower = new CalculatePower();

        public string number;
        public string address;
        public string url;
        public long height = 1;
        public string hashmining = "";
        public string random;
        public string taskid = "";
        double diff_max = 0;
        public async void Run()
        {
            await Task.Delay(1000);

            HttpMessage quest = new HttpMessage();
            quest.map = new Dictionary<string, string>();

            while (true)
            {
                quest.map.Clear();
                quest.map.Add("cmd", "submit");
                quest.map.Add("height", height.ToString());
                quest.map.Add("address", address);
                quest.map.Add("number", number);
                quest.map.Add("random", random);
                quest.map.Add("taskid", taskid);
                HttpMessage result = await ComponentNetworkHttp.Query(url, quest);

                string rel = result.map["rel"];
                if (rel != "ok")
                {
                    long.TryParse(result.map["height"], out long tempheight);
                    taskid = result.map["taskID"];
                    string temphash = result.map["hashmining"];
                    if (temphash == null || temphash == "" || temphash != hashmining)
                    {
                        if (diff_max != 0)
                            calculatePower.Insert(diff_max);

                        diff_max = 0;
                        hashmining = temphash;
                        height = tempheight;
                        random = "";
                    }
                }
                await Task.Delay(1000);
            }
        }

        // 矿工奖励,确认不可回滚后才发奖励
        Dictionary<string, Transfer> minerTransfer = new Dictionary<string, Transfer>();
        public void MinerReward()
        {
            if (httpPool != null)
            {
                Dictionary<string, MinerTask> miners = httpPool.GetMinerReward(out long miningHeight);
                if (miners != null && miningHeight + 3 < height)
                {
                    string ownerAddress = Wallet.GetWallet().GetCurWallet().ToAddress();

                    var mcblk = BlockChainHelper.GetMcBlock(miningHeight);
                    if (mcblk != null && mcblk.Address == ownerAddress)
                    {
                        var miner = miners.Values.First(c => c.random == mcblk.random);
                        WalletKey walletKey = Wallet.GetWallet().GetCurWallet();

                        // 出块奖励
                        if (miner != null)
                        {
                            Transfer transfer = new Transfer();
                            transfer.addressIn = ownerAddress;
                            transfer.addressOut = miner.address;
                            transfer.amount = Consensus.GetReward(miningHeight).ToString();
                            transfer.notice = TimeHelper.NowSeconds();
                            transfer.type = "tranfer";
                            transfer.hash = transfer.ToHash();
                            transfer.sign = transfer.ToSign(walletKey);
                            minerTransfer.Add(mcblk.hash, transfer);
                        }

                        // 参与奖励


                    }
                    httpPool.DelMiner(miningHeight);
                }
            }
        }

    }


}






















