using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace bgT5Launcher
{
    internal static class NetUtil
    {
        public static List<string> GetLocalIPv4s()
        {
            var list = new List<string>();
            try
            {
                foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        string s = ip.ToString();
                        if (!list.Contains(s)) list.Add(s);
                    }
                }
            }
            catch
            {
            }

            if (list.Count == 0) list.Add("127.0.0.1");
            return list;
        }
    }
}