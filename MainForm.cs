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


// TODO target groups, pinned, recent, ...?

// Windows standard locations:  %PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs
// WinX/Start context menu:  %LOCALAPPDATA%\Microsoft\Windows\WinX\GroupX
// Plain files:
//   @"C:\Users\cepth\OneDrive\Tools\backup_loose.py",
//   @"C:\Users\cepth\OneDrive\Tools\Wavosaur.exe",
// Plain folders
//   @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup",
//   @"C:\Users\cepth\OneDrive\OneDriveDocuments",
// URLs:
//   @"https://www.bobrosslipsum.com/",
// Others (probably not usefule):
//   User Start menu => %APPDATA%\Microsoft\Windows\Start Menu\Programs\+subdirs...
//   Taskbar pinned => %APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar
//   Recent files => %APPDATA%\Microsoft\Windows\Recent and %APPDATA%\Microsoft\Office\Recent

namespace Trebuchet
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
            string appDir = MiscUtils.GetAppDataDir("Trebuchet", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => { Tell(e.Message); };
            LogManager.Run(logFileName, 50000);

            Text = $"Trebuchet {MiscUtils.GetVersionString()}";

            // Init selector configuration.
            var config = new Config()
            {
                AllowExternalSource = true,
                IndicatorColor = _settings.MarkerColor,
                EnableToolTip = true,
                Spacing = 10,
                Pad = 8,
                Mode = OpMode.Click,
                Style = SelectorStyle.Icon,
                NumColumns = _settings.NumColumns,
                ImageSize = new(_settings.ImageSize, _settings.ImageSize),
            };
            selector.Init(config);

            // Selector events.
            selector.Click += Selector_Click;
            selector.ContextMenuStrip = new();
            selector.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
            selector.ContextMenuStrip.ItemClicked += ContextMenuStrip_ItemClicked;

            // Init the data.
            _settings.Targets.ForEach(item => selector.AddResourceItem(item));

            // TODO Best way to start up? maybe setting?
            ShowInTaskbar = true;
            ShowIcon = true;
            WindowState = FormWindowState.Normal;
            //WindowState = FormWindowState.Minimized;
            FormBorderStyle = FormBorderStyle.FixedToolWindow; // SizableToolWindow
            Size = new(selector.GetTotalArea().Width + SystemInformation.VerticalScrollBarWidth, 600);
            StartPosition = FormStartPosition.Manual;
            Location =  new(Screen.PrimaryScreen!.Bounds.Width / 2 - Width / 2, Screen.PrimaryScreen!.Bounds.Height - Height - 50); // taskbar
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

        /// <summary>
        /// User wants to do something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ContextMenuStrip_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            selector.ContextMenuStrip!.Items.Clear();

            selector.ContextMenuStrip.Items.Add("Add File");
            selector.ContextMenuStrip.Items.Add("Add Folder");
            selector.ContextMenuStrip.Items.Add("Settings");

            if (selector.GetFocusedItem() != null)
            {
                selector.ContextMenuStrip.Items.Add("Remove");
            }
        }

        /// <summary>
        /// User wants to do something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ContextMenuStrip_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            selector.ContextMenuStrip!.Close();

            switch (e.ClickedItem!.Text)
            {
                case "Add File":
                case "Add Folder":
                    CommonOpenFileDialog dialog = new()
                    {
                        InitialDirectory = @"TODO from settings?",
                        IsFolderPicker = e.ClickedItem!.Text == "Add Folder"
                    };
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        selector.AddResourceItem(dialog.FileName);
                    }
                    break;

                case "Settings":
                    var changes = SettingsEditor.Edit(_settings, "User Settings", 450);
                    MessageBox.Show("Restart required for changes to take effect");
                    _settings.Save();
                    break;

                case "Remove":
                    var item = selector.GetFocusedItem();
                    if (item != null)
                    {
                        selector.RemoveItem(item);
                    }
                    break;
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
            Console.WriteLine(s);
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
