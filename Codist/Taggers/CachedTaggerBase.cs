﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Codist.Taggers
{
	abstract class CachedTaggerBase : ITagger<IClassificationTag>
	{
		readonly ITextView _TextView;
		readonly TaggerResult _Tags;

		protected CachedTaggerBase(ITextView textView) {
			_TextView = textView;
			_Tags = textView.Properties.GetOrCreateSingletonProperty(() => new TaggerResult());
		}

		protected ITextView TextView => _TextView;
		public TaggerResult Result => _Tags;
		protected abstract bool DoFullParseAtFirstLoad { get; }

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0) {
				yield break;
			}
			IEnumerable<SnapshotSpan> parseSpans = spans;

			if (_Tags.LastParsed == 0 && DoFullParseAtFirstLoad) {
				var textSnapshot = _TextView.TextSnapshot;
				// perform a full parse for the first time
				System.Diagnostics.Debug.WriteLine("Full parse");
				parseSpans = textSnapshot.Lines.Select(l => l.Extent);
				_Tags.LastParsed = textSnapshot.Length;
			}
			foreach (var span in parseSpans) {
				var r = Parse(span);
				if (r != null) {
					yield return _Tags.Add(r);
				}
			}
		}

		protected abstract TaggedContentSpan Parse(SnapshotSpan span);
	}
}
