using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameLibraryViewer.Models;
using Microsoft.Win32;

namespace GameLibraryViewer.Threads
{
    public class EpicGamesLibraryRetriever : IGame
    {
        private bool _registryFound;
        private string _epicGamesInstallPath;
        private GameModel _game;
        private List<IObserver> _observers = new List<IObserver>();
        private string _epicGamesLibraryPath;
        private FileSystemWatcher _watcher = new FileSystemWatcher();

        public bool RegistryFound
        {
            get { return _registryFound; }
        }

        public string EpicGamesInstallPath
        {
            get { return _epicGamesInstallPath; }
        }

        public GameModel Game
        {
            get { return _game; }
            set
            {
                _game = value;
            }
        }

        public void Attach(IObserver observer)
        {
            _observers.Add(observer);
        }

        public void Detach(IObserver observer)
        {
            _observers.Remove(observer);
        }


        public void Notify(WatcherChangeTypes changeType)
        {
            foreach (IObserver observer in _observers)
            {
                observer.Update(Game, changeType);
            }
        }

        public EpicGamesLibraryRetriever()
        {
            _registryFound = false;
            try
            {
                if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node").GetSubKeyNames().Contains("Epic Games"))
                {
                    _epicGamesInstallPath = Registry.LocalMachine
                        .OpenSubKey(@"SOFTWARE\Wow6432Node\Epic Games\EpicGamesLauncher")
                        .GetValue("AppDataPath").ToString(); 
                    _registryFound = true;
                }
            }
            catch
            { }

            if (_registryFound)
            {
                _epicGamesLibraryPath = $@"{_epicGamesInstallPath}\Manifests";

                _watcher = new FileSystemWatcher(_epicGamesLibraryPath, "*.item");

                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                _watcher.Created += EpicGamesLibraryEvent;
                _watcher.Changed += EpicGamesLibraryEvent;
                _watcher.Deleted += EpicGamesLibraryEvent;
                _watcher.EnableRaisingEvents = true;
            }
        }

        private void EpicGamesLibraryEvent(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                string name = null;
                string id = null;
                string size = null;
                string path = null;
                string[] fileText = null;
                bool gotFileContent = false;

                while (gotFileContent == false)
                {
                    try
                    {
                        fileText = File.ReadAllLines(e.FullPath);
                        gotFileContent = true;
                    }
                    catch
                    {
                        gotFileContent = false;
                    }
                }

                foreach (string line in fileText)
                {
                    if (line.Contains("\"DisplayName\""))
                    {
                        name = line.Split('\"')[3];
                    }
                    else if (line.Contains("\"AppName\""))
                    {
                        id = line.Split('\"')[3];
                    }
                    else if (line.Contains("\"InstallSize\""))
                    {
                        size = line.Split('\"', ':', ',')[3].Trim();
                    }
                    else if (line.Contains("\"InstallLocation\""))
                    {
                        path = line.Split('\"')[3].Substring(0, 2);
                    }
                }
                Game = new GameModel(name, id, size, path, "EpicGames");
                Notify(e.ChangeType);
            }
            else
            {
                Game = new GameModel("", e.Name.Split('.')[0], "", "", "EpicGames");
                Notify(e.ChangeType);
            }
        }

        public void EpicGamesLibraryRetrieverThread()
        {
            List<FileInfo> gameFiles = new DirectoryInfo(_epicGamesLibraryPath).GetFiles("*.item", SearchOption.TopDirectoryOnly).ToList();

            foreach (FileInfo fi in gameFiles)
            {
                string name = null;
                string id = null;
                string size = null;
                string path = null;

                string[] fileText = File.ReadAllLines(fi.FullName);

                foreach (string line in fileText)
                {
                    if (line.Contains("\"DisplayName\""))
                    {
                        name = line.Split('\"')[3];
                    }
                    else if (line.Contains("\"InstallationGuid\""))
                    {
                        id = line.Split('\"')[3];
                    }
                    else if (line.Contains("\"InstallSize\""))
                    {
                        size = line.Split('\"', ':', ',')[3].Trim();
                    }
                    else if (line.Contains("\"InstallLocation\""))
                    {
                        path = line.Split('\"')[3].Substring(0, 2);
                    }
                }
                Game = new GameModel(name, id, size, path, "EpicGames");
                Notify(WatcherChangeTypes.Created);
            }
        }
    }
}
