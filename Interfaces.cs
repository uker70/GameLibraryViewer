using System.IO;
using GameLibraryViewer.Models;

namespace GameLibraryViewer
{
    public interface IObserver
    {
        // Receive update from subject
        void Update(GameModel newGame, WatcherChangeTypes changeType);
    }

    public interface IGame
    {
        // Attach an observer to the subject.
        void Attach(IObserver observer);

        // Detach an observer from the subject.
        void Detach(IObserver observer);

        // Notify all observers about an event.
        void Notify(WatcherChangeTypes changeType);
    }
}
