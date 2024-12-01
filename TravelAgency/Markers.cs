using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace TravelAgency {
    public class Markers : IDisposable {
        private const string MarkerPath = "bgcommon/world/common/vfx_for_live/eff/b0810_tnsk_y.avfx";
        private Dictionary<uint, nint> Spawned { get; } = new();
        private HashSet<nint> Queue { get; } = new();

        public Markers() {
            Svc.ClientState.TerritoryChanged += this.OnTerritoryChange;
            Service.Framework.Update += this.OnFrameworkUpdate;

            if (Service.Config.ShowArrVistas) {
                this.SpawnVfxForCurrentZone(Svc.ClientState.TerritoryType);
            }
        }

        public void Dispose() {
            Service.Framework.Update -= this.OnFrameworkUpdate;
            Svc.ClientState.TerritoryChanged -= this.OnTerritoryChange;
            this.RemoveAllVfx();
        }

        internal void RemoveVfx(ushort index) {
            var adventure = Service.DataManager.GetExcelSheet<Adventure>()!
                .Skip(index)
                .First();

            if (!this.Spawned.TryGetValue(adventure.RowId, out var vfx)) {
                return;
            }

            Service.GameFunctions.RemoveVfx(vfx);
            this.Spawned.Remove(adventure.RowId);
        }

        internal void RemoveAllVfx() {
            foreach (var vfx in this.Spawned.Values) {
                Service.GameFunctions.RemoveVfx(vfx);
            }

            this.Spawned.Clear();
        }

        internal void SpawnVfxForCurrentZone(ushort territory) {
            var row = 0;
            foreach (var adventure in Service.DataManager.GetExcelSheet<Adventure>()!) {
                if (row >= 80) {
                    break;
                }

                row += 1;

                if (adventure.Level.Value!.Territory.RowId != territory) {
                    continue;
                }

                if (Service.GameFunctions.HasVistaUnlocked((short) (row - 1))) {
                    continue;
                }

                var loc = adventure.Level.Value;
                var pos = new Vector3(loc.X, loc.Z, loc.Y + 0.5f);
                var vfx = Service.GameFunctions.SpawnVfx(MarkerPath, pos);
                this.Spawned[adventure.RowId] = vfx;
                this.Queue.Add(vfx);
            }
        }

        private void OnTerritoryChange(ushort territory) {
            if (!Service.Config.ShowArrVistas) {
                return;
            }

            try {
                this.RemoveAllVfx();
                this.SpawnVfxForCurrentZone(territory);
            } catch (Exception ex) {
                Service.Log.Error(ex, "Exception in territory change");
            }
        }

        private void OnFrameworkUpdate(IFramework framework1) {
            foreach (var vfx in this.Queue.ToArray()) {
                if (Service.GameFunctions.PlayVfx(vfx)) {
                    this.Queue.Remove(vfx);
                }
            }
        }
    }
}
