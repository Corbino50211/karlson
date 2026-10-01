using System.Collections.Generic;
using Momentum.Levels;

namespace Momentum.LevelEditor
{
    /// <summary>
    /// Snapshot-based undo/redo. Before every change the whole level is serialized; levels are small so
    /// this is cheap, simple and never gets out of sync with the scene.
    /// </summary>
    public class EditorHistory
    {
        readonly List<string> undo = new List<string>();
        readonly List<string> redo = new List<string>();
        readonly int capacity;

        public EditorHistory(int capacity = 100)
        {
            this.capacity = capacity;
        }

        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;
        public int UndoCount => undo.Count;

        public void Record(LevelData data)
        {
            undo.Add(data.ToJson(false));
            if (undo.Count > capacity) undo.RemoveAt(0);
            redo.Clear();
        }

        /// <summary>Returns the snapshot to restore; the current state goes onto the redo stack.</summary>
        public string Undo(string currentJson)
        {
            if (undo.Count == 0) return null;
            string snapshot = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            redo.Add(currentJson);
            return snapshot;
        }

        public string Redo(string currentJson)
        {
            if (redo.Count == 0) return null;
            string snapshot = redo[redo.Count - 1];
            redo.RemoveAt(redo.Count - 1);
            undo.Add(currentJson);
            return snapshot;
        }

        /// <summary>Drops the most recent undo entry (e.g. a drag that ended without changes).</summary>
        public void DiscardLast()
        {
            if (undo.Count > 0) undo.RemoveAt(undo.Count - 1);
        }

        public void Clear()
        {
            undo.Clear();
            redo.Clear();
        }
    }
}
