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
    public class OriginLibraryRetriever : IGame
    {
        private bool _registryFound;
        private string _originInstallPath;
        private GameModel _game;
        private List<IObserver> _observers = new List<IObserver>();
        private string _originLibraryPath;
        private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

        public bool RegistryFound
        {
            get { return _registryFound; }
        }

        public string OriginInstallPath
        {
            get { return _originInstallPath; }
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

        public OriginLibraryRetriever()
        {
            _registryFound = false;
            try
            {
                if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node").GetSubKeyNames().Contains("Origin"))
                {
                    _originInstallPath = Registry.LocalMachine
                        .OpenSubKey(@"SOFTWARE\Wow6432Node\Origin")
                        .GetValue("OriginPath").ToString();
                    _registryFound = true;
                }
            }
            catch
            { }

            if (_registryFound)
            {
                _originLibraryPath = @"C:\ProgramData\Origin\LocalContent";

                foreach (DirectoryInfo subDirectory in new DirectoryInfo(_originLibraryPath).GetDirectories())
                {
                    _watchers.Add(new FileSystemWatcher(subDirectory.FullName, "*.ddc"));
                }

                foreach (FileSystemWatcher watcher in _watchers)
                {
                    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                    watcher.Created += OriginLibraryEvent;
                    watcher.Changed += OriginLibraryEvent;
                    watcher.Deleted += OriginLibraryEvent;
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        private void OriginLibraryEvent(object source, FileSystemEventArgs e)
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
                    if (line.Contains("\"productId\""))
                    {
                        id = line.Split('\"')[3];
                    }
                    else if (line.Contains("\"bytesTotal\""))
                    {
                        try
                        {
                            size = (Convert.ToByte(size) + Convert.ToByte(line.Split('\"')[3])).ToString();
                        }
                        catch
                        { }
                    }
                    else if (line.Contains("\"packageRootPath\""))
                    {
                        path = line.Split('\"')[3].Substring(0, 2);
                        name = line.Split('\\')[6];
                    }
                }
                Game = new GameModel(name, id, size, path, "Origin");
                Notify(e.ChangeType);
            }
            else
            {
                Game = new GameModel("", e.Name.Split('.')[0], "", "", "Origin");
                Notify(e.ChangeType);
            }
        }

        public void OriginLibraryRetrieverThread()
        {
            List<FileInfo> gameFiles = new DirectoryInfo(_originLibraryPath).GetFiles("*.ddc", SearchOption.AllDirectories).ToList();

            foreach (FileInfo fi in gameFiles)
            {
                string name = null;
                string id = null;
                string size = null;
                string path = null;

                string[] fileText = File.ReadAllLines(fi.FullName);

                foreach (string line in fileText)
                {
                    if (line.Contains("\"productId\""))
                    {
                        id = line.Split('\"')[3];
                    }
                    else if (line.Contains("\"bytesTotal\""))
                    {
                        try
                        {
                            size = (Convert.ToInt64(size) + Convert.ToInt64(line.Split('\"')[3])).ToString();
                        }
                        catch
                        { }
                    }
                    else if (line.Contains("\"packageRootPath\""))
                    {
                        path = line.Split('\"')[3].Substring(0, 2);
                        name = line.Split('\\')[6];
                    }
                }
                Game = new GameModel(name, id, size, path, "Origin");
                Notify(WatcherChangeTypes.Created);
            }
        }
    }
}
