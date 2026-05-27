using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Dialogs;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.IconicSelector;


// TODO2 target groups, pinned?

namespace WinStart
{
    /// <summary>
    /// The application.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args"></param>
        public MainForm(string[] args)
        {
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Load settings first before initializing.
            string appDir = MiscUtils.GetAppDataDir("WinStart", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => { Tell(e.Message); };
            LogManager.Run(logFileName, 50000);

            Text = $"WinStart {MiscUtils.GetVersionString()}";

            // Default - big X.
            Bitmap defbmp = new(32, 32);
            using Graphics gr = Graphics.FromImage(defbmp);
            gr.Clear(Color.LightSalmon);
            gr.DrawString($"????", Font, Brushes.Black, 2, 2);

            // Init selector configuration.
            var config = new Config()
            {
                AllowExternalSource = true,
                IndicatorColor = _settings.MarkerColor,
                Spacing = 10,
                Pad = 8,
                Mode = OpMode.Click,
                Style = SelectorStyle.Icon,
                NumColumns = _settings.NumColumns,
                ImageSize = new(_settings.ImageSize, _settings.ImageSize),
            };
            selector.Init(config);

            // Hook selector events.
            selector.Click += Selector_Click;

            // Selector menu.
            selector.ContextMenuStrip = new();
            selector.ContextMenuStrip.Items.Add("Add File");
            selector.ContextMenuStrip.Items.Add("Add Folder");
            selector.ContextMenuStrip.Items.Add("Paste");
            selector.ContextMenuStrip.Items.Add("Remove");
            selector.ContextMenuStrip.ItemClicked += Menu_ItemClicked;

            // Init the data.
            _settings.Targets.ForEach(item => selector.AddResourceItem(item));
            // Debug.DoDummy().ForEach(item => selector.AddResourceItem(item));

            // Size and location. TODO1 probably user option? always/popup/?
            FormBorderStyle = FormBorderStyle.SizableToolWindow;// FixedToolWindow;
            StartPosition = FormStartPosition.Manual;
            Size = new(selector.GetTotalArea().Width + SystemInformation.VerticalScrollBarWidth, 600);
            WindowState = FormWindowState.Normal;
            //WindowState = FormWindowState.Minimized;
            var pos = Cursor.Position;
            Location = new Point(200, 200);
        }

        /// <summary>
        /// User wants to do something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            selector.ContextMenuStrip!.Close();

            switch (e.ClickedItem!.Text) // TODO1 which? put in lib?
            {
                case "Add File":
                case "Add Folder":
                    CommonOpenFileDialog dialog = new()
                    {
                        InitialDirectory = @"%APPDATA%\Microsoft\Windows\Start Menu\Programs", // TODO1 from settings?
                        IsFolderPicker = e.ClickedItem!.Text == "Add Folder"
                    };
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        selector.AddResourceItem(dialog.FileName);
                    }
                    break;

                case "Paste":
                    selector.AddResourceItem(Clipboard.GetText());
                    break;

                case "Remove":
                    var sels = selector.GetSelectedItems();
                    sels.ForEach(sel => selector.RemoveItem(sel));
                    break;
            }
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();

            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };

            _settings.Targets.Clear();
            selector.GetAllItems().ForEach(it => _settings.Targets.Add(it.Value.ToString()! ));

            _settings.Save();

            base.OnFormClosing(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                selector?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Selector interaction
        /// <summary>
        /// User clicked something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_Click(object? sender, ClickEventArgs e)
        {
            if (e.ClickedItem is not null)
            {
                // Item click.
                string fn;
                List<string> args;
                //_logger.Info($"Selection -> [{e.ClickedItem.Caption}] [{e.ClickedItem.Value}]");

                switch (e.ClickedItem.DataType)
                {
                    case ItemDataType.Dir:
                        fn = "explorer";
                        args = [e.ClickedItem.Value.ToString()!];
                        break;

                    case ItemDataType.Url:
                        fn = e.ClickedItem.Value.ToString()!;
                        args = [];
                        break;

                    default:
                        fn = "cmd";
                        args = ["/C", e.ClickedItem.Value.ToString()!];
                        break;
                }

                ProcessStartInfo pinfo = new(fn, args)
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                try
                {
                    using Process proc = new() { StartInfo = pinfo };
                    proc.Start();
                    proc.WaitForExit();
                    int code = proc.ExitCode;

                    if (code != 0)
                    {
                        _logger.Error($"Start process code:{code}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Execute failed [{ex.Message}]");
                }
            }
        }
        #endregion

        #region Privates
        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="s"></param>
        void Tell(string s)
        {
            // TODO1 ?? this.InvokeIfRequired(_ => { _log.Add(s + Environment.NewLine); });
        }

        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 450);
            MessageBox.Show("Restart required for changes to take effect");
            _settings.Save();
        }

        /// <summary>
        /// Build the list of recent items.
        /// </summary>
        List<string> GetRecents()
        {
            List<string> recents = [];

            List<string> filters = ["bat", "cmd", "config", "css", "csv", "json", "log", "md", "txt", "xml"];

            DirectoryInfo diRecent = new(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
            // Key is target, value is shortcut.
            Dictionary<FileInfo, FileInfo> finfos = [];
            foreach (var f in filters)
            {
                // Get the links.
                foreach (var fs in diRecent.GetFiles($"*.{f}.lnk"))
                {
                    var sl = ShellObject.FromParsingName(fs.FullName);
                    var ft = ((ShellLink)sl).TargetLocation;
                    var fi1 = new FileInfo(ft);
                    finfos.Add(fi1, fs);
                }
            }

            // Most recent first.
            finfos.OrderBy(key => key.Key.LastAccessTime).
                Reverse().
                ForEach(fi => recents.Add(fi.Key.FullName));

            return recents;
        }
        #endregion
    }
}
