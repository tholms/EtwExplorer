using EtwManifestParsing;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace EtwExplorer.ViewModels {
	sealed class EventsTabViewModel : TabViewModelBase {
		public override string Icon => "/icons/events.ico";

		public override string Header => "Events";

		readonly EtwManifest _manifest;
		public event EventHandler<NavigateToTaskEventArgs> NavigateToTaskRequested;

		public EventsTabViewModel(EtwManifest manifest) {
			_manifest = manifest;
			Events = _manifest.Events
				.OrderBy(e => e.Value)
				.ThenBy(e => e.Version)
				.Select(e => new EventViewItemModel(e, _manifest))
				.ToArray();
		}

		public EventViewItemModel[] Events { get; }

		public void RaiseNavigateToTask(string taskName) {
			if (!string.IsNullOrEmpty(taskName)) {
				NavigateToTaskRequested?.Invoke(this, new NavigateToTaskEventArgs(taskName));
			}
		}

		string _searchText;
		public string SearchText {
			get => _searchText;
			set {
				if (SetProperty(ref _searchText, value)) {
					var cvs = CollectionViewSource.GetDefaultView(Events);
					if (string.IsNullOrWhiteSpace(_searchText))
						cvs.Filter = null;
					else {
						string text = _searchText.ToLower();
						cvs.Filter = obj => {
							var evt = (EventViewItemModel)obj;
							return evt.Symbol.ToLower().Contains(text) ||
								   evt.Task.ToLower().Contains(text) ||
								   evt.Opcode.ToLower().Contains(text);
						};
					}
				}
			}
		}
}

	public class NavigateToTaskEventArgs : EventArgs {
		public string TaskName { get; }

		public NavigateToTaskEventArgs(string taskName) {
			TaskName = taskName;
		}
	}
}
