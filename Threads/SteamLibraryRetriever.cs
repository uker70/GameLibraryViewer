using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameLibraryViewer.Models;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

namespace GameLibraryViewer.Threads
{
    public class SteamLibraryRetriever : IGame
    {
        private bool _registryFound;
        private string _steamInstallPath;
        private GameModel _game;
        private List<IObserver> _observers = new List<IObserver>();
        private List<string> _steamLibraryPaths = new List<string>();
        private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

        public bool RegistryFound
        {
            get { return _registryFound; }
        }

        public string SteamInstallPath
        {
            get { return _steamInstallPath; }
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

        public SteamLibraryRetriever()
        {
            _registryFound = false;

            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node").GetSubKeyNames().Contains("Valve"))
            {
                _steamInstallPath = Registry.LocalMachine
                    .OpenSubKey(@"SOFTWARE\Wow6432Node\Valve\Steam")
                    .GetValue("InstallPath").ToString();
                _registryFound = true;
            }

            if (_registryFound)
            {
                _steamLibraryPaths.Add(_steamInstallPath + @"\steamapps");

                dynamic libraryFile = VdfConvert.Deserialize(File.ReadAllText(_steamLibraryPaths[0] + @"\libraryfolders.vdf"));
                if (((VProperty)libraryFile).Value.Count() != 2)
                {
                    for (int i = 2; i < ((VProperty)libraryFile).Value.Count(); i++)
                    {
                        _steamLibraryPaths.Add(libraryFile.Value[i].Value.ToString() + @"\steamapps");
                    }

                    foreach (string steamLibraryPath in _steamLibraryPaths)
                    {
                        _watchers.Add(new FileSystemWatcher(steamLibraryPath, "*.acf"));
                    }

                    foreach (FileSystemWatcher watcher in _watchers)
                    {
                        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                        watcher.Created += SteamLibraryEvent;
                        watcher.Changed += SteamLibraryEvent;
                        watcher.Deleted += SteamLibraryEvent;
                        watcher.EnableRaisingEvents = true;
                    }
                }
            }
        }

        private void SteamLibraryEvent(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                if (e.Name != "appmanifest_228980.acf")
                {
                    string name = null;
                    string id = null;
                    string size = null;
                    string path = e.FullPath.Substring(0, e.FullPath.IndexOf('\\'));
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
                        if (line.Contains("\"name\""))
                        {
                            name = line.Split('\"')[3];
                        }
                        else if (line.Contains("\"appid\""))
                        {
                            id = line.Split('\"')[3];
                        }
                        else if (line.Contains("\"SizeOnDisk\""))
                        {
                            size = line.Split('\"')[3];
                        }
                    }
                    Game = new GameModel(name, id, size, path, "Steam");
                    Notify(e.ChangeType);
                }
            }
            else
            {
                Game = new GameModel("", e.Name.Split('_','.')[1], "", "", "Steam");
                Notify(e.ChangeType);
            }
        }

        public void SteamLibraryRetrieverThread()
        {
            foreach (string steamLibraryPath in _steamLibraryPaths)
            {
                List<FileInfo> gameFiles = new DirectoryInfo(steamLibraryPath).GetFiles("*.acf", SearchOption.TopDirectoryOnly).ToList();

                foreach (FileInfo fi in gameFiles)
                {
                    if (fi.Name != "appmanifest_228980.acf")
                    {
                        string name = null;
                        string id = null;
                        string size = null;
                        string path = fi.FullName.Substring(0, fi.FullName.IndexOf('\\'));

                        string[] fileText = File.ReadAllLines(fi.FullName);

                        foreach (string line in fileText)
                        {
                            if (line.Contains("\"name\""))
                            {
                                name = line.Split('\"')[3];
                            }
                            else if (line.Contains("\"appid\""))
                            {
                                id = line.Split('\"')[3];
                            }
                            else if (line.Contains("\"SizeOnDisk\""))
                            {
                                size = line.Split('\"')[3];
                            }
                        }
                        Game = new GameModel(name, id, size, path, "Steam");
                        Notify(WatcherChangeTypes.Created);
                    }
                }
            }
        }
    }
}
