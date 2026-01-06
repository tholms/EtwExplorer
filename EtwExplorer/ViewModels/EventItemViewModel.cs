using EtwManifestParsing;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;

namespace EtwExplorer.ViewModels {
	public sealed class EventItemViewModel : BindableBase {
		private readonly EtwManifest _manifest;
		public EtwEvent[] AllVersions { get; }

		public EventItemViewModel(EtwEvent[] versions, EtwManifest manifest) {
			AllVersions = versions;
			_manifest = manifest;

			// Display properties from first version (they should be similar across versions)
			var firstVersion = versions[0];
			Name = firstVersion.Symbol;
			Value = firstVersion.Value;
			Task = firstVersion.Task;
			Keyword = firstVersion.Keyword;
			Template = firstVersion.Template;
			Opcode = firstVersion.Opcode;
			Level = firstVersion.Level;

			// Version display
			if (versions.Length == 1) {
				Version = versions[0].Version.ToString();
			} else {
				Version = string.Join(", ", versions.Select(v => v.Version));
			}

			HasMultipleVersions = versions.Length > 1;

			// Build template details for each version
			VersionTemplates = new List<VersionTemplateViewModel>();
			foreach (var evt in versions) {
				var templateData = GetTemplateData(evt);
				VersionTemplates.Add(new VersionTemplateViewModel {
					Version = evt.Version,
					TemplateData = templateData
				});
			}

			// Default selection for diff
			if (versions.Length >= 2) {
				SelectedVersion1 = 0;
				SelectedVersion2 = versions.Length - 1;
			}
		}

		public string Name { get; }
		public int Value { get; }
		public string Version { get; }
		public string Task { get; }
		public string Keyword { get; }
		public string Template { get; }
		public string Opcode { get; }
		public string Level { get; }
		public bool HasMultipleVersions { get; }

		public List<VersionTemplateViewModel> VersionTemplates { get; }

		int? _selectedVersion1;
		public int? SelectedVersion1 {
			get => _selectedVersion1;
			set {
				SetProperty(ref _selectedVersion1, value);
				RaisePropertyChanged(nameof(CanDiff));
			}
		}

		int? _selectedVersion2;
		public int? SelectedVersion2 {
			get => _selectedVersion2;
			set {
				SetProperty(ref _selectedVersion2, value);
				RaisePropertyChanged(nameof(CanDiff));
			}
		}

		public bool CanDiff => HasMultipleVersions && SelectedVersion1.HasValue && SelectedVersion2.HasValue && SelectedVersion1 != SelectedVersion2;

		private EtwTemplateData[] GetTemplateData(EtwEvent evt) {
			if (string.IsNullOrEmpty(evt.Template) || _manifest.Templates == null)
				return new EtwTemplateData[0];

			var template = _manifest.Templates.FirstOrDefault(t => t.Id == evt.Template);
			return template?.Items ?? new EtwTemplateData[0];
		}
	}

	public class VersionTemplateViewModel {
		public int Version { get; set; }
		public EtwTemplateData[] TemplateData { get; set; }
	}
}
