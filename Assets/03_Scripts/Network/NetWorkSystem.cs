using UnityEngine;

// ====== 网络角色 ======
public enum E_NetRole
{
    None,
    Local,      // 本地玩家（输入来源）
    Remote,     // 远程玩家（输入来自网络）
    Spectator   // 观战（无输入）
}

public enum E_NetMessage
{
    HandshakeRequest,   // 客户端请求加入
    HandshakeResponse,  // 主机分配 playerIndex
    InputFrame,         // 帧输入数据
    Disconnect          // 断开通知
}

public class NetWorkSystem
{
    
}
