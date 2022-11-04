
using System.Net.Sockets;
using System;
using System.Text;

public class NetworkStreamUtils
{
    public static int readInt(NetworkStream ns)
    {
        byte[] intToBytes = new byte[4];
        ns.Read(intToBytes, 0, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intToBytes);
        return BitConverter.ToInt32(intToBytes, 0);
    }

    public static void sendIntWithoutFlush(NetworkStream ns, int intValue)
    {
        byte[] intToBytes = BitConverter.GetBytes(intValue);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intToBytes);
        ns.Write(intToBytes, 0, 4);
    }

    public static void sendInt(NetworkStream ns, int intValue)
    {
        sendIntWithoutFlush(ns, intValue);
        ns.Flush();
    }

    public static void sendFloat(NetworkStream ns, int intValue)
    {
        byte[] floatToBytes = BitConverter.GetBytes(intValue);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(floatToBytes);
        ns.Write(floatToBytes, 0, 4);
        ns.Flush();
    }

    public static float readFloat(NetworkStream ns)
    {
        byte[] floatToBytes = new byte[4];
        ns.Read(floatToBytes, 0, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(floatToBytes);
        return BitConverter.ToSingle(floatToBytes, 0);
    }


    public static string readString(NetworkStream ns)
    {
        int bytesToRead = readInt(ns);

        byte[] stringToBytes = new byte[bytesToRead];
        ns.Read(stringToBytes, 0, bytesToRead);
        return Encoding.ASCII.GetString(stringToBytes);
    }

    public static void sendString(NetworkStream ns, String text)
    {
        byte[] stringToBytes = Encoding.ASCII.GetBytes(text);
        sendIntWithoutFlush(ns, stringToBytes.Length);

        ns.Write(stringToBytes, 0, stringToBytes.Length);
        ns.Flush();
    }

    public static void sendBytes(NetworkStream ns, byte[] bytes)
    {
        sendIntWithoutFlush(ns, bytes.Length);
        ns.Write(bytes, 0, bytes.Length);
        ns.Flush();
    }

    public static byte[] readBytes(NetworkStream ns)
    {
        int bytesToRead = readInt(ns);

        byte[] bytes = new byte[bytesToRead];
        ns.Read(bytes, 0, bytesToRead);
        return bytes;
    }
}