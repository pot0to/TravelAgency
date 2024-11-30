using System;
using Dalamud.Game.Command;

namespace Tourist {
    public class Commands : IDisposable {

        public Commands(Plugin plugin) {

            Service.CommandManager.AddHandler("/tourist", new CommandInfo(this.OnCommand) {
                HelpMessage = "Opens the Tourist interface",
            });
        }

        public void Dispose() {
            Service.CommandManager.RemoveHandler("/tourist");
        }

        private void OnCommand(string command, string arguments) {
            Service.Ui.Show = !Service.Ui.Show;
        }
    }
}
