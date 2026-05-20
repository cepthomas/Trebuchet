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


// TODO1 recent files? pin to start?

// https://github.com/oozcitak/imagelistview

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

        /// <summary>Folder.</summary>
        readonly Bitmap _folderImage;

        /// <summary>URL.</summary>
        readonly Bitmap _urlImage;

        /// <summary>Default if not available.</summary>
        readonly Bitmap _defaultImage;
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
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 50000);

            // Main form.
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;

            //WindowState = FormWindowState.Minimized;
            StartPosition = FormStartPosition.Manual;
            var pos = Cursor.Position;
            Location = new Point(200, 200);

            Text = $"WinStart {MiscUtils.GetVersionString()}";

            // Init selector properties.
            selector.ImageSize = new(_settings.ImageSize, _settings.ImageSize);
            selector.IndicatorColor = _settings.MarkerColor;
            selector.DrawFont = _settings.TileFont;
            selector.Style = SelectorStyle.Icon;
            selector.Mode = OpMode.Click;
            selector.NumColumns = 4;
            selector.AllowExternalDrop = true;
            selector.Spacing = 10;
            selector.Pad = 8;

            // Hook selector events.
            selector.Click += Selector_Click;
            //icsel.Selection += (sender, e) => { e.SelectedItems.ForEach(it => tvInfo.Append($"Selection -> [{it}]")); };
            //icsel.Trace += (sender, e) => { tvInfo.Append($"Trace -> [{e.Line}]"); };

            // Selector menu.
            selector.ContextMenuStrip = new();
            selector.ContextMenuStrip.Items.Add("Add File");
            selector.ContextMenuStrip.Items.Add("Add Folder");
            selector.ContextMenuStrip.Items.Add("Paste");
            selector.ContextMenuStrip.Items.Add("Remove");
            selector.ContextMenuStrip.ItemClicked += Menu_ItemClicked;

            // Grab some system icons. Selector takes ownership of lifetime.
            _folderImage = Icon.ExtractIcon("shell32.dll", 3, false)!.ToBitmap();
            _urlImage = Icon.ExtractIcon("shell32.dll", 13, false)!.ToBitmap();
            _defaultImage = Icon.ExtractIcon("shell32.dll", 23, false)!.ToBitmap();

            // Init the data.
            _settings.Targets.ForEach(item => AddTarget(item));
        }


        void Dummy()
        {
            /*
Windows standard locations

-  All programs available in Start menu => %PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs +subdirs
C:\ProgramData\Microsoft\Windows\Start Menu\Programs
    |   Access.lnk (2k)
    |   Blend for Visual Studio 2022.lnk (1k)
    |   Everything.lnk (1k)
    |   Excel.lnk (2k)
    |   Firefox Private Browsing.lnk (1k)
    |   Firefox.lnk (1k)
    |   Microsoft Edge.lnk (2k)
    |   MIDI Settings.lnk (1k)
    |   Notepad++.lnk (879b)
    |   NZXT CAM.lnk (1k)
    |   OneNote.lnk (2k)
    |   PowerPoint.lnk (2k)
    |   Publisher.lnk (2k)
    |   Sticky Notes (new).lnk (2k)
    |   Sublime Text.lnk (915b)
    |   Visual Studio 2022.lnk (1k)
    |   Visual Studio Installer.lnk (1k)
    |   WinDirStat.lnk (1k)
    |   Word.lnk (2k)
    +---7-Zip
    |       7-Zip File Manager.lnk (778b)
    |       7-Zip Help.lnk (783b)
    +---Administrative Tools
    |       Event Viewer.lnk (1k)
    |       Performance Monitor.lnk (1k)
    |       Registry Editor.lnk (1k)
    |       Resource Monitor.lnk (1k)
    |       System Configuration.lnk (1k)
    +---Brother
    |       Brother Utilities.lnk (2k)
    +---Git
    |       Git Bash.lnk (1k)
    |       Git CMD.lnk (1k)
    |       Git GUI.lnk (1k)
    +---H&R Block 2024
    +---H&R Block 2025
    |       H&R Block 2025.lnk (1k)
    +---JetBrains
    |       JetBrains Rider 2025.3.2.lnk (1k)
    |       PyCharm 2025.3.2.1.lnk (1k)
    +---LibreOffice
    |       LibreOffice Calc.lnk (1k)
    |       LibreOffice Draw.lnk (1k)
    |       LibreOffice Math.lnk (1k)
    |       LibreOffice Writer.lnk (1k)
    |       LibreOffice.lnk (1k)
    +---Logi
    |       Logi Options+.lnk (877b)
    +---LOUD Technologies Inc\LOUD Technologies Inc. Mackie USB
    |       Mackie Control Panel.lnk (1k)
    +---Maintenance
    +---Microsoft Office
    |   |   Microsoft Excel 2010.lnk (2k)
    |   |   Microsoft Word 2010.lnk (2k)
    +---Microsoft Office Tools
    +---PuTTY (64-bit)
    |       PuTTY.lnk (1021b)
    +---REAPER (x64)
    |       REAPER (x64).lnk (961b)
    +---Startup
    +---System Tools
    |       Task Manager.lnk (1k)
    +---TortoiseGit
    |       TortoiseGit.lnk (1k)
    |       TortoiseGitBlame.lnk (1k)
    |       TortoiseGitIDiff.lnk (1k)
    |       TortoiseGitMerge.lnk (1k)
    +---VirtualMIDISynth
    |       VirtualMIDISynth.lnk (969b)
    +---Visual Studio 2022
    |   \---Visual Studio Tools
    |       |   Developer Command Prompt for VS 2022.lnk (2k)
    +---Windows Kits
    +---Windows PowerShell
    |       Windows PowerShell ISE (x86).lnk (1k)
    |       Windows PowerShell ISE.lnk (1k)
    \---WinMerge
            User's Guide.lnk (976b)
            WinMerge.lnk (975b)


-  Win-X/Start context menu => %LOCALAPPDATA%\Microsoft\Windows\WinX\GroupX
    +---C:\Users\cepth\AppData\Local\Microsoft\Windows\WinX\Group1
    |       1 - Desktop.lnk (1k)
    +---C:\Users\cepth\AppData\Local\Microsoft\Windows\WinX\Group2
    |       1 - Run.lnk (1k)
    |       2 - Search.lnk (1k)
    |       3 - Windows Explorer.lnk (1k)
    |       4 - Control Panel.lnk (1k)
    |       5 - Task Manager.lnk (1021b)
    \---C:\Users\cepth\AppData\Local\Microsoft\Windows\WinX\Group3
            01a - Windows PowerShell.lnk (1k)
            02a - Windows PowerShell.lnk (1k)
            03 - Computer Management.lnk (1015b)
            04 - Disk Management.lnk (1015b)
            04-1 - NetworkStatus.lnk (1k)
            05 - Device Manager.lnk (1k)
            06 - SystemAbout.lnk (1k)
            07 - Event Viewer.lnk (1015b)
            08 - PowerAndSleep.lnk (1k)
            09 - Mobility Center.lnk (1015b)
            10 - AppsAndFeatures.lnk (1k)


Others::
  - User Start menu => %APPDATA%\Microsoft\Windows\Start Menu\Programs\+subdirs...
  - Taskbar pinned => %APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar
  - Recent files => %APPDATA%\Microsoft\Windows\Recent and %APPDATA%\Microsoft\Office\Recent
*/





        }
        /// <summary>
        /// User wants to do something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            selector.ContextMenuStrip!.Close();
            //int index = selector.SelectedIndexes.Count > 0 ? selector.SelectedIndexes[0] : -1;

            switch (e.ClickedItem!.Text)
            {
                case "Add File":
                case "Add Folder":
                    CommonOpenFileDialog dialog = new()
                    {
                        InitialDirectory = @"%APPDATA%\Microsoft\Windows\Start Menu\Programs", // TODO1 from where?
                        IsFolderPicker = e.ClickedItem!.Text == "Add Folder"
                    };
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        AddTarget(dialog.FileName);
                    }
                    break;

                case "Paste":
                    AddTarget(Clipboard.GetText());
                    break;

                case "Remove":
//                    selector.RemoveSelectedItems();
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
            selector.GetAllItems().ForEach(it => _settings.Targets.Add(new() { Name = it.Value.ToString() } ));

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
        /// Add an item.
        /// </summary>
        /// <param name="target"></param>
        void AddTarget(Target target)
        {
            string text = "???";
            string targetname = target.Name;
            string targetnamelc = targetname.ToLower();
            string fulltargetname = "";
            Bitmap image = _defaultImage;

            ///// Determine target type.

            // Link?
            if (targetnamelc.EndsWith(".lnk"))
            {
                // What is it pointing to?
                var sl = ShellObject.FromParsingName(targetname);
                var ft = ((ShellLink)sl).TargetLocation;

                // File?
                if (File.Exists(ft))
                {
                    FileInfo finfo = new(ft);
                    text = finfo.Name;
                    fulltargetname = ft;

                    var icon = Icon.ExtractAssociatedIcon(ft);
                    if (icon != null)
                    {
                        image = icon.ToBitmap();
                    }
                }
                // Directory?
                else if (Directory.Exists(ft))
                {
                    DirectoryInfo dinfo = new(ft);
                    text = dinfo.Name;
                    fulltargetname = ft;
                    image = _folderImage;
                }
                else
                {
                    _logger.Error($"Invalid link [{targetname}]");
                }
            }
            // File?
            else if (File.Exists(targetname))
            {
                FileInfo finfo = new(targetname);
                text = finfo.Name;
                fulltargetname = targetname;

                var icon = Icon.ExtractAssociatedIcon(fulltargetname);
                if (icon != null)
                {
                    image = icon.ToBitmap();
                }
            }
            // Directory?
            else if (Directory.Exists(targetname))
            {
                DirectoryInfo dinfo = new(targetname);
                text = dinfo.Name;
                fulltargetname = targetname;
                image = _folderImage;
            }
            // URL?
            else if (targetnamelc.StartsWith("http://") || targetnamelc.StartsWith("https://") || targetnamelc.StartsWith("file://"))
            {
                var parts = targetname.Split("://");
                text = parts[1];
                fulltargetname = targetname;
                image = _urlImage;
            }
            // Not supported.
            else
            {
                _logger.Error($"Invalid target [{targetname}]");
            }

            if (fulltargetname != "")
            {
                selector.AddItem(text, image, fulltargetname);
            }
        }

        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="group"></param>
        void AddTarget(string name, string group = "")
        {
            AddTarget(new(){ Name = name, Group = group });
        }

        /// <summary>
        /// User clicked a selection. Execute it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Selector_Click(object? sender, ClickEventArgs e)
        {
            //_logger.Info($"Selection -> [{e.Entry.Text}] [{e.Entry.ImageName}] [{e.Entry.Tag}]");

            ProcessStartInfo pinfo = new("cmd", ["/C", e.ClickedItem.Value.ToString()!])
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            try
            {
                using Process proc = new() { StartInfo = pinfo };
                proc.Start();

                // TIL: To avoid deadlocks, always read the output stream first and then wait.
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();

                // LogInfo("Wait for process to exit...");
                proc.WaitForExit();
                // proc.ExitCode, stdout, stderr

            }
            catch (Exception ex)
            {
                _logger.Error($"Execute failed [{ex.Message}]");
            }
        }
        #endregion

        #region Privates
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            this.InvokeIfRequired(_ => Tell(e.Message));
        }

        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="s"></param>
        void Tell(string s)
        {
            rtbTell.AppendText(s);
            rtbTell.AppendText(Environment.NewLine);
            rtbTell.ScrollToCaret();
        }
        #endregion

        #region User settings
        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 450);

            // Detect changes of interest.
            bool restart = false;
            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "Style":
                    case "ImageSize":
                        restart = true;
                        break;
                }
            }
            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            selector.IndicatorColor = _settings.MarkerColor;
            selector.DrawFont = _settings.TileFont;

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
