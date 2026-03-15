using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public static class NetworkTool
{
    public static string GetLocalIPAddress()
    {
        try
        {
            // 获取所有“Up”状态、非 Loopback 的网络接口
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in interfaces)
            {
                var props = ni.GetIPProperties();

                // 只考虑有默认网关的接口（排除虚拟机/WSL/Docker等）
                if (props.GatewayAddresses.Count == 0)
                    continue;

                foreach (var unicast in props.UnicastAddresses)
                {
                    var ip = unicast.Address;
                    if (ip.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ip) &&
                        !ip.ToString().StartsWith("169.254") && // APIPA 地址（未获取到 DHCP）
                        IsPrivateIP(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Critical("Error while getting local IP address: " + ex.Message);
        }

        return "127.0.0.1";
    }

    static bool IsPrivateIP(IPAddress ip)
    {
        byte[] bytes = ip.GetAddressBytes();
        if (bytes[0] == 10) return true;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        return false;
    }
}
