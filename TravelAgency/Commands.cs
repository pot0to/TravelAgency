using System;
using Dalamud.Game.Command;

namespace TravelAgency {
    public class Commands : IDisposable {

        public Commands(Plugin plugin) {

            Service.CommandManager.AddHandler("/travelagency", new CommandInfo(this.OnCommand) {
                HelpMessage = "Opens the Tourist interface",
            });
        }

        public void Dispose() {
            Service.CommandManager.RemoveHandler("/travelagency");
        }

        private void OnCommand(string command, string arguments) {
            Service.Ui.Show = !Service.Ui.Show;
        }
    }
}
