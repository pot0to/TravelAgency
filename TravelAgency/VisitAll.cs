using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using Dalamud.Game.ClientState.Conditions;
using ImGuizmoNET;
using ECommons.Automation.LegacyTaskManager;
using Dalamud.Game.Text;

namespace TravelAgency
{
    public enum State
    {
        Ready,
        GoToSightseeingPoint,
        Move,
        Mount,
        Dismount,
        Emote
    }

    public class VisitAll
    {
        const uint first = 2162688;
        private List<Adventure> Adventures;
        private int NextAdventureIndex;
        private Adventure NextAdventure;

        private State State;
        
        
        private Lumina.Excel.Sheets.Aetheryte ClosestAetheryte;

        public VisitAll()
        {
            Adventures = Service.DataManager.GetExcelSheet<Adventure>()!
                    .Select(adventure => (idx: adventure.RowId - first, adventure))
                    .Where(adventure => !Service.GameFunctions.HasVistaUnlocked((short)adventure.idx))
                    .OrderBy(entry => Service.Config.SortMode switch
                    {
                        SortMode.Number => entry.idx,
                        SortMode.Zone => entry.adventure.Level.Value!.Map.RowId,
                        _ => throw new ArgumentOutOfRangeException(),
                    })
                    .Select(a => a.adventure)
                    .ToList();
            NextAdventureIndex = 0;
        }

        public unsafe void GoToSightseeingPoint()
        {
            var level = NextAdventure.Level.Value;
            var map = level.Map.Value;
            var territoryType = map.TerritoryType.Value;
            var sightseeingPos = new Vector3(level.X, level.Y, level.Z);

            var closestAetheryteToSightseeing = Map.FindClosestAetheryte(territoryType.RowId, sightseeingPos);
            if (closestAetheryteToSightseeing == null)
            {
                Svc.Log.Info($"Cannot find nearby aetheryte. Skipping sightseeing log #{NextAdventure.RowId}");
                State = State.Ready;
                return;
            }

            ClosestAetheryte = closestAetheryteToSightseeing!.Value;

            if (Svc.ClientState.TerritoryType != territoryType.RowId)
            {
                if (!ClosestAetheryte.IsAetheryte) // closest aetheryte is aethernet destination
                {
                    var primaryAetheryte = Map.FindPrimaryAetheryte(ClosestAetheryte);
                    Map.ExecuteTeleport(primaryAetheryte);
                    return;
                }
                else
                {
                    Map.ExecuteTeleport(ClosestAetheryte);
                    return;
                }
            }

            // now in correct  territory
            var distancePlayerToSightseeing = Vector3.Distance(Svc.ClientState.LocalPlayer!.Position, sightseeingPos);

            if (distancePlayerToSightseeing < 5)
            {
                if (!NavmeshIPC.PathfindInProgress() && !NavmeshIPC.PathIsRunning())
                {
                    if (Svc.Condition[ConditionFlag.Mounted])
                        Map.ExecuteDismount();
                    else State = State.Emote;
                }
                return;
            }

            var aetherytePos = Map.AetherytePosition(ClosestAetheryte);
            var distanceAetheryteToSightseeing = Vector3.Distance(aetherytePos, sightseeingPos);
            var flyingUnlocked = PlayerState.Instance()->IsAetherCurrentZoneComplete(territoryType.Unknown4);
            if ( distancePlayerToSightseeing < distanceAetheryteToSightseeing + 20) // closer than any aetheryte, fly straight there
            {
                if (!NavmeshIPC.PathfindInProgress() && !NavmeshIPC.PathIsRunning())
                {
                    var flying = territoryType.Mount && flyingUnlocked;
                    if (distanceAetheryteToSightseeing < 50)
                        flying = false;
                    if (flying && !Svc.Condition[ConditionFlag.Mounted])
                    {
                        Map.ExecuteMount();
                        return;
                    }
                    Service.TaskManager.Enqueue(() => NavmeshIPC.PathfindAndMoveTo(sightseeingPos, flying));
                }
                return;
            }

            var closestAetheryteToPlayer = Map.FindClosestAetheryte(territoryType.RowId, Svc.ClientState.LocalPlayer!.Position)!.Value;
            var distancePlayerToClosestAetheryte = Vector3.Distance(Svc.ClientState.LocalPlayer!.Position, Map.AetherytePosition(closestAetheryteToPlayer));
            if (closestAetheryteToPlayer.RowId != ClosestAetheryte.RowId && distancePlayerToSightseeing < distancePlayerToClosestAetheryte + distanceAetheryteToSightseeing + 20)
            {
                if (distancePlayerToClosestAetheryte < 8)
                    Service.TaskManager.Enqueue(() => Service.Lifestream.AethernetTeleport(ClosestAetheryte.AethernetName.Value.Name.ExtractText()));
                else
                {
                    if (!NavmeshIPC.PathfindInProgress() && !NavmeshIPC.PathIsRunning())
                    {
                        Service.TaskManager.Enqueue(() => NavmeshIPC.PathfindAndMoveTo(sightseeingPos, false));
                    }
                }
                return;
            }
        }

        public unsafe void OnUpdate(IFramework framework) //IOrderedEnumerable<(uint idx, Adventure adventure)> adventures)
        {
            if (!Service.Config.Active || Service.TaskManager.IsBusy || Svc.ClientState.LocalPlayer is null)
            {
                return;
            }

            if (NextAdventureIndex >= Adventures.Count())
            {
                Svc.Chat.Print(new XivChatEntry()
                {
                    Type = XivChatType.Echo,
                    Message = "[TravelAgency] No more uncollected sightseeing logs available at this time. Turning off auto mode.",
                });
                Svc.Log.Info("[TravelAgency] No more uncollected sightseeing logs available at this time. Turning off auto mode.");
                Service.Config.Active = false;
                return;
            }


            switch (this.State)
            {
                case State.Ready:
                    NextAdventure = Adventures[NextAdventureIndex];
                    NextAdventureIndex++;

                    if (!NextAdventure.Available(Service.Weather))
                    {
                        Svc.Log.Info($"Sightseeing log #{NextAdventure.RowId}: {NextAdventure.Name.ExtractText()} is currently unavailable due to time or weather. Skipping.");
                        return;
                    }

                    Svc.Log.Info($"Next sightseeing log is #{NextAdventure.RowId}: {NextAdventure.Name.ExtractText()}");

                    Service.GameGui.OpenMapLocation(NextAdventure);

                    State = State.GoToSightseeingPoint;
                    break;
                case State.GoToSightseeingPoint:
                    GoToSightseeingPoint();
                    break;
                case State.Emote:
                    Svc.Log.Info($"Executing emote {NextAdventure.Emote.Value.Name.ExtractText()}");
                    Service.TaskManager.Enqueue(() => Service.ChatFunctions.UseEmote(NextAdventure.Emote.Value.RowId));
                    Service.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Emoting]);
                    Service.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Emoting]);
                    State = State.Ready;
                    break;
                default:
                    break;
            }
        }
    }
}
