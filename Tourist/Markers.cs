﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Internal;
using Lumina.Excel.GeneratedSheets;

namespace Tourist {
    public class Markers : IDisposable {
        private const string MarkerPath = "bgcommon/world/common/vfx_for_live/eff/b0810_tnsk_y.avfx";

        private Plugin Plugin { get; }
        private Dictionary<uint, IntPtr> Spawned { get; } = new();
        private HashSet<IntPtr> Queue { get; } = new();

        public Markers(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.ClientState.TerritoryChanged += this.OnTerritoryChange;
            this.Plugin.Interface.Framework.OnUpdateEvent += this.OnFrameworkUpdate;

            if (this.Plugin.Config.ShowArrVistas) {
                this.SpawnVfxForCurrentZone(this.Plugin.Interface.ClientState.TerritoryType);
            }
        }

        public void Dispose() {
            this.Plugin.Interface.Framework.OnUpdateEvent -= this.OnFrameworkUpdate;
            this.Plugin.Interface.ClientState.TerritoryChanged -= this.OnTerritoryChange;
            this.RemoveAllVfx();
        }

        internal void RemoveVfx(ushort index) {
            var adventure = this.Plugin.Interface.Data.GetExcelSheet<Adventure>()
                .Skip(index)
                .First();

            if (!this.Spawned.TryGetValue(adventure.RowId, out var vfx)) {
                return;
            }

            this.Plugin.Functions.RemoveVfx(vfx);
            this.Spawned.Remove(adventure.RowId);
        }

        internal void RemoveAllVfx() {
            foreach (var vfx in this.Spawned.Values) {
                this.Plugin.Functions.RemoveVfx(vfx);
            }

            this.Spawned.Clear();
        }

        internal void SpawnVfxForCurrentZone(ushort territory) {
            var row = 0;
            foreach (var adventure in this.Plugin.Interface.Data.GetExcelSheet<Adventure>()) {
                if (row >= 80) {
                    break;
                }

                row += 1;

                if (adventure.Level.Value.Territory.Row != territory) {
                    continue;
                }

                if (this.Plugin.Functions.HasVistaUnlocked((short) (row - 1))) {
                    continue;
                }

                var loc = adventure.Level.Value;
                var pos = new Vector3(loc.X, loc.Z, loc.Y + 0.5f);
                var vfx = this.Plugin.Functions.SpawnVfx(MarkerPath, pos);
                this.Spawned[adventure.RowId] = vfx;
                this.Queue.Add(vfx);
            }
        }

        private void OnTerritoryChange(object sender, ushort territory) {
            if (!this.Plugin.Config.ShowArrVistas) {
                return;
            }

            this.RemoveAllVfx();

            this.SpawnVfxForCurrentZone(territory);
        }

        private void OnFrameworkUpdate(Framework framework) {
            foreach (var vfx in this.Queue.ToArray()) {
                if (this.Plugin.Functions.PlayVfx(vfx)) {
                    this.Queue.Remove(vfx);
                }
            }
        }
    }
}