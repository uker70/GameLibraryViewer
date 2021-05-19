using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Caliburn.Micro;
using ConvertDatatypes.Byte;
using GameLibraryViewer.Logic;
using GameLibraryViewer.Models;
using GameLibraryViewer.Threads;

namespace GameLibraryViewer.ViewModels
{
    public class ShellViewModel : Screen, IObserver
    {
        private BindableCollection<GameModel> _games = new BindableCollection<GameModel>();
        private List<GameModel> _allGames = new List<GameModel>();
        private SteamLibraryRetriever _slr = new SteamLibraryRetriever();
        private EpicGamesLibraryRetriever _eglr = new EpicGamesLibraryRetriever();
        private OriginLibraryRetriever _olr = new OriginLibraryRetriever();
        private GameModel _selectedGame;
        private string _gameBanner;
        private string _gameImage;
        private string _search;
        private long _gameSizeTotal;
        private bool _showSteamGames;
        private bool _showOriginGames;
        private bool _showUplayGames;
        private bool _showEPGames;
        private bool _showBattlenetGames;
        private bool _showGogGames;

        public ShellViewModel()
        {
            IObserver observer = this;

            if (_slr.RegistryFound)
            {
                _slr.Attach(observer);
                Thread getSteamGames = new Thread(new ThreadStart(_slr.SteamLibraryRetrieverThread));
                getSteamGames.Start();
            }

            if (_eglr.RegistryFound)
            {
                _eglr.Attach(observer);
                Thread getEpicGamesGames = new Thread(new ThreadStart(_eglr.EpicGamesLibraryRetrieverThread));
                getEpicGamesGames.Start();
            }

            if (_olr.RegistryFound)
            {
                _olr.Attach(observer);
                Thread getOriginGames = new Thread(new ThreadStart(_olr.OriginLibraryRetrieverThread));
                getOriginGames.Start();
            }

            _allGames.Add(new GameModel("test3", "3", "1", "C:", "GoG"));
        }

        #region Launcher public bools
        public bool ShowSteamGames
        {
            get { return _showSteamGames; }
            set
            {
                _showSteamGames = value;
                _FilterShownGames(value, "STEAM");
            }
        }

        public bool ShowOriginGames
        {
            get { return _showOriginGames; }
            set
            {
                _showOriginGames = value;
                _FilterShownGames(value, "ORIGIN");
            }
        }

        public bool ShowUplayGames
        {
            get { return _showUplayGames; }
            set
            {
                _showUplayGames = value;
                _FilterShownGames(value, "UPLAY");
            }
        }

        public bool ShowEPGames
        {
            get { return _showEPGames; }
            set
            {
                _showEPGames = value;
                _FilterShownGames(value, "EPICGAMES");
            }
        }

        public bool ShowBattlenetGames
        {
            get { return _showBattlenetGames; }
            set
            {
                _showBattlenetGames = value;
                _FilterShownGames(value, "BATTLENET");
            }
        }

        public bool ShowGogGames
        {
            get { return _showGogGames; }
            set
            {
                _showGogGames = value;
                _FilterShownGames(value, "GOG");
            }
        }

        private void _FilterShownGames(bool showGames, string launcher)
        {
            if (showGames)
            {
                Games.AddRange(ShellViewModelLogic.FilterShownGames(launcher, _allGames));
            }
            else
            {
                Games.RemoveRange(ShellViewModelLogic.FilterShownGames(launcher, Games.ToList()));
            }

            List<GameModel> bufferGameList = Games.ToList();
            Games.Clear();
            Games.AddRange(bufferGameList.OrderBy(x => x.Name));
            _UpdateTotalGameSize();
            NotifyOfPropertyChange(() => GameSizeTotal);
            if (Search != null)
            {
                Search = Search;
            }
        }
        #endregion

        public string Search
        {
            get { return _search; }
            set
            {
                _search = value;
                List<GameModel> bufferGameList = Games.ToList();
                Games.Clear();
                if (_search.Length != 0)
                {
                    Games.AddRange(bufferGameList.OrderByDescending(x => x.Name.ToLower().StartsWith(_search)));
                }
                else
                {
                    Games.AddRange(bufferGameList.OrderBy(x => x.Name));
                }
                NotifyOfPropertyChange(() => Search);
            }
        }

        public string GameBanner
        {
            get { return _gameBanner; }
            set
            {
                _gameBanner = value;
                NotifyOfPropertyChange(() => GameBanner);
            }
        }

        public string GameImage
        {
            get { return _gameImage; }
            set
            {
                _gameImage = value;
                NotifyOfPropertyChange(() => GameImage);
            }
        }

        public string GameSizeTotal
        {
            get { return ConvertFromBytes.BytesToFileSizeString(_gameSizeTotal); }
        }

        public string SelectedGameSize
        {
            get {
                if (SelectedGame != null)
                {
                    return ConvertFromBytes.BytesToFileSizeString(Convert.ToInt64(SelectedGame.Size));
                }

                return null;
            }
        }

        public BindableCollection<GameModel> Games
        {
            get { return _games; }
            set
            {
                _games = value;
            }
        }

        public GameModel SelectedGame
        {
            get { return _selectedGame; }
            set
            {
                if (value != null)
                {
                    _selectedGame = value;
                    _GetGameImage(value);
                    NotifyOfPropertyChange(() => SelectedGame);
                    NotifyOfPropertyChange(() => SelectedGameSize);
                }
            }
        }

        public void Update(GameModel newGame, WatcherChangeTypes changeType)
        {
            if (changeType == WatcherChangeTypes.Created)
            {
                // add new game

                bool gameLauncherIsShown = false;
                _allGames.Add(newGame);
                switch (newGame.Launcher.ToUpper())
                {
                    case "STEAM":
                        if (ShowSteamGames)
                        {
                            gameLauncherIsShown = true;
                        }
                        break;

                    case "EPICGAMES":
                        if (ShowEPGames)
                        {
                            gameLauncherIsShown = true;
                        }
                        break;

                    case "UPLAY":
                        break;

                    case "ORIGIN":
                        if (ShowOriginGames)
                        {
                            gameLauncherIsShown = true;
                        }
                        break;

                    case "GOG":
                        break;

                    case "BATTLENET":
                        break;
                }

                if (gameLauncherIsShown)
                {
                    Games.Add(newGame);
                    List<GameModel> bufferGameList = Games.ToList();
                    Games.Clear();
                    Games.AddRange(bufferGameList.OrderBy(x => x.Name));
                    _UpdateTotalGameSize();
                    NotifyOfPropertyChange(() => GameSizeTotal);
                    if (Search != null)
                    {
                        Search = Search;
                    }
                }
            }
            else if (changeType == WatcherChangeTypes.Deleted)
            {
                // delete existing game

                GameModel gameToDelete = _allGames[_allGames.FindIndex(x => x.Id == newGame.Id)];
                _allGames.Remove(gameToDelete);

                if (Games.Contains(gameToDelete))
                {
                    Games.Remove(gameToDelete);
                    _UpdateTotalGameSize();
                }
            }
            else if(changeType == WatcherChangeTypes.Changed)
            {
                // update for existing game

                GameModel gameToUpdate = _allGames[_allGames.FindIndex(x => x.Id == newGame.Id)];
                _allGames[_allGames.IndexOf(gameToUpdate)] = newGame;

                switch (newGame.Launcher.ToUpper())
                {
                    case "STEAM":
                        try
                        {
                            if (ShowSteamGames)
                            {
                                Games[Games.IndexOf(gameToUpdate)] = newGame;
                                _UpdateTotalGameSize();
                            }
                        }
                        catch
                        { }
                        break;

                    case "EPICGAMES":
                        try
                        {
                            if (ShowEPGames)
                            {
                                Games[Games.IndexOf(gameToUpdate)] = newGame;
                                _UpdateTotalGameSize();
                            }
                        }
                        catch
                        { }
                        break;

                    case "UPLAY":
                        break;

                    case "ORIGIN":
                        try
                        {
                            if (ShowOriginGames)
                            {
                                Games[Games.IndexOf(gameToUpdate)] = newGame;
                                _UpdateTotalGameSize();
                            }
                        }
                        catch
                        { }
                        break;

                    case "GOG":
                        break;

                    case "BATTLENET":
                        break;
                }
            }
        }

        private void _UpdateTotalGameSize()
        {
            _gameSizeTotal = 0;
            foreach (GameModel game in Games)
            {
                _gameSizeTotal += Convert.ToInt64(game.Size);
            }
            NotifyOfPropertyChange(() => GameSizeTotal);
        }

        private void _GetGameImage(GameModel selectedGame)
        {
            switch (selectedGame.Launcher.ToUpper())
            {
                case "STEAM":
                    GameBanner = _slr.SteamInstallPath + @"\appcache\librarycache\" + selectedGame.Id + "_library_hero.jpg";
                    GameImage = _slr.SteamInstallPath + @"\appcache\librarycache\" + selectedGame.Id + "_library_600x900.jpg";
                    break;

                case "EPICGAMES":
                    GameBanner = null;
                    GameImage = null;
                    break;

                case "UPLAY":
                    GameBanner = null;
                    GameImage = null;
                    break;

                case "ORIGIN":
                    GameBanner = null;
                    GameImage = null;
                    break;

                case "GOG":
                    GameBanner = null;
                    GameImage = null;
                    break;

                case "BATTLENET":
                    GameBanner = null;
                    GameImage = null;
                    break;
            }
        }

        public void LaunchPlatform(string platform)
        {
            try
            {
                switch (platform.Split(':')[1].ToUpper().TrimStart())
                {
                    case "STEAM":
                        Process.Start("steam:");
                        break;

                    case "EPIC GAMES":
                        Process.Start("com.epicgames.launcher:");
                        break;

                    case "UPLAY":
                        Process.Start("");
                        break;

                    case "ORIGIN":
                        Process.Start("origin:");
                        break;

                    case "GOG":
                        Process.Start("");
                        break;

                    case "BATTLENET":
                        Process.Start("battlenet:");
                        break;
                }
            }
            catch
            { }
        }

        public void LaunchGame()
        {
            try
            {
                switch (SelectedGame.Launcher.ToUpper())
                {
                    case "STEAM":
                        Process.Start("steam://rungameid/" + SelectedGame.Id);
                        break;

                    case "EPICGAMES":
                        Process.Start("com.epicgames.launcher://apps/"+SelectedGame.Id+"?action=launch&silent=true");
                        break;

                    case "UPLAY":
                        Process.Start("uplay://launch" + SelectedGame.Id + "/0");
                        break;

                    case "ORIGIN":
                        Process.Start("origin://launchgame/" + SelectedGame.Id);
                        break;

                    case "GOG":
                        Process.Start("");
                        break;

                    case "BATTLENET":
                        Process.Start("battlenet://GAME");
                        break;
                }
            }
            catch
            { }
        }
    }
}
