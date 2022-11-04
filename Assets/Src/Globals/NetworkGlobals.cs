using UnityEngine;
using UnityEditor;

internal class NetworkGlobals : ScriptableObject
{
    internal const int kMasterServerPort = 8000;

    internal const int kBytesPerPose = 4 * 7;
    internal const int kBufSize = 508; // https://stackoverflow.com/questions/1098897/what-is-the-largest-safe-udp-packet-size-on-the-internet

    internal const int kClientTimeoutTime_ms = 30000;
    internal const int kClientTCPKeepAlivePeriod_ms = 5000;
    internal const int kClientUDPKeepAlivePeriod_ms = 1000;
    internal const int kLoginScreenTimeout_ms = 180000;

    //internal const int entitiesPerBuf = bufSize / bytesPerPose;   // 18

    internal const string kLoadLevelProtocol = "loadlevel";
    internal const string kOk = "OK";

    internal const int kUdpConnectionTimeout_ms = 5000;
    internal const int kUdpTimestampMax = 1000;
}
