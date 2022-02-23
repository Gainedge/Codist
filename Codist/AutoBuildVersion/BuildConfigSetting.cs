﻿using System;
using System.Text.RegularExpressions;
using AppHelpers;
using EnvDTE;
using Microsoft.VisualStudio;
using Newtonsoft.Json;

namespace Codist.AutoBuildVersion
{
	public sealed class BuildConfigSetting
	{
		const string ProjectAssemblyVersionName = "AssemblyVersion";
		const string ProjectFileVersionName = "AssemblyFileVersion";
		const string SdkProjectFileVersionName = "FileVersion";
		const string SdkCopyrightName = "Copyright";

		public VersionSetting AssemblyVersion { get; set; }
		public VersionSetting AssemblyFileVersion { get; set; }
		public bool UpdateLastYearNumberInCopyright { get; set; }

		[JsonIgnore]
		public bool ShouldRewrite => ShouldSerializeAssemblyVersion() || ShouldSerializeAssemblyFileVersion() || UpdateLastYearNumberInCopyright;

		public bool ShouldSerializeAssemblyVersion() {
			return AssemblyVersion?.ShouldRewrite == true;
		}
		public bool ShouldSerializeAssemblyFileVersion() {
			return AssemblyFileVersion?.ShouldRewrite == true;
		}

		public void RewriteVersion(Project project) {
			var f = RewriteFlags.None
				.SetFlags(RewriteFlags.AssemblyVersion, AssemblyVersion?.ShouldRewrite == true)
				.SetFlags(RewriteFlags.AssemblyFileVersion, AssemblyFileVersion?.ShouldRewrite == true)
				.SetFlags(RewriteFlags.CopyrightYear, UpdateLastYearNumberInCopyright);
			RewriteSdkProjectFile(project, ref f);
			//var r = RewriteSdkProjectFile(project, ref f)
			//	|| RewriteAssemblyInfoFile(project, project.FindItem("Properties", "AssemblyInfo.cs"), ref f);
		}

		public static bool TryGetVersions(Project project, out string[] version, out string[] fileVersion, out string copyright) {
			version = AttributePattern.GetMatchedVersion(project, ProjectAssemblyVersionName);
			fileVersion = AttributePattern.GetMatchedVersion(project, ProjectFileVersionName)
				?? AttributePattern.GetMatchedVersion(project, SdkProjectFileVersionName);
			copyright = AttributePattern.GetProperty(project, SdkCopyrightName);
			if (version != null && fileVersion != null) {
				return true;
			}
			var assemblyInfo = project.FindItem("Properties", "AssemblyInfo.cs");
			if (assemblyInfo != null) {
				bool isOpen = assemblyInfo.IsOpen;
				if (isOpen == false) {
					assemblyInfo.Open();
				}
				var doc = assemblyInfo.Document;
				if (doc.Selection is TextSelection s) {
					s.SelectAll();
					var t = s.Text;
					if (version is null) {
						version = AttributePattern.GetMatchedVersion(AttributePattern.AssemblyVersion.Match(t));
					}
					if (fileVersion is null) {
						fileVersion = AttributePattern.GetMatchedVersion(AttributePattern.AssemblyFileVersion.Match(t));
					}
				}
				if (isOpen == false) {
					doc.Close(vsSaveChanges.vsSaveChangesNo);
				}
			}
			return version != null || fileVersion != null;
		}

		bool RewriteAssemblyInfoFile(Project project, ProjectItem assemblyInfo, ref RewriteFlags f) {
			if (assemblyInfo is null || f == RewriteFlags.None) {
				return false;
			}
			bool isOpen = assemblyInfo.IsOpen;
			if (isOpen == false) {
				assemblyInfo.Open();
			}
			var doc = assemblyInfo.Document;
			var rf = RewriteFlags.None;
			if (f.MatchFlags(RewriteFlags.AssemblyVersion) && RewriteVersion(AssemblyVersion, AttributePattern.AssemblyVersion, project, doc)) {
				rf = rf.SetFlags(RewriteFlags.AssemblyVersion, true);
			}
			if (f.MatchFlags(RewriteFlags.AssemblyFileVersion) && RewriteVersion(AssemblyFileVersion, AttributePattern.AssemblyFileVersion, project, doc)) {
				rf = rf.SetFlags(RewriteFlags.AssemblyFileVersion, true);
			}
			if (f.MatchFlags(RewriteFlags.CopyrightYear) && RewriteCopyrightYear(AttributePattern.AssemblyCopyrightYear, project, doc)) {
				rf = rf.SetFlags(RewriteFlags.CopyrightYear, true);
			}
			f = f.SetFlags(rf, false);
			var r = rf != RewriteFlags.None;
			if (r && doc.Saved == false) {
				doc.Save();
			}
			if (isOpen == false) {
				doc.Close(vsSaveChanges.vsSaveChangesNo);
			}
			return r;
		}

		bool RewriteVersion(VersionSetting setting, Regex regex, Project project, Document docAssemblyInfo) {
			if (setting == null || !(docAssemblyInfo.Selection is TextSelection s)) {
				return false;
			}
			s.SelectAll();
			var m = regex.Match(s.Text.Replace("\r\n", "\n"));
			if (m.Success) {
				var g = m.Groups;
				var n = $"{g[1].Value}{setting.Rewrite(g[2].Value, g[3].Value, g[4].Value, g[5].Value)}{g[6].Value}";
				if (g[0].Value != null) {
					WriteBuildOutput(project.Name + " => " + n);
					s.MoveToAbsoluteOffset(m.Index + 1);
					docAssemblyInfo.ReplaceText(g[0].Value, n);
				}
				return true;
			}
			return false;
		}

		bool RewriteCopyrightYear(Regex regex, Project project, Document docAssemblyInfo) {
			if (!(docAssemblyInfo.Selection is TextSelection s)) {
				return false;
			}
			s.SelectAll();
			var m = regex.Match(s.Text.Replace("\r\n", "\n"));
			if (m.Success) {
				var g = m.Groups;
				var y = Int32.Parse(g[2].Value);
				if (y != DateTime.Now.Year) {
					var n = $"{g[1].Value}{DateTime.Now.Year.ToText()}{g[3].Value}";
					if (g[0].Value != null) {
						WriteBuildOutput(project.Name + " => " + n);
						s.MoveToAbsoluteOffset(m.Index + 1);
						docAssemblyInfo.ReplaceText(g[0].Value, n);
					}
					return true;
				}
			}
			return false;
		}

		bool RewriteSdkProjectFile(Project project, ref RewriteFlags f) {
			var rf = RewriteFlags.None;
			if (f.MatchFlags(RewriteFlags.AssemblyVersion) && RewriteVersion(project, AssemblyVersion, ProjectAssemblyVersionName)) {
				rf = rf.SetFlags(RewriteFlags.AssemblyVersion, true);
			}
			if (f.MatchFlags(RewriteFlags.AssemblyFileVersion) && (RewriteVersion(project, AssemblyFileVersion, SdkProjectFileVersionName) || RewriteVersion(project, AssemblyFileVersion, ProjectFileVersionName))) {
				rf = rf.SetFlags(RewriteFlags.AssemblyFileVersion, true);
			}
			if (f.MatchFlags(RewriteFlags.CopyrightYear) && RewriteCopyrightYear(project, SdkCopyrightName)) {
				rf = rf.SetFlags(RewriteFlags.CopyrightYear, true);
			}
			f = f.SetFlags(rf, false);
			var r = rf != RewriteFlags.None;
			if (r && project.Saved == false) {
				project.Save();
			}
			return r;
		}

		bool RewriteVersion(Project project, VersionSetting setting, string versionName) {
			Property ver;
			if (setting is null) {
				return false;
			}
			try {
				if ((ver = project.Properties.Item(versionName)) == null) {
					return false;
				}
			}
			catch (ArgumentException) {
				return false;
			}
			var t = ver.Value.ToString();
			if (Version.TryParse(t, out var v)) {
				var n = setting.Rewrite(v.Major.ToText(), v.Minor.ToText(), v.Build.ToText(), v.Revision.ToText());
				if (n != t) {
					ver.Value = n;
					WriteBuildOutput($"{project.Name} {versionName} => {n}");
				}
				return true;
			}
			return false;
		}

		bool RewriteCopyrightYear(Project project, string propertyName) {
			Property property;
			try {
				if ((property = project.Properties.Item(propertyName)) == null) {
					return false;
				}
			}
			catch (ArgumentException) {
				return false;
			}
			var t = property.Value.ToString();
			int i = t.Length;
			while ((i = t.LastIndexOf("20", i, StringComparison.Ordinal)) >= 0) {
				if (i + 4 <= t.Length && IsDigit(t[i + 2]) && IsDigit(t[i + 3])
					&& (i == 0 || IsDigit(t[i - 1]) == false)
					&& (i + 5 > t.Length || IsDigit(t[i + 4]) == false)) {
					var n = DateTime.Now.Year.ToText();
					if (t.Substring(i, 4) != n) {
						property.Value = t = t.Substring(0, i) + n + t.Substring(i + 4);
						WriteBuildOutput($"{project.Name} {propertyName} => {t}");
						return true;
					}
					else {
						return false;
					}
				}
			}
			return false;
		}

		static bool IsDigit(char c) {
			return c >= '0' && c <= '9';
		}

		void WriteBuildOutput(string text) {
			CodistPackage.Instance?.GetOutputPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, "Build")?.OutputString(nameof(Codist) + ": " + text + Environment.NewLine);
		}

		[Flags]
		enum RewriteFlags
		{
			None,
			AssemblyVersion,
			AssemblyFileVersion,
			CopyrightYear
		}

		static class AttributePattern
		{
			const string Pattern = @"(^\s*\[\s*assembly\s*:\s*AssemblyVersion(?:Attribute)?\s*\(\s*"")(\d+)\.(\d+)\.(\d+)\.(\d+)(\s*""\s*\)\s*\])";
			const string CopyrightYearPattern = @"(^\s*\[\s*assembly\s*:\s*AssemblyCopyright(?:Attribute)?\s*\(\s*"".*)(20\d\d)(.*?\s*""\s*\)\s*\])";
			internal static readonly Regex AssemblyVersion = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.Multiline);
			internal static readonly Regex AssemblyFileVersion = new Regex(Pattern.Replace("AssemblyVersion", "AssemblyFileVersion"), RegexOptions.Compiled | RegexOptions.Multiline);
			internal static readonly Regex AssemblyCopyrightYear = new Regex(CopyrightYearPattern, RegexOptions.Compiled | RegexOptions.Multiline);

			public static string[] GetMatchedVersion(Version v) {
				return new[] { v.Major.ToText(), v.Minor.ToText(), v.Build.ToText(), v.Revision.ToText() };
			}
			public static string[] GetMatchedVersion(Project project, string propertyName) {
				try {
					var ver = project.Properties.Item(propertyName);
					return ver != null && Version.TryParse(ver.Value.ToString(), out var v)
						? GetMatchedVersion(v)
						: null;
				}
				catch (ArgumentException) {
					return null;
				}
			}
			public static string GetProperty(Project project, string propertyName) {
				try {
					var p = project.Properties.Item(propertyName);
					return p?.Value is string s ? s : null;
				}
				catch (ArgumentException) {
					return null;
				}
			}
			public static string[] GetMatchedVersion(Match match) {
				if (match.Success == false) {
					return null;
				}
				var g = match.Groups;
				return new[] { g[2].Value, g[3].Value, g[4].Value, g[5].Value };
			}
		}
	}
}
