using EtwManifestParsing;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;

namespace EtwExplorer.ViewModels {
	/// <summary>
	/// Represents a group of event versions with the same event number (Value)
	/// </summary>
	sealed class EventGroupViewModel : BindableBase {
		public int EventNumber { get; }
		public string EventName { get; }
		public ObservableCollection<EtwEventViewModel> Versions { get; }
		public bool HasMultipleVersions => Versions.Count > 1;
		public string VersionCount => HasMultipleVersions ? $"({Versions.Count} versions)" : string.Empty;

		EtwEventViewModel _selectedVersion1;
		public EtwEventViewModel SelectedVersion1 {
			get => _selectedVersion1;
			set {
				SetProperty(ref _selectedVersion1, value);
				RaisePropertyChanged(nameof(CanDiff));
			}
		}

		EtwEventViewModel _selectedVersion2;
		public EtwEventViewModel SelectedVersion2 {
			get => _selectedVersion2;
			set {
				SetProperty(ref _selectedVersion2, value);
				RaisePropertyChanged(nameof(CanDiff));
			}
		}

		public bool CanDiff => SelectedVersion1 != null && SelectedVersion2 != null && SelectedVersion1 != SelectedVersion2;

		public EventGroupViewModel(int eventNumber, EtwEventViewModel[] versions) {
			EventNumber = eventNumber;
			EventName = versions[0].Event.Symbol;
			Versions = new ObservableCollection<EtwEventViewModel>(versions);

			// Default selection for diff: first and last version
			if (Versions.Count >= 2) {
				SelectedVersion1 = Versions[0];
				SelectedVersion2 = Versions[Versions.Count - 1];
			}
		}
	}
}
