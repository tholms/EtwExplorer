using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using EtwManifestParsing;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace EtwExplorer.ViewModels {
	sealed class EventDiffTabViewModel : TabViewModelBase {
		public override string Icon => "/icons/compare.ico";
		public override string Header { get; }
		public override bool IsCloseable => true;

		public List<DiffLineViewModel> LeftLines { get; }
		public List<DiffLineViewModel> RightLines { get; }
		public string LeftVersionLabel { get; }
		public string RightVersionLabel { get; }

		public EventDiffTabViewModel(string eventName, EtwEventViewModel version1, EtwEventViewModel version2, EtwManifest manifest) {
			Header = $"Diff: {eventName}";
			LeftVersionLabel = $"Version {version1.Event.Version}";
			RightVersionLabel = $"Version {version2.Event.Version}";

			var leftXml = GenerateEventXml(version1, manifest);
			var rightXml = GenerateEventXml(version2, manifest);

			// Use DiffPlex to generate side-by-side diff
			var diffBuilder = new SideBySideDiffBuilder(new Differ());
			var diff = diffBuilder.BuildDiffModel(leftXml, rightXml);

			// Convert to view models for binding
			LeftLines = diff.OldText.Lines.Select(line => new DiffLineViewModel {
				Text = line.Text,
				Type = line.Type,
				Position = line.Position
			}).ToList();

			RightLines = diff.NewText.Lines.Select(line => new DiffLineViewModel {
				Text = line.Text,
				Type = line.Type,
				Position = line.Position
			}).ToList();
		}

		private string GenerateEventXml(EtwEventViewModel eventViewModel, EtwManifest manifest) {
			var sb = new StringBuilder();
			var evt = eventViewModel.Event;

			sb.AppendLine($"<!-- Event: {evt.Symbol} -->");
			sb.AppendLine($"<event");
			sb.AppendLine($"    value=\"{evt.Value}\"");
			sb.AppendLine($"    version=\"{evt.Version}\"");
			sb.AppendLine($"    symbol=\"{evt.Symbol}\"");
			if (!string.IsNullOrEmpty(evt.Level))
				sb.AppendLine($"    level=\"{evt.Level}\"");
			if (!string.IsNullOrEmpty(evt.Task))
				sb.AppendLine($"    task=\"{evt.Task}\"");
			if (!string.IsNullOrEmpty(evt.Opcode))
				sb.AppendLine($"    opcode=\"{evt.Opcode}\"");
			if (!string.IsNullOrEmpty(evt.Keyword))
				sb.AppendLine($"    keyword=\"{evt.Keyword}\"");
			if (!string.IsNullOrEmpty(evt.Template))
				sb.AppendLine($"    template=\"{evt.Template}\"");
			sb.AppendLine($"/>");

			// Add template details if available
			if (!string.IsNullOrEmpty(evt.Template) && manifest.Templates != null) {
				var template = manifest.Templates.FirstOrDefault(t => t.Id == evt.Template);
				if (template != null && template.Items != null && template.Items.Length > 0) {
					sb.AppendLine();
					sb.AppendLine($"<!-- Template: {template.Id} -->");
					sb.AppendLine($"<template tid=\"{template.Id}\">");
					foreach (var item in template.Items) {
						sb.AppendLine($"    <data name=\"{item.Name}\" inType=\"{item.Type}\" />");
					}
					sb.AppendLine($"</template>");
				}
			}

			return sb.ToString();
		}
	}

	public class DiffLineViewModel : BindableBase {
		public string Text { get; set; }
		public ChangeType Type { get; set; }
		public int? Position { get; set; }

		public string Background {
			get {
				switch (Type) {
					case ChangeType.Deleted:
						return "#FFFFE0E0"; // Light red
					case ChangeType.Inserted:
						return "#FFE0FFE0"; // Light green
					case ChangeType.Modified:
						return "#FFFFFFE0"; // Light yellow
					case ChangeType.Imaginary:
						return "#FFF0F0F0"; // Light gray
					default:
						return "Transparent";
				}
			}
		}
	}
}
