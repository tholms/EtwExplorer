using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using EtwManifestParsing;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EtwExplorer.ViewModels {
	sealed class TemplateDiffTabViewModel : TabViewModelBase {
		public override string Icon => "/icons/compare.ico";
		public override string Header { get; }
		public override bool IsCloseable => true;

		public List<DiffLineViewModel> LeftLines { get; }
		public List<DiffLineViewModel> RightLines { get; }
		public string LeftVersionLabel { get; }
		public string RightVersionLabel { get; }

		public TemplateDiffTabViewModel(string taskName, EtwTemplate template1, EtwTemplate template2, EtwManifest manifest) {
			Header = $"Diff: {taskName}";
			LeftVersionLabel = template1.Id;
			RightVersionLabel = template2.Id;

			var leftXml = GenerateTemplateXml(template1);
			var rightXml = GenerateTemplateXml(template2);

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

		private string GenerateTemplateXml(EtwTemplate template) {
			var sb = new StringBuilder();

			sb.AppendLine($"<!-- Template: {template.Id} -->");
			sb.AppendLine($"<template tid=\"{template.Id}\">");

			if (template.Items != null && template.Items.Length > 0) {
				foreach (var item in template.Items) {
					sb.AppendLine($"    <data name=\"{item.Name}\" inType=\"{item.Type}\" />");
				}
			}

			sb.AppendLine($"</template>");

			return sb.ToString();
		}
	}
}
