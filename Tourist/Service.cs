using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using FFXIVWeather.Lumina;
using ECommons.Automation.NeoTaskManager;
using Dalamud.Plugin;

namespace Tourist
{
    public class Service
    {
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IDalamudPluginInterface Interface { get; set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
        [PluginService] public static ICondition Conditions { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
        [PluginService] public static IGameGui GameGui { get; set; } = null!;


        public static Plugin Plugin { get; set; } = null!;
        public static Lumina.GameData LuminaGameData => DataManager.GameData;
        public static Lumina.Excel.ExcelSheet<T>? LuminaSheet<T>() where T : struct, Lumina.Excel.IExcelRow<T> => LuminaGameData?.GetExcelSheet<T>(Lumina.Data.Language.English);
        public static Lumina.Excel.SubrowExcelSheet<T>? LuminaSheetSubrow<T>() where T : struct, Lumina.Excel.IExcelSubrow<T> => LuminaGameData?.GetSubrowExcelSheet<T>(Lumina.Data.Language.English);
        public static T? LuminaRow<T>(uint row) where T : struct, Lumina.Excel.IExcelRow<T> => LuminaSheet<T>()?.GetRowOrDefault(row);
        public static Lumina.Excel.SubrowCollection<T>? LuminaSubrows<T>(uint row) where T : struct, Lumina.Excel.IExcelSubrow<T> => LuminaSheetSubrow<T>()?.GetRowOrDefault(row);
        public static T? LuminaRow<T>(uint row, ushort subRow) where T : struct, Lumina.Excel.IExcelSubrow<T> => LuminaSheetSubrow<T>()?.GetSubrowOrDefault(row, subRow);

        public static TaskManager TaskManager { get; set; } = null!;
        
        public static Configuration Config { get; set; } = null!;
        public static NavmeshIPC Navmesh { get; set; } = null!;
        public static LifestreamIPC Lifestream { get; set; } = null!;
        public static GameFunctions GameFunctions { get; set; } = null!;
        public static ChatFunctions ChatFunctions { get; set; } = null!;
        public static Markers Markers { get; set; } = null!;
        public static PluginUi Ui { get; set; } = null!;
        public static Commands Commands { get; set; } = null!;
        public static VisitAll VisitAll { get; set; } = null!;
        public static FFXIVWeatherLuminaService Weather { get; set; } = null!;
    }

}
