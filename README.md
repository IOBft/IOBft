# IOBft
IOBft白皮书
1	综述
IOBft是一个使用简单可靠的技术手段实现高TPS、人人都可挖矿,去中心化、支持Pow+DPos双模式挖矿、使用BFT(拜占庭容错)共识算法、基于Block Dag的开源社区项目。
2	BFT共识
IOBft使用BFT共识算法解决在一些节点出现故障或恶意篡改数据的情况下，保证整个系统的正确性和一致性，防止双花攻击，分叉攻击。
Block权重公式: T2_weight = MIN（2/3*T1max，T1num） + 0.5f*(diff) + T1max*MIN（2/3*T3max，T3num）
T1max为当前Rule总数, T1num链接块总数, T3num为此Block被引用次数,权重最大的块胜出为主块
3	Block Dag
IOBft使用Block Dag（区块DAG结构）实现链上数据融合,优点是算法简单可靠性高，66%节点正常即可保证全网高速正常出块,它可以使每个节点所包含的交易数据都可以正确的入链,而不是像BTC，ETH那样被标记为废块,这样带来的好处是只要曾加出块节点数量就可提高TPS。
Block Dag
 
4	智能合约
IOBft支持智能合约,IOBft使用lua作为智能合约的脚本语言,lua具有图灵完备，简
单易用方便调试执行速度快等诸多优点。
支持合约模块,用户可从模板库中直接创建自己的智能合约，无需编码。
5	竞价rule机制
持有100W IOBFT币就可以竞争成为rule节点，每次出块rule都可以
获得收益。 目前最大支持24个出块节点，价高者得。
6	IO挖矿
IOBft使用IO挖矿的方式来防止芯片机、显卡矿机挖矿，这样做的目的是尽可能让数量庞大的普通PC参与挖矿，人人都可挖矿并且有利于去中心化。
7	IOBft的优点
DAG链TPS1000以上,Rule节点越多TPS越高,智能合约模块一键创建合约，智能合约发布和调用费用低,没有交易手续费,并且已确认(2F+1)交易不可回滚,安全可靠。项目代码简洁，可读性好，扩展性强。

8	IOBft币总量
IOBft币总量大约为8,399,089,755， 15秒产生出块奖励，初始奖励为：
主块奖励1024，Rule奖励128，每2年产量减半。
9	egametang.ET
使用egametang.ET游戏引擎作为基础架构，提高开发效率，便于接入游戏业务。
10	展望：
    1.构建IOBft技术社区
    2.构建DApp生态圈
    3.侧链,物联网、私密社交
    4.接入游戏业务
    5.C++、Rust版本
11	技术栈：
    C#,Bft,Dag,xlua,levelDB,egametang.ET,Ed25519,NLog,Protobuf,Newtonsoft.Json

12	其他
网站：www.IOBft.com
钱包：www.IOBft.com/Wallet
GitHub:
Gitee:
Telegram:


