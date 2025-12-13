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

        public void UpdateResults(List<PopupResult> newResults)
        {
            Results = newResults;
        }
    }
    
    // Simple base class required by Avalonia/ReactiveUI template convention
    public class ViewModelBase : ReactiveObject { }
}