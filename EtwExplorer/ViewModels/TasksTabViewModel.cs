using EtwManifestParsing;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace EtwExplorer.ViewModels {
	sealed class TasksTabViewModel : TabViewModelBase {
		public override string Icon => "/icons/tasks.ico";

		public override string Header => "Tasks";

		readonly EtwManifest _manifest;
		public event EventHandler<TemplateDiffRequestedEventArgs> DiffRequested;

		public TasksTabViewModel(EtwManifest manifest) {
			_manifest = manifest;

			// Group templates by task
			var taskGroups = new List<TaskGroupViewModel>();

			if (manifest.Templates != null) {
				// Get all events grouped by task
				var eventsByTask = manifest.Events
					.Where(e => !string.IsNullOrEmpty(e.Task))
					.GroupBy(e => e.Task)
					.OrderBy(g => g.Key);

				foreach (var taskGroup in eventsByTask) {
					// Get all unique templates used by events in this task
					var templateIds = taskGroup
						.Where(e => !string.IsNullOrEmpty(e.Template))
						.Select(e => e.Template)
						.Distinct()
						.ToArray();

					if (templateIds.Length > 0) {
						var templates = templateIds
							.Select(tid => manifest.Templates.FirstOrDefault(t => t.Id == tid))
							.Where(t => t != null)
							.ToArray();

						if (templates.Length > 0) {
							taskGroups.Add(new TaskGroupViewModel(taskGroup.Key, templates, _manifest));
						}
					}
				}
			}

			TaskGroups = taskGroups.ToArray();
			DiffCommand = new DelegateCommand<TaskGroupViewModel>(OnDiff, item => item?.CanDiff ?? false);
		}

		public TaskGroupViewModel[] TaskGroups { get; }

		public ICommand DiffCommand { get; }

		TaskGroupViewModel _selectedTaskGroup;
		public TaskGroupViewModel SelectedTaskGroup {
			get => _selectedTaskGroup;
			set => SetProperty(ref _selectedTaskGroup, value);
		}

		public void SelectTask(string taskName) {
			var task = TaskGroups.FirstOrDefault(t => t.TaskName == taskName);
			if (task != null) {
				SelectedTaskGroup = task;
			}
		}

		string _searchText;
		public string SearchText {
			get => _searchText;
			set {
				if (SetProperty(ref _searchText, value)) {
					var cvs = CollectionViewSource.GetDefaultView(TaskGroups);
					if (string.IsNullOrWhiteSpace(_searchText))
						cvs.Filter = null;
					else {
						string text = _searchText.ToLower();
						cvs.Filter = obj => {
							var item = (TaskGroupViewModel)obj;
							return item.TaskName.ToLower().Contains(text);
						};
					}
				}
			}
		}

		private void OnDiff(TaskGroupViewModel item) {
			if (item?.SelectedTemplate1 != null && item?.SelectedTemplate2 != null) {
				DiffRequested?.Invoke(this, new TemplateDiffRequestedEventArgs(
					item.TaskName,
					item.DisplayTemplates[item.SelectedTemplate1.Value].Template,
					item.DisplayTemplates[item.SelectedTemplate2.Value].Template,
					_manifest
				));
			}
		}
	}

	public class TemplateDisplayItem {
		public EtwTemplate Template { get; set; }
		public string DisplayName { get; set; }
	}

	public class TaskGroupViewModel : BindableBase {
		private readonly EtwManifest _manifest;

		public string TaskName { get; }
		public EtwTemplate[] AllTemplates { get; }
		public List<TemplateDisplayItem> DisplayTemplates { get; }
		public List<TemplateDetailsViewModel> TemplateDetails { get; }

		public bool HasMultipleTemplates { get; }

		int? _selectedTemplate1;
		public int? SelectedTemplate1 {
			get => _selectedTemplate1;
			set {
				SetProperty(ref _selectedTemplate1, value);
				RaisePropertyChanged(nameof(CanDiff));
			}
		}

		int? _selectedTemplate2;
		public int? SelectedTemplate2 {
			get => _selectedTemplate2;
			set {
				SetProperty(ref _selectedTemplate2, value);
				RaisePropertyChanged(nameof(CanDiff));
			}
		}

		public bool CanDiff => HasMultipleTemplates && SelectedTemplate1.HasValue && SelectedTemplate2.HasValue && SelectedTemplate1 != SelectedTemplate2;

		public TaskGroupViewModel(string taskName, EtwTemplate[] templates, EtwManifest manifest) {
			TaskName = taskName;
			AllTemplates = templates;
			_manifest = manifest;
			HasMultipleTemplates = templates.Length > 1;

			// Build template details for display
			TemplateDetails = new List<TemplateDetailsViewModel>();
			foreach (var template in templates) {
				TemplateDetails.Add(new TemplateDetailsViewModel {
					TemplateName = template.Id,
					TemplateData = template.Items ?? new EtwTemplateData[0]
				});
			}

			// Build shortened display names for dropdowns
			DisplayTemplates = CreateShortenedDisplayNames(templates);

			// Default selection for diff
			if (templates.Length >= 2) {
				SelectedTemplate1 = 0;
				SelectedTemplate2 = templates.Length - 1;
			}
		}

		private List<TemplateDisplayItem> CreateShortenedDisplayNames(EtwTemplate[] templates) {
			var result = new List<TemplateDisplayItem>();

			// Find the common prefix ending before "Args"
			var names = templates.Select(t => t.Id).ToArray();

			// Start with just the suffix after the last occurrence before "Args"
			var shortNames = new List<string>();
			foreach (var name in names) {
				// Find "Args" and extract the suffix
				var argsIndex = name.LastIndexOf("Args");
				if (argsIndex > 0) {
					// Get everything from "Args" onwards
					shortNames.Add(name.Substring(argsIndex));
				} else {
					shortNames.Add(name);
				}
			}

			// Check for duplicates and expand backwards if needed
			int charsToInclude = 0;
			while (shortNames.Distinct().Count() < shortNames.Count && charsToInclude < 100) {
				charsToInclude++;
				shortNames.Clear();

				foreach (var name in names) {
					var argsIndex = name.LastIndexOf("Args");
					if (argsIndex > 0) {
						// Include more characters before "Args"
						var startIndex = Math.Max(0, argsIndex - charsToInclude);
						shortNames.Add(name.Substring(startIndex));
					} else {
						shortNames.Add(name);
					}
				}
			}

			// Build the result list
			for (int i = 0; i < templates.Length; i++) {
				result.Add(new TemplateDisplayItem {
					Template = templates[i],
					DisplayName = shortNames[i]
				});
			}

			return result;
		}

		public string TemplateCount => HasMultipleTemplates ? $"{AllTemplates.Length} templates" : "1 template";
	}

	public class TemplateDetailsViewModel {
		public string TemplateName { get; set; }
		public EtwTemplateData[] TemplateData { get; set; }
	}

	public class TemplateDiffRequestedEventArgs : EventArgs {
		public string TaskName { get; }
		public EtwTemplate Template1 { get; }
		public EtwTemplate Template2 { get; }
		public EtwManifest Manifest { get; }

		public TemplateDiffRequestedEventArgs(string taskName, EtwTemplate template1, EtwTemplate template2, EtwManifest manifest) {
			TaskName = taskName;
			Template1 = template1;
			Template2 = template2;
			Manifest = manifest;
		}
	}
}
