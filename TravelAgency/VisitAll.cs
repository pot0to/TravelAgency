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

namespace TravelAgency
{
    public class VisitAll
    {

        public unsafe void OnUpdate(IFramework framework) //IOrderedEnumerable<(uint idx, Adventure adventure)> adventures)
        {
            if (!Service.Config.Active || Service.TaskManager.IsBusy || Svc.ClientState.LocalPlayer is null)
            {
                return;
            }

            const uint first = 2162688;
            var adventures = Service.DataManager.GetExcelSheet<Adventure>()!
                    .Select(adventure => (idx: adventure.RowId - first, adventure))
                    .Where(adventure => !Service.GameFunctions.HasVistaUnlocked((short)adventure.idx) && adventure.adventure.Available(Service.Weather))
                    .OrderBy(entry => Service.Config.SortMode switch
                    {
                        SortMode.Number => entry.idx,
                        SortMode.Zone => entry.adventure.Level.Value!.Map.RowId,
                        _ => throw new ArgumentOutOfRangeException(),
                    });

            foreach (var (idx, adventure) in adventures)
            {

                var level = adventure.Level.Value;
                var map = level.Map.Value;
                var territoryType = map.TerritoryType.Value;

                var pos = new Vector3(level.X, level.Y, level.Z);

                Service.GameGui.OpenMapLocation(adventure);

                var closestAetheryte = Map.FindClosestAetheryte(territoryType.RowId, pos);

                if (closestAetheryte == null) {
                    Svc.Log.Info($"Could not find aetheryte for sightseeing log #{adventure.RowId}. Skipping");
                    continue;
                }

                if (territoryType.RowId != Svc.ClientState.TerritoryType) //tp to main aetheryte
                {
                    if (!closestAetheryte.Value.IsAetheryte)
                    {
                        Service.Log.Info("Closest aetheryte is mini aetheryte.");
                        var mainAetheryte = Map.FindPrimaryAetheryte(closestAetheryte.Value);

                        if (territoryType.RowId != mainAetheryte.Territory.RowId)
                        {
                            Svc.Log.Info($"Teleporting to main aetheryte: {mainAetheryte.PlaceName.Value.Name.ExtractText()}");
                            Service.TaskManager.Enqueue(() => Telepo.Instance()->Teleport(mainAetheryte.RowId, 0));
                            Service.TaskManager.Enqueue(() => Svc.ClientState.TerritoryType == mainAetheryte.Territory.RowId);
                            Service.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
                            Service.TaskManager.Enqueue(() => Svc.Log.Info($"Successfuly teleported to main aetheryte: {mainAetheryte.PlaceName.Value.Name.ExtractText()}"));
                        }
                    }
                    else
                    {
                        Svc.Log.Info($"Teleporting to {territoryType.Name.ExtractText()}");
                        Service.TaskManager.Enqueue(() => Telepo.Instance()->Teleport(closestAetheryte.Value.RowId, 0));
                        Service.TaskManager.Enqueue(() => territoryType.RowId == Svc.ClientState.TerritoryType);
                        Service.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
                        Service.TaskManager.Enqueue(() => Svc.Log.Info($"Successfuly teleported to {territoryType.Name.ExtractText()}"));
                    }
                    return;
                }

                var distanceToPos = Vector3.Distance(pos, Svc.ClientState.LocalPlayer!.Position);
                if (!closestAetheryte.Value.IsAetheryte) // use mini aetheryte
                {
                    var destinationAethernetPos = Map.AetherytePosition(closestAetheryte!.Value);

                    var startAethernet = Map.FindClosestAetheryte(territoryType.RowId, Svc.ClientState.LocalPlayer.Position);
                    if (startAethernet?.RowId != closestAetheryte.Value.RowId)
                    {
                        var startAethernetPos = Map.AetherytePosition(startAethernet!.Value);
                        var distanceViaAethernet = Vector3.Distance(Svc.ClientState.LocalPlayer.Position, startAethernetPos) + Vector3.Distance(destinationAethernetPos, pos);

                        if (distanceToPos > distanceViaAethernet + 10)
                        {
                            var miniAetheryteName = closestAetheryte?.AethernetName.Value.Name.ExtractText();
                            Svc.Log.Info($"Teleporting to mini aetheryte #{closestAetheryte?.RowId}: {miniAetheryteName}");
                            Service.TaskManager.Enqueue(() => Service.Lifestream.AethernetTeleport(miniAetheryteName));
                            Service.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas]);
                            Service.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
                            Service.TaskManager.Enqueue(() => Svc.Log.Info($"Successfuly teleported to mini aetheryte: {closestAetheryte.Value.PlaceName.Value.Name.ExtractText()}"));
                            return;
                        }
                    }
                }

                if (distanceToPos > 1)
                {
                    Svc.Log.Info($"Not within 5 distance of ({pos.X}, {pos.Y}, {pos.Z})");
                    if (!Svc.Condition[ConditionFlag.Mounted] && territoryType.Mount) // not mounted
                    {
                        Svc.Log.Info($"Not mounted");
                        Service.TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 24)); //ExecuteActionSafe(ActionType.GeneralAction, 24));
                        Service.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Mounted]);
                        Service.TaskManager.Enqueue(() => Svc.Log.Info("Successfully mounted"));
                        return;
                    }

                    //await WaitWhile(() => NavBuildProgress() >= 0, "BuildMesh");
                    //ErrorIf(!NavIsReady(), "Failed to build navmesh for the zone");
                    //ErrorIf(!NavPathfindAndMoveTo(dest, fly), "Failed to start pathfinding to destination");
                    //using var stop = new OnDispose(NavStop);
                    //await WaitWhile(() => !Game.PlayerInRange(dest, tolerance), "Navigate");
                    if (!NavmeshIPC.PathfindInProgress() && !NavmeshIPC.PathIsRunning())
                    {
                        var flyingUnlocked = PlayerState.Instance()->IsAetherCurrentZoneComplete(territoryType.Unknown4);
                        Svc.Log.Info($"Navmesh move to: {pos.X}, {pos.Y}, {pos.Z}");
                        Service.TaskManager.Enqueue(() => NavmeshIPC.PathfindAndMoveTo(pos, fly: territoryType.Mount && flyingUnlocked));
                        Service.TaskManager.Enqueue(() => NavmeshIPC.PathIsRunning());
                        Service.TaskManager.Enqueue(() => !NavmeshIPC.PathIsRunning());
                    }
                    return;
                }

                if (Svc.Condition[ConditionFlag.Mounted]) // dismount
                {
                    Service.TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 23)); //ExecuteActionSafe(ActionType.GeneralAction, 24));
                    Service.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Mounted]);
                    Service.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.NormalConditions]);
                    return;
                }

                Svc.Log.Info($"Executing emote {adventure.Emote.Value.Name.ExtractText()}");
                Service.TaskManager.Enqueue(() => Service.ChatFunctions.UseEmote(adventure.Emote.Value.RowId));
                Service.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Emoting]);
                Service.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Emoting]);
            }

            Service.Config.Active = false;
        }
    }
}
