﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Codist.Options
{
	public partial class MiscPage : UserControl
	{
		readonly UiLock _UI = new UiLock();
		bool _Loaded;

		public MiscPage() {
			InitializeComponent();
		}
		internal MiscPage(ConfigPage page) : this() {

		}
		private void MiscPage_Load(object sender, EventArgs e) {
			if (_Loaded) {
				return;
			}
			_TopMarginBox.Value = (decimal)LineTransformers.LineHeightTransformProvider.TopSpace;
			_BottomMarginBox.Value = (decimal)LineTransformers.LineHeightTransformProvider.BottomSpace;
			LoadConfig(Config.Instance);

			_TopMarginBox.ValueChanged += _UI.HandleEvent(() => LineTransformers.LineHeightTransformProvider.TopSpace = (double)_TopMarginBox.Value);
			_BottomMarginBox.ValueChanged += _UI.HandleEvent(() => LineTransformers.LineHeightTransformProvider.BottomSpace = (double)_BottomMarginBox.Value);
			_NoSpaceBetweenWrappedLinesBox.CheckedChanged += _UI.HandleEvent(() => Config.Instance.NoSpaceBetweenWrappedLines = _NoSpaceBetweenWrappedLinesBox.Checked);
			_SaveConfigButton.Click += (s, args) => {
				using (var d = new SaveFileDialog {
					Title = "Save Codist configuration file...",
					FileName = "Codist.json",
					DefaultExt = "json",
					Filter = "Codist configuration file|*.json"
				}) {
					if (d.ShowDialog() != DialogResult.OK) {
						return;
					}
					Config.Instance.SaveConfig(d.FileName);
				}
			};
			_LoadConfigButton.Click += (s, args) => {
				using (var d = new OpenFileDialog {
					Title = "Load Codist configuration file...",
					FileName = "Codist.json",
					DefaultExt = "json",
					Filter = "Codist configuration file|*.json"
				}) {
					if (d.ShowDialog() != DialogResult.OK) {
						return;
					}
					try {
						Config.LoadConfig(d.FileName);
						System.IO.File.Copy(d.FileName, Config.ConfigPath, true);
					}
					catch (Exception ex) {
						MessageBox.Show("Error occured while loading config file: " + ex.Message, "Codist");
					}
				}
			};
			Config.ConfigUpdated += (s, args) => LoadConfig(s as Config);
			_Loaded = true;
		}

		void LoadConfig(Config config) {
			_UI.DoWithLock(() => {
				_NoSpaceBetweenWrappedLinesBox.Checked = config.NoSpaceBetweenWrappedLines;
			});
		}
	}
}
