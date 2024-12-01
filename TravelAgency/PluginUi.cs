using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace TravelAgency {
    public class PluginUi : IDisposable {
        private Plugin Plugin { get; }

        private bool _show;

        internal bool Show {
            get => this._show;
            set => this._show = value;
        }

        public PluginUi(Plugin plugin) {
            this.Plugin = plugin;

            Service.Interface.UiBuilder.Draw += this.Draw;
            Service.Interface.UiBuilder.OpenConfigUi += this.OpenConfig;
        }

        public void Dispose() {
            Service.Interface.UiBuilder.OpenConfigUi -= this.OpenConfig;
            Service.Interface.UiBuilder.Draw -= this.Draw;
        }

        private void OpenConfig() {
            this.Show = true;
        }

        private void Draw() {
            ImGui.SetNextWindowSize(new Vector2(350f, 450f), ImGuiCond.FirstUseEver);

            if (!this.Show) {
                return;
            }

            if (!ImGui.Begin(Plugin.Name, ref this._show, ImGuiWindowFlags.MenuBar)) {
                ImGui.End();
                return;
            }

            if (ImGui.BeginMenuBar()) {
                if (ImGui.BeginMenu("Options")) {
                    if (ImGui.BeginMenu("Sort by")) {
                        foreach (var mode in (SortMode[]) Enum.GetValues(typeof(SortMode))) {
                            if (!ImGui.MenuItem($"{mode}", null, Service.Config.SortMode == mode)) {
                                continue;
                            }

                            Service.Config.SortMode = mode;
                            Service.Config.Save();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Times")) {
                        var showTimeUntil = Service.Config.ShowTimeUntilAvailable;
                        if (ImGui.MenuItem("Show time until available", null, ref showTimeUntil)) {
                            Service.Config.ShowTimeUntilAvailable = showTimeUntil;
                            Service.Config.Save();
                        }

                        var showTimeLeft = Service.Config.ShowTimeLeft;
                        if (ImGui.MenuItem("Show time left", null, ref showTimeLeft)) {
                            Service.Config.ShowTimeLeft = showTimeLeft;
                            Service.Config.Save();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Visibility")) {
                        var showFinished = Service.Config.ShowFinished;
                        if (ImGui.MenuItem("Show finished", null, ref showFinished)) {
                            Service.Config.ShowFinished = showFinished;
                            Service.Config.Save();
                        }

                        var showUnavailable = Service.Config.ShowUnavailable;
                        if (ImGui.MenuItem("Show unavailable", null, ref showUnavailable)) {
                            Service.Config.ShowUnavailable = showUnavailable;
                            Service.Config.Save();
                        }

                        var onlyCurrent = Service.Config.OnlyShowCurrentZone;
                        if (ImGui.MenuItem("Show current zone only", null, ref onlyCurrent)) {
                            Service.Config.OnlyShowCurrentZone = onlyCurrent;
                            Service.Config.Save();
                        }

                        ImGui.EndMenu();
                    }

                    var showArrVistas = Service.Config.ShowArrVistas;
                    if (ImGui.MenuItem("Add markers for ARR vistas", null, ref showArrVistas)) {
                        Service.Config.ShowArrVistas = showArrVistas;
                        Service.Config.Save();

                        if (showArrVistas) {
                            var territory = Svc.ClientState.TerritoryType;
                            Service.Markers.SpawnVfxForCurrentZone(territory);
                        } else {
                            Service.Markers.RemoveAllVfx();
                        }
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Help")) {
                    if (ImGui.BeginMenu("Can't unlock vistas 21 to 80")) {
                        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 10);
                        ImGui.TextUnformatted("Vistas 21 to 80 require the completion of the first 20. Talk to Millith Ironheart in Old Gridania to unlock the rest.");
                        ImGui.PopTextWrapPos();

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            const uint first = 2162688;
            var adventures = Service.DataManager.GetExcelSheet<Adventure>()!
                    .Select(adventure => (idx: adventure.RowId - first, adventure))
                    .OrderBy(entry => Service.Config.SortMode switch {
                        SortMode.Number => entry.idx,
                        SortMode.Zone => entry.adventure.Level.Value!.Map.RowId,
                        _ => throw new ArgumentOutOfRangeException(),
                    });

            ImGui.PushStyleColor(ImGuiCol.Button, Service.Config.Active ? ImGuiColors.HealerGreen : ImGuiColors.DalamudGrey3);
            ImGui.Button($"Visit All Sightseeing###Active");
            if (ImGui.IsItemClicked())
            {
                Service.Config.Active ^= true;
                if (!Service.Config.Active && (NavmeshIPC.PathfindInProgress() || NavmeshIPC.PathIsRunning()))
                {
                    NavmeshIPC.PathStop();
                }
            }
                
            ImGui.PopStyleColor();

            if (ImGui.BeginChild("travelagency-adventures", new Vector2(0, 0))) {

                Lumina.Excel.Sheets.Map? lastMap = null;
                var lastTree = false;

                foreach (var (idx, adventure) in adventures) {
                    if (Service.Config.OnlyShowCurrentZone && adventure.Level.Value!.Territory.RowId != Svc.ClientState.TerritoryType) {
                        continue;
                    }

                    var has = Service.GameFunctions.HasVistaUnlocked((short) idx);

                    if (!Service.Config.ShowFinished && has) {
                        continue;
                    }

                    var available = adventure.Available(Service.Weather);

                    if (!Service.Config.ShowUnavailable && !available) {
                        continue;
                    }

                    if (Service.Config.SortMode == SortMode.Zone) {
                        var map = adventure.Level.Value!.Map.Value;
                        if (lastMap?.Id != map.Id) {
                            if (lastMap != null) {
                                ImGui.TreePop();
                            }

                            lastTree = ImGui.CollapsingHeader($"{map!.PlaceName.Value!.Name}");
                            ImGui.TreePush();
                        }

                        lastMap = map;

                        if (!lastTree) {
                            continue;
                        }
                    }

                    var availability = adventure.NextAvailable(Service.Weather);

                    DateTimeOffset? countdown = null;
                    if (has) {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 1f));
                    } else if (available) {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 1f, 0f, 1f));
                        if (Service.Config.ShowTimeLeft) {
                            countdown = availability?.end;
                        }
                    } else if (availability != null && Service.Config.ShowTimeUntilAvailable) {
                        countdown = availability.Value.start;
                    }

                    var next = countdown == null
                        ? string.Empty
                        : $" ({(countdown.Value - DateTimeOffset.UtcNow).ToHumanReadable()})";

                    var name = adventure.Name.ToDalamudString();
                    var header = ImGui.CollapsingHeader($"#{idx + 1} - {name.TextValue}{next}###adventure-{adventure.RowId}");

                    if (has || available) {
                        ImGui.PopStyleColor();
                    }

                    if (!header) {
                        continue;
                    }

                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, ImGui.CalcTextSize("Eorzea time").X + ImGui.GetStyle().ItemSpacing.X * 2);

                    ImGui.TextUnformatted("Command");
                    ImGui.NextColumn();

                    ImGui.TextUnformatted(adventure.Emote.Value.TextCommand.Value.Command.ExtractText() ?? "<unk>");
                    ImGui.NextColumn();

                    ImGui.TextUnformatted("Eorzea time");
                    ImGui.NextColumn();

                    if (adventure.MinTime != 0 || adventure.MaxTime != 0) {
                        ImGui.TextUnformatted($"{adventure.MinTime / 100:00}:00 to {adventure.MaxTime / 100 + 1:00}:00");
                    } else {
                        ImGui.TextUnformatted("Any");
                    }

                    ImGui.NextColumn();

                    ImGui.TextUnformatted("Weather");
                    ImGui.NextColumn();

                    if (Weathers.All.TryGetValue(adventure.RowId, out var weathers)) {
                        var weatherString = string.Join(", ", weathers
                            .OrderBy(id => id)
                            .Select(id => Service.DataManager.GetExcelSheet<Weather>()!.GetRow(id))
                            //.Where(weather => weather != null)
                            .Cast<Weather>()
                            .Select(weather => weather.Name));
                        ImGui.TextUnformatted(weatherString);
                    } else {
                        ImGui.TextUnformatted("Any");
                    }

                    ImGui.Columns();

                    if (ImGui.Button($"Open map##{adventure.RowId}")) {
                        Service.GameGui.OpenMapLocation(adventure);
                    }
                }

                ImGui.EndChild();
            }

            ImGui.End();
        }
    }
}
