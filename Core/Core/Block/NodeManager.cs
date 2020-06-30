﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace ETModel
{
    public class NodeManager : Component
    {
        static public int K_INDEX_ROW = 12; // height
        static public int K_INDEX_COL = 12; // width

        public class NodeData
        {
            public long   nodeId;
            public string address;
            public string ipEndPoint;
            public int    kIndex; // K桶序号
            public long   time;
        }

        public static string GetIpV4()
        {
            try
            {
                using (System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 1337);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }

            }
            catch (Exception)
            {
            }

            return "";
        }

        // 节点队列
        List<NodeData> nodes = new List<NodeData>();
        ComponentNetworkInner networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
        public long nodeTimeOffset = 0;

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            //componentNetMsg.registerMsg(NetOpcode.A2M_HearBeat, A2M_HearBeat_Handle);
            componentNetMsg.registerMsg(NetOpcode.Q2P_New_Node, Q2P_New_Node_Handle);
            //componentNetMsg.registerMsg(NetOpcode.R2P_New_Node, R2P_New_Node_Handle);

        }

        public long GetMyNodeId()
        {
            return StringHelper.HashCode(networkInner.ipEndPoint.ToString());
        }

        public long GetNodeTime()
        {
            return TimeHelper.Now()+ nodeTimeOffset;
        }

        public async override void Start()
        {
            networkInner.ipEndPoint = NetworkHelper.ToIPEndPoint(GetIpV4() + ":" + networkInner.ipEndPoint.Port);
            Log.Info($"Node:{networkInner.ipEndPoint.ToString()}");

            string tmp = Program.jdNode["NodeSessions"].ToString();
            List<string> list = JsonHelper.FromJson<List<string>>(tmp);

            Q2P_New_Node new_Node = new Q2P_New_Node();
            new_Node.ActorId = GetMyNodeId();
            new_Node.address = Wallet.GetWallet().GetCurWallet().ToAddress();
            new_Node.ipEndPoint = networkInner.ipEndPoint.ToString();

            Log.Debug($"NodeManager.Start");
            while (true && list.Count>0)
            {
                try
                {
                    for (int ii = 0; ii < list.Count; ii++)
                    {
                        bool bResponse = false;
                        new_Node.HashCode = StringHelper.HashCode(JsonHelper.ToJson(nodes));
                        new_Node.sendTime = TimeHelper.Now();
                        Session session = await networkInner.Get(NetworkHelper.ToIPEndPoint(list[0]));
                        if (session != null && session.IsConnect())
                        {
                            //Log.Debug($"NodeSessions connect " + r2P_New_Node.ActorId);
                            //session.Send(new_Node);

                            R2P_New_Node r2P_New_Node = (R2P_New_Node)await session.Query(new_Node, 0.3f);
                            if (r2P_New_Node != null)
                            {
                                if (r2P_New_Node.Nodes != "")
                                {
                                    nodes = JsonHelper.FromJson<List<NodeData>>(r2P_New_Node.Nodes);
                                    long timeNow = TimeHelper.Now() ;
                                    nodeTimeOffset = (timeNow - new_Node.sendTime) / 2 + r2P_New_Node.nodeTime - timeNow;
                                }
                                bResponse = true;
                            }
                        }
                        if (bResponse)
                        {
                            break;
                        }
                    }

                    // 等待5秒后关闭连接
                    await Task.Delay(5 * 1000);
                }
                catch (Exception)
                {
                    await Task.Delay(5 * 1000);
                }
            }
        }

        //[MessageMethod(NetOpcode.O2G_New_Node)]
        void Q2P_New_Node_Handle(Session session, int opcode, object msg)
        {
            Q2P_New_Node new_Node = msg as Q2P_New_Node;
            //Log.Debug($"Q2P_New_Nod {new_Node.ActorId} \r\nHash: {new_Node.HashCode}");

            NodeData data = new NodeData();
            data.nodeId     = new_Node.ActorId;
            data.address    = new_Node.address;
            data.ipEndPoint = new_Node.ipEndPoint;
            data.kIndex = GetkIndex();
            AddNode(data);

            R2P_New_Node response = new R2P_New_Node() { Nodes = "" , sendTime = new_Node.sendTime , nodeTime = TimeHelper.Now() };
            if(StringHelper.HashCode(JsonHelper.ToJson(nodes))!= new_Node.HashCode)
            {
                response.Nodes = JsonHelper.ToJson(nodes);
                session.Send(response);
            }
            session.Reply(new_Node, response);
        }

        void R2P_New_Node_Handle(Session session, int opcode, object msg)
        {
            //R2P_New_Node new_Node = msg as R2P_New_Node;
            //nodes = JsonHelper.FromJson<List<NodeData>>(new_Node.Nodes);
            //var tempnodes = JsonHelper.FromJson<List<NodeData>>(new_Node.Nodes);
            //for(int i=0;i< tempnodes.Count;i++)
            //{
            //    NodeData node = tempnodes[i];
            //    if (nodes.Find((n) => { return n.nodeId == node.nodeId; }) == null)
            //    {
            //        nodes.Add(node);
            //        Log.Debug($"\r\nAddNode {node.nodeId} {node.address} {node.ipEndPoint} kIndex:{node.kIndex} nodes:{nodes.Count}");
            //    }
            //}
        }

        Dictionary<long, float> nodesLastTime = new Dictionary<long, float>();
        public bool AddNode(NodeData data)
        {
            nodesLastTime.Remove(data.nodeId);
            nodesLastTime.Add(data.nodeId, TimeHelper.time);
            NodeData node = nodes.Find((n) => { return n.nodeId == data.nodeId; });
            if (node != null)
            {
                return false;
            }

            data.time = DateTime.Now.Ticks;
            nodes.Add(data);

            Log.Debug($"\r\nAddNode {data.nodeId} {data.address} {data.ipEndPoint} kIndex:{data.kIndex} nodes:{nodes.Count}");
            return true;
        }

        TimePass timePass = new TimePass(1);
        public override void Update()
        {
            if (timePass.IsPassSet() && nodesLastTime.Count>0)
            {
                float lastTime = 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    NodeData node = nodes[i];
                    if (nodesLastTime.TryGetValue(node.nodeId, out lastTime))
                    {
                        if (TimeHelper.time - lastTime > 30f )
                        {
                            nodes.Remove(node);
                            nodesLastTime.Remove(node.nodeId);
                            i--;

                            Log.Debug($"nodes.Remove {node.nodeId} {nodes.Count}");
                        }
                    }
                }
            }
        }

        //async void UpdateNodes()
        //{
        //    Log.Debug($"NodeManager.UpdateNodes");

        //    Session session = null;
        //    while (true)
        //    {
        //        await Task.Delay(5 * 1000);
        //        for (int i = 0; i < nodes.Count; i++)
        //        {
        //            NodeData node = nodes[i];
        //            session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
        //            if (session != null && session.IsConnect())
        //            {

        //            }
        //            else
        //            {
        //                nodes.RemoveAt(i);
        //                i--;
        //            }
        //            await Task.Delay(1000);
        //            session?.Dispose();
        //        }
        //    }
        //}

        public int GetNodeCount()
        {
            return nodes.Count;
        }

        public List<NodeData> GetNodeList()
        {
            return nodes;
        }

        public NodeData GetRandomNode()
        {
            NodeData[] nodesTemp = nodes.Where( a => a.ipEndPoint != networkInner.ipEndPoint.ToString()).ToArray();
            if(nodesTemp.Length!=0)
                return nodesTemp[RandomHelper.Random() % nodesTemp.Length];
            return null;
        }

        public List<NodeData> GetBroadcastNode()
        {
            List<NodeData> result = new List<NodeData>();
            for (int i = 0; i < K_INDEX_ROW; i++)
            {
                List<NodeData> nodestmp = GetkList(i);
                if (nodestmp.Count > 0)
                {
                    result.Add(nodestmp[RandomHelper.Random() % nodestmp.Count]);
                }
            }
            return result;
        }

        public async void Broadcast(IMessage message)
        {
            // 向所在桶广播
            Broadcast2Kad(message);

            // 获取广播列表
            List<NodeData> result = GetBroadcastNode();

            // 剔除自己所在桶
            NodeData nodeSelf = nodes.Find((n) => { return n.nodeId == StringHelper.HashCode(networkInner.ipEndPoint.ToString()); });
            if (nodeSelf != null)
            {
                NodeData nodeIgnore = result.Find((n) => { return n.kIndex == nodeSelf.kIndex; });
                if (nodeIgnore != null)
                {
                    result.Remove(nodeIgnore);
                }
            }

            // 开始广播
            for (int i = 0; i < result.Count; i++)
            {
                NodeData node = result[i];
                Session session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
                if (session != null && session.IsConnect())
                {
                    session.Send(message);
                }
            }

        }

        // Broadcast to my Kademlia
        public async void Broadcast2Kad(IMessage message)
        {
            NodeData nodeSelf = nodes.Find((n) => { return n.nodeId == StringHelper.HashCode(networkInner.ipEndPoint.ToString());});
            if (nodeSelf == null)
                return;

            List<NodeData> result = GetkList(nodeSelf.kIndex);

            for (int i = 0; i < result.Count; i++)
            {
                NodeData node = result[i];
                if (node.nodeId != nodeSelf.nodeId) // ignore self
                {
                    Session session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
                    if (session != null && session.IsConnect())
                    {
                        session.Send(message);
                    }
                }
            }

        }

        // 如果收到的是桶外的数据 , 向K桶内进行一次广播
        public bool IsNeedBroadcast2Kad(IPEndPoint ipEndPoint)
        {
            NodeData nodetarget = nodes.Find((n) => { return n.nodeId == StringHelper.HashCode(ipEndPoint.ToString()); });
            NodeData nodeSelf   = nodes.Find((n) => { return n.nodeId == StringHelper.HashCode(ipEndPoint.ToString()); });
            if (nodetarget != null && nodeSelf != null)
            {
                if (nodetarget.kIndex != nodeSelf.kIndex)
                {
                    return true;
                }
            }
            return false;
        }

        List<NodeData> GetkList(int i)
        {
            return nodes.FindAll((node) => { return node.kIndex == i; });
        }

        int GetkIndex()
        {
            for (int i = 0; i < K_INDEX_ROW ;i++ )
            {
                List<NodeData> list = nodes.FindAll((node) => { return node.kIndex == i; });
                if (list.Count < K_INDEX_COL)
                    return i;
            }
            return RandomHelper.Random() % K_INDEX_ROW;
        }




    }


}
