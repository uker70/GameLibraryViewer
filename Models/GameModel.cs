

namespace GameLibraryViewer.Models
{
    public class GameModel
    {
        public GameModel(string name, string id, string size, string path, string launcher)
        {
            this.id = id;
            this.name = name;
            this.size = size;
            this.path = path;
            this.launcher = launcher;
        }

        private string name;
        public string Name
        {
            get { return name; }
        }

        private string size;
        public string Size
        {
            get { return size; }
            set { size = value; }
        }

        private string id;
        public string Id
        {
            get { return id; }
        }

        private string launcher;
        public string Launcher
        {
            get { return launcher; }
        }

        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; }
        }
    }
}
