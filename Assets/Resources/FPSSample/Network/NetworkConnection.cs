using System.Net;

public struct NetworkConnection
{
    public IPEndPoint portIp;
    public int id;

    public NetworkConnection(IPEndPoint ip)
    {
        portIp = ip;
        id = 0;
    }
}