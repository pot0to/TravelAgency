using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Lumina.Extensions;
using ECommons.DalamudServices;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game;


namespace TravelAgency
{
    public static class Map
    {
        public static Vector3 PixelCoordsToWorldCoords(int x, int z, uint mapId)
        {
            var map = Service.LuminaRow<Lumina.Excel.Sheets.Map>(mapId);
            //var map = Plugin.DataManager.GameData?.GetExcelSheet<Lumina.Excel.Sheets.Map>(Lumina.Data.Language.English)?.GetRowOrDefault(mapId);
            var scale = (map?.SizeFactor ?? 100) * 0.01f;
            var wx = PixelCoordToWorldCoord(x, scale, map?.OffsetX ?? 0);
            var wz = PixelCoordToWorldCoord(z, scale, map?.OffsetY ?? 0);
            return new(wx, 0, wz);
        }

        // see: https://github.com/xivapi/ffxiv-datamining/blob/master/docs/MapCoordinates.md
        // see: dalamud MapLinkPayload class
        public static float PixelCoordToWorldCoord(float coord, float scale, short offset)
        {
            // +1 - networkAdjustment == 0
            // (coord / scale * 2) * (scale / 100) = coord / 50
            // * 2048 / 41 / 50 = 0.999024
            const float factor = 2048.0f / (50 * 41);
            return (coord * factor - 1024f) / scale - offset * 0.001f;
        }

        public static Aetheryte? FindClosestAetheryte(uint territoryTypeId, Vector3 worldPos)
        {
            var aetherytes = Service.LuminaSheet<Aetheryte>()?.Where(a => a.Territory.RowId == territoryTypeId);
            Svc.Log.Info($"Number of aetherytes found in zone #{territoryTypeId}: {aetherytes?.Count()}");
            return aetherytes?.Count() > 0 ? aetherytes.MinBy(a => Vector3.Distance(worldPos, AetherytePosition(a))) : null;
        }

        public static Vector3 AetherytePosition(Aetheryte a)
        {
            // stolen from HTA, uses pixel coordinates
            var level = a.Level[0].ValueNullable;
            if (level != null)
            {
                return new(level.Value.X, level.Value.Y, level.Value.Z);
            }
            var marker = Service.LuminaSheetSubrow<MapMarker>()!.Flatten().FirstOrNull(m => m.DataType == 3 && m.DataKey.RowId == a.RowId)
                ?? Service.LuminaSheetSubrow<MapMarker>()!.Flatten().First(m => m.DataType == 4 && m.DataKey.RowId == a.AethernetName.RowId);
            return PixelCoordsToWorldCoords(marker.X, marker.Y, a.Territory.Value.Map.RowId);
        }

        // if aetheryte is 'primary' (i.e. can be teleported to), return it; otherwise (i.e. aethernet shard) find and return primary aetheryte from same group
        public static Aetheryte FindPrimaryAetheryte(Aetheryte aetheryte)
        {
            if (aetheryte.IsAetheryte)
                return aetheryte;
            var primary = Service.LuminaSheet<Aetheryte>()!.FirstOrDefault(a => a.AethernetGroup == aetheryte.AethernetGroup);
            return primary;
        }

        public static void OpenMapLocation(this IGameGui gameGui, Adventure adventure)
        {
            var loc = adventure.Level.Value;
            var map = loc.Map.Value;
            var terr = map.TerritoryType.Value;

            //if (terr == null) {
            //    return;
            //}

            var mapLink = new MapLinkPayload(
                terr.RowId,
                map!.RowId,
                (int)(loc!.X * 1_000f),
                (int)(loc.Z * 1_000f)
            );

            gameGui.OpenMapWithMapLink(mapLink);
        }

        public static unsafe void ExecuteTeleport(Aetheryte aetheryte)
        {
            Service.Log.Info($"Teleporting to aetheryte: {aetheryte.PlaceName.Value.Name.ExtractText()}");
            Service.TaskManager.Enqueue(() => Telepo.Instance()->Teleport(aetheryte.RowId, 0));
            Service.TaskManager.Enqueue(() => Svc.ClientState.TerritoryType == aetheryte.Territory.RowId);
            Service.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
            Service.TaskManager.Enqueue(() => Svc.Log.Info($"Successfuly teleported to aetheryte: {aetheryte.PlaceName.Value.Name.ExtractText()}"));
        }
        public static unsafe void ExecuteMount()
        {
            Svc.Log.Info("Dismounting");
            Service.TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 24));
            Service.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Mounted] && !Svc.Condition[ConditionFlag.Jumping]);
            Svc.Log.Info("Successfully dismounted");
            return;
        }
        public static unsafe void ExecuteDismount()
        {
            Svc.Log.Info("Dismounting");
            Service.TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 23));
            Service.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Mounted] && Svc.Condition[ConditionFlag.NormalConditions]);
            Svc.Log.Info("Successfully dismounted");
            return;
        }
    }
}
