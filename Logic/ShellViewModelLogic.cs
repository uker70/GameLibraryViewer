using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using GameLibraryViewer.Models;
using GameLibraryViewer.ViewModels;

namespace GameLibraryViewer.Logic
{
    public class ShellViewModelLogic
    {
        public static List<GameModel> FilterShownGames(string launcher, List<GameModel> gameList)
        {
            List<GameModel> gameMatches = new List<GameModel>();
            foreach (GameModel game in gameList)
            {
                if (game.Launcher.ToUpper() == launcher)
                {
                    gameMatches.Add(game);
                }
            }
            return gameMatches;
        }
    }
}
