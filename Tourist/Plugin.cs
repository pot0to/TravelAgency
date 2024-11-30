using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVWeather.Lumina;
using ECommons;
using ECommons.DalamudServices;
using ECommons.SimpleGui;
using System;

namespace Tourist {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : IDalamudPlugin
    {
        public static string Name => "Tourist";

        public Plugin(IDalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            ECommonsMain.Init(pluginInterface, this);

            Service.Plugin = this;
            Service.TaskManager = new();
            Service.TaskManager.DefaultConfiguration.AbortOnTimeout = true;
            Service.TaskManager.DefaultConfiguration.TimeLimitMS = 10000; // 10s
            Service.Config = Service.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            Service.Config.Initialise(this);
            //Service.Navmesh = new();
            NavmeshIPC.Init();
            Service.Lifestream = new();
            Service.GameFunctions = new();
            Service.ChatFunctions = new(Service.SigScanner, Service.DataManager, Service.GameFunctions);
            Service.Markers = new();
            Service.Weather = new FFXIVWeatherLuminaService(Service.DataManager.GameData);

            Service.Interface = pluginInterface;
            Service.Ui = new PluginUi(this);
            Service.Commands = new Commands(this);

            Service.VisitAll = new();
            Svc.Framework.Update += Service.VisitAll.OnUpdate;
        }

        public void Dispose()
        {
            Service.Commands.Dispose();
            Service.Ui.Dispose();
            Service.Markers.Dispose();
            Service.GameFunctions.Dispose();
            ECommonsMain.Dispose();
        }
    }
}
