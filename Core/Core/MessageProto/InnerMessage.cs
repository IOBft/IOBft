using ETModel;
using System.Collections.Generic;
namespace ETModel
{
	public partial class IQueryProto : IQuery
	{
		public int RpcId { get; set; }

		public long ActorId { get; set; }

	}

	public partial class IResponseProto: IResponse
	{
		public int RpcId { get; set; }

		public int Result { get; set; }

		public int Error { get; set; }

		public string Message { get; set; }

		public long ActorId { get; set; }

	}

	[Message(NetOpcode.P2P_HearBeat,AppType.Core)]
	public partial class P2P_HearBeat: IMessage
	{
	}

	[Message(NetOpcode.P2P_Broadcast,AppType.Core)]
	public partial class P2P_Broadcast: IMessage
	{
		public List<long> ActorIds = new List<long>();

		public IMessage msg { get; set; }

	}

	[Message(NetOpcode.Q2P_New_Node,AppType.Core)]
	public partial class Q2P_New_Node: IQueryProto
	{
		public int HashCode { get; set; }

		public string address { get; set; }

		public string ipEndPoint { get; set; }

		public long sendTime { get; set; }

	}

	[Message(NetOpcode.R2P_New_Node,AppType.Core)]
	public partial class R2P_New_Node: IResponseProto
	{
		public string Nodes { get; set; }

		public long sendTime { get; set; }

		public long nodeTime { get; set; }

	}

	[Message(NetOpcode.Q2P_Block,AppType.Core)]
	public partial class Q2P_Block: IQueryProto
	{
		public string hash { get; set; }

	}

	[Message(NetOpcode.R2P_Block,AppType.Core)]
	public partial class R2P_Block: IResponseProto
	{
		public string block { get; set; }

	}

	[Message(NetOpcode.Q2P_McBlock,AppType.Core)]
	public partial class Q2P_McBlock: IQueryProto
	{
		public long height { get; set; }

	}

	[Message(NetOpcode.R2P_McBlock,AppType.Core)]
	public partial class R2P_McBlock: IResponseProto
	{
		public string block { get; set; }

	}

	[Message(NetOpcode.Q2P_Prehashmkl,AppType.Core)]
	public partial class Q2P_Prehashmkl: IQueryProto
	{
		public long height { get; set; }

	}

	[Message(NetOpcode.R2P_Prehashmkl,AppType.Core)]
	public partial class R2P_Prehashmkl: IResponseProto
	{
		public string prehashmkl { get; set; }

	}

	[Message(NetOpcode.P2P_NewBlock,AppType.Core)]
	public partial class P2P_NewBlock: IMessage
	{
		public string block { get; set; }

		public string ipEndPoint { get; set; }

	}

	[Message(NetOpcode.Q2P_Transfer,AppType.Core)]
	public partial class Q2P_Transfer: IQueryProto
	{
		public string transfer { get; set; }

	}

	[Message(NetOpcode.R2P_Transfer,AppType.Core)]
	public partial class R2P_Transfer: IResponseProto
	{
		public string rel { get; set; }

	}

	[Message(NetOpcode.Q2P_McBlockHash,AppType.Core)]
	public partial class Q2P_McBlockHash: IQueryProto
	{
		public long height { get; set; }

	}

	[Message(NetOpcode.R2P_McBlockHash,AppType.Core)]
	public partial class R2P_McBlockHash: IResponseProto
	{
		public string hash { get; set; }

	}

	[Message(NetOpcode.Q2P_BeLinkHash,AppType.Core)]
	public partial class Q2P_BeLinkHash: IQueryProto
	{
		public string hash { get; set; }

	}

	[Message(NetOpcode.R2P_BeLinkHash,AppType.Core)]
	public partial class R2P_BeLinkHash: IResponseProto
	{
		public string hashs { get; set; }

	}

}
