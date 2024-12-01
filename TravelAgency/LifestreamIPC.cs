using ECommons.EzIpcManager;
using System;

namespace TravelAgency;

#nullable disable
public class LifestreamIPC
{
    public const string Name = "Lifestream";
    public LifestreamIPC() => EzIPC.Init(this, Name, SafeWrapper.AnyException);

    [EzIPC] public Func<string, bool> AethernetTeleport;
    [EzIPC] public Func<uint, byte, bool> Teleport;
    [EzIPC] public Func<bool> TeleportToHome;
    [EzIPC] public Func<bool> TeleportToFC;
    [EzIPC] public Func<bool> TeleportToApartment;
    [EzIPC] public Func<bool> IsBusy;
    [EzIPC] public Action<string> ExecuteCommand;
    [EzIPC] public Action Abort;
}
