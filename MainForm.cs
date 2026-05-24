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
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 50000);

            Text = $"WinStart {MiscUtils.GetVersionString()}";

            // Default - big X.
            Bitmap defbmp = new(32, 32);
            using Graphics gr = Graphics.FromImage(defbmp);
            gr.Clear(Color.LightSalmon);
            gr.DrawString($"????", Font, Brushes.Black, 2, 2);

            // Init selector properties.
            selector.AllowExternalSource = true;
            selector.AutoScroll = true;
            //selector.Dock = DockStyle.Fill;
            selector.Style = SelectorStyle.Icon;
            selector.Mode = OpMode.Click;
            selector.Spacing = 10;
            selector.Pad = 8;
            selector.DefaultImage = defbmp;
            selector.DrawFont = _settings.Font;
            selector.IndicatorColor = _settings.MarkerColor;
            selector.ImageSize = new(_settings.ImageSize, _settings.ImageSize);
            selector.NumColumns = _settings.NumColumns;

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
            //_settings.Targets.ForEach(item => selector.AddResourceItem(item));
            DoDummy();

            // Size and location. TODO1 probably user option? always/popup/?
            FormBorderStyle = FormBorderStyle.SizableToolWindow;// FixedToolWindow;
            StartPosition = FormStartPosition.Manual;
            Size = new(selector.Width + SystemInformation.VerticalScrollBarWidth, 600);
            WindowState = FormWindowState.Normal;
            //WindowState = FormWindowState.Minimized;
            var pos = Cursor.Position;
            Location = new Point(200, 200);
        }

        /// <summary>
        /// 
        /// </summary>
        void DoDummy() //_TODO1_test()
        {
            string[] locs =
            [
                // Windows standard locations  %PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Everything.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Excel.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Firefox.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\MIDI Settings.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Notepad++.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Sublime Text.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Visual Studio 2022.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\WinDirStat.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Word.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Administrative Tools\Performance Monitor.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Administrative Tools\Registry Editor.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Administrative Tools\Resource Monitor.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Git\Git Bash.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Logi\Logi Options+.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\LOUD Technologies Inc\LOUD Technologies Inc. Mackie USB\Mackie Control Panel.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\REAPER (x64)\REAPER (x64).lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\System Tools\Task Manager.lnk",
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\VirtualMIDISynth\VirtualMIDISynth.lnk",
                // Win-X/Start context menu  %LOCALAPPDATA%\Microsoft\Windows\WinX\GroupX => don't use
                //@"C:\Users\cepth\AppData\Local\Microsoft\Windows\WinX\Group2\1 - Run.lnk",
                //@"C:\Users\cepth\AppData\Local\Microsoft\Windows\WinX\Group2\2 - Search.lnk",
                //@"C:\Users\cepth\AppData\Local\Microsoft\Windows\WinX\Group2\3 - Windows Explorer.lnk",
                //@"C:\Users\cepth\AppData\Local\Microsoft\Windows\WinX\Group2\4 - Control Panel.lnk",
                // Plain files
                @"C:\Users\cepth\OneDrive\Tools\backup_loose.py",
                @"C:\Users\cepth\OneDrive\Tools\Wavosaur.exe",
                @"C:\Users\cepth\OneDrive\Tools\procexp.exe",
                @"C:\Dev\Libs\IconicSelector\Test\Files\color_wheel.png",
                @"C:\Users\cepth\OneDrive\OneDriveDocuments\eat\Dried Cherry Scones.txt",
                @"C:\Users\cepth\OneDrive\OneDriveDocuments\eat\food-places.xlsx",
                @"C:\Users\cepth\OneDrive\OneDriveDocuments\eat\makepage.py",
                @"C:\Users\cepth\OneDrive\OneDriveDocuments\eat\faves\bean-potato-gratin.pdf",
                @"C:\Users\cepth\OneDrive\OneDriveDocuments\eat\faves\Beans.docx",
                // Plain folders
                @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup",
                @"C:\Users\cepth\OneDrive\OneDriveDocuments",
                @"C:\Dev\Apps",
                // URLs
                @"https://github.com/oozcitak/imagelistview",
                @"https://www.bobrosslipsum.com/",
                @"https://en.wikipedia.org/wiki/INI_file",
            ];
            
            // Others::
            //   User Start menu => %APPDATA%\Microsoft\Windows\Start Menu\Programs\+subdirs...
            //   Taskbar pinned => %APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar
            //   Recent files => %APPDATA%\Microsoft\Windows\Recent and %APPDATA%\Microsoft\Office\Recent

            locs.ForEach(item => selector.AddResourceItem(item));
        }

        /// <summary>
        /// User wants to do something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            selector.ContextMenuStrip!.Close();

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
                        selector.AddResourceItem(dialog.FileName);
                    }
                    break;

                case "Paste":
                    selector.AddResourceItem(Clipboard.GetText());
                    break;

                //case "Remove": // TODO1
                //    selector.RemoveSelectedItems();
                //    break;
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
        ///// <summary>
        ///// Add an item.
        ///// </summary>
        ///// <param name="target"></param>
        //void AddTarget(Target target)
        //{
        //    ItemDataType dtype = ItemDataType.None;
        //    string text = "???";
        //    string targetname = target.Name;
        //    string targetnamelc = targetname.ToLower();
        //    string fulltargetname = "";
        //    Bitmap image = _defaultImage;


        //    ///// Determine target type. =======> this is in selector now

        //    // Link?
        //    if (targetnamelc.EndsWith(".lnk")) //check is-a-link?
        //    {
        //        try
        //        {
        //            // What is it pointing to?
        //            var sl = ShellObject.FromParsingName(targetname);
        //            var ft = ((ShellLink)sl).TargetLocation;

        //            // File?
        //            if (File.Exists(ft))
        //            {
        //                FileInfo finfo = new(ft);
        //                text = finfo.Name;
        //                fulltargetname = ft;

        //                var icon = Icon.ExtractAssociatedIcon(ft);
        //                if (icon != null)
        //                {
        //                    image = icon.ToBitmap();
        //                }
        //            }
        //            // Directory?
        //            else if (Directory.Exists(ft))
        //            {
        //                DirectoryInfo dinfo = new(ft);
        //                text = dinfo.Name;
        //                fulltargetname = ft;
        //                image = _folderImage;
        //            }
        //            else
        //            {
        //                _logger.Error($"Invalid target for link [{targetname}]");
        //            }

        //        }
        //        catch (Exception)
        //        {
        //            _logger.Error($"Invalid link [{targetname}]");
        //        }
        //    }

        //    // File?
        //    else if (File.Exists(targetname))
        //    {
        //        FileInfo finfo = new(targetname);
        //        text = finfo.Name;
        //        fulltargetname = targetname;

        //        var icon = Icon.ExtractAssociatedIcon(fulltargetname);
        //        if (icon != null)
        //        {
        //            image = icon.ToBitmap();
        //        }
        //    }
        //    // Directory?
        //    else if (Directory.Exists(targetname))
        //    {
        //        DirectoryInfo dinfo = new(targetname);
        //        text = dinfo.Name;
        //        fulltargetname = targetname;
        //        image = _folderImage;
        //    }
        //    // URL?
        //    else if (targetnamelc.StartsWith("http://") || targetnamelc.StartsWith("https://") || targetnamelc.StartsWith("file://"))
        //    {
        //        var parts = targetname.Split("://");
        //        text = parts[1];
        //        fulltargetname = targetname;
        //        image = _urlImage;
        //    }
        //    // Not supported.
        //    else
        //    {
        //        _logger.Error($"Invalid target [{targetname}]");
        //    }

        //    if (fulltargetname != "")
        //    {
        //        selector.AddItem(text, image, fulltargetname);
        //    }
        //}

        ///// <summary>
        ///// Add an item.
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="group"></param>
        //void AddTarget(string name, string group = "")
        //{
        //    AddTarget(new(){ Name = name, Group = group });
        //}

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

                // if folder: explorer "c:\dev"
                // if url: start https://www.bobrosslipsum.com/
                // else cmd

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
                    int code = proc.ExitCode;

                    // LogInfo("Wait for process to exit...");
                    proc.WaitForExit();

                    if (code != 0)
                    {
                        _logger.Error($"code:{code}");
                        _logger.Error($"stderr: {stderr}");
                        _logger.Error($"stdout: {stdout}");
                    }

                }
                catch (Exception ex)
                {
                    _logger.Error($"Execute failed [{ex.Message}]");
                }

            }
            else
            {
                // Item selection(s).


            }
            _logger.Info($"Selection -> [{e.ClickedItem.Caption}] [{e.ClickedItem.Value}]");

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
            this.InvokeIfRequired(_ =>
            {
                //rtbTell.AppendText(s);
                //rtbTell.AppendText(Environment.NewLine);
                //rtbTell.ScrollToCaret();
            });
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
            selector.DrawFont = _settings.Font;

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
