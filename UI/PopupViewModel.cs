using ReactiveUI;
using System.Collections.Generic;
using System.Linq;

namespace Emoki.UI
{
    public class PopupViewModel : ViewModelBase
    {
        private List<PopupResult> _results = new List<PopupResult>();
        public List<PopupResult> Results
        {
            get => _results;
            private set => this.RaiseAndSetIfChanged(ref _results, value);
        }

        // Index of the currently highlighted/selected item in the UI list
        private int _selectedIndex = 0;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
        }

        // Returns the currently highlighted result or null when out of range
        public PopupResult? SelectedResult =>
            (Results != null && SelectedIndex >= 0 && SelectedIndex < Results.Count)
            ? Results[SelectedIndex]
            : null;

        // Replace results and reset selection to the first item
        public void UpdateResults(List<PopupResult> newResults)
        {
            Results = newResults;
            SelectedIndex = 0;
        }
    }
    
    // Simple base class required by Avalonia/ReactiveUI template convention
    public class ViewModelBase : ReactiveObject { }
}