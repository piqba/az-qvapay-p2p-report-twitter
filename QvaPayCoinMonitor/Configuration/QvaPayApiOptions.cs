using System;

namespace QvaPayCoinMonitor.Configuration;

public class QvaPayApiOptions
{
    public Uri BaseAddress { get; set; }
    public P2P P2P { get; set; }
}

public class P2P
{
    public string ResourceAddress { get; set; }
    public string Coins { get; set; }
}