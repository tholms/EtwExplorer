using EtwManifestParsing;
using System.Linq;

namespace EtwExplorer.ViewModels {
	public class EventViewItemModel {
		private readonly EtwEvent _event;
		private readonly EtwManifest _manifest;

		public EventViewItemModel(EtwEvent evt, EtwManifest manifest) {
			_event = evt;
			_manifest = manifest;
		}

		// Expose EtwEvent properties
		public string Symbol => _event.Symbol;
		public int Value => _event.Value;
		public int Version => _event.Version;
		public string Task => _event.Task;
		public string Keyword => _event.Keyword;
		public string Template => _event.Template;
		public string Opcode => _event.Opcode;
		public string Level => _event.Level;

		// Expose the underlying event for context menu/commands
		public EtwEvent Event => _event;

		// Get template data for row details
		public EtwTemplateData[] TemplateData {
			get {
				if (string.IsNullOrEmpty(_event.Template) || _manifest.Templates == null)
					return new EtwTemplateData[0];

				var template = _manifest.Templates.FirstOrDefault(t => t.Id == _event.Template);
				return template?.Items ?? new EtwTemplateData[0];
			}
		}
	}
}
