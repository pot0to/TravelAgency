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


namespace Tourist
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
            Svc.Log.Info($"Found {a.Level.Count} levels for aetheryte {a.RowId}: {a.Level}");
            if (level != null)
            {
                Svc.Log.Info($"Level: {level.Value.X}, {level.Value.Y}, {level.Value.Z}");
                return new(level.Value.X, level.Value.Y, level.Value.Z);
            }
            else
            {
                Svc.Log.Info($"Level is null");
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
    }
}
