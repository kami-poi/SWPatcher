﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Ionic.Zip;
using MadMilkman.Ini;
using SWPatcher.Downloading;
using SWPatcher.General;
using SWPatcher.Helpers;
using SWPatcher.Helpers.GlobalVar;
using SWPatcher.Patching;
using System.Net;
using System.Threading.Tasks;

namespace SWPatcher.Forms
{
    public partial class MainForm : Form
    {
        public enum States
        {
            Idle = 0,
            Downloading = 1,
            Patching = 2,
            Preparing = 3,
            WaitingClient = 4,
            Applying = 5,
            WaitingClose = 6
        }

        private States _state;
        private readonly Downloader Downloader;
        private readonly Patcher Patcher;
        private readonly BackgroundWorker Worker;
        private readonly List<SWFile> SWFiles;

        public States State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state != value)
                {
                    switch (value)
                    {
                        case States.Idle:
                            comboBoxLanguages.Enabled = true;
                            buttonDownload.Enabled = true;
                            buttonDownload.Text = Strings.FormText.Download;
                            buttonPlay.Enabled = true;
                            buttonPlay.Text = Strings.FormText.Play;
                            buttonExit.Enabled = true;
                            forceStripMenuItem.Enabled = true;
                            refreshToolStripMenuItem.Enabled = true;
                            toolStripStatusLabel.Text = Strings.FormText.Status.Idle;
                            toolStripProgressBar.Value = toolStripProgressBar.Minimum;
                            toolStripProgressBar.Style = ProgressBarStyle.Blocks;
                            break;
                        case States.Downloading:
                            comboBoxLanguages.Enabled = false;
                            buttonDownload.Enabled = true;
                            buttonDownload.Text = Strings.FormText.Cancel;
                            buttonPlay.Enabled = false;
                            buttonPlay.Text = Strings.FormText.Play;
                            buttonExit.Enabled = false;
                            forceStripMenuItem.Enabled = false;
                            refreshToolStripMenuItem.Enabled = false;
                            toolStripStatusLabel.Text = Strings.FormText.Status.Download;
                            toolStripProgressBar.Value = toolStripProgressBar.Minimum;
                            toolStripProgressBar.Style = ProgressBarStyle.Blocks;
                            break;
                        case States.Patching:
                            comboBoxLanguages.Enabled = false;
                            buttonDownload.Enabled = true;
                            buttonDownload.Text = Strings.FormText.Cancel;
                            buttonPlay.Enabled = false;
                            buttonPlay.Text = Strings.FormText.Play;
                            buttonExit.Enabled = false;
                            forceStripMenuItem.Enabled = false;
                            refreshToolStripMenuItem.Enabled = false;
                            toolStripStatusLabel.Text = Strings.FormText.Status.Patch;
                            toolStripProgressBar.Value = toolStripProgressBar.Minimum;
                            toolStripProgressBar.Style = ProgressBarStyle.Blocks;
                            break;
                        case States.Preparing:
                            comboBoxLanguages.Enabled = false;
                            buttonDownload.Enabled = false;
                            buttonDownload.Text = Strings.FormText.Download;
                            buttonPlay.Enabled = false;
                            buttonPlay.Text = Strings.FormText.Play;
                            buttonExit.Enabled = false;
                            forceStripMenuItem.Enabled = false;
                            refreshToolStripMenuItem.Enabled = false;
                            toolStripStatusLabel.Text = Strings.FormText.Status.Prepare;
                            toolStripProgressBar.Value = toolStripProgressBar.Minimum;
                            toolStripProgressBar.Style = ProgressBarStyle.Marquee;
                            break;
                        case States.WaitingClient:
                            comboBoxLanguages.Enabled = false;
                            buttonDownload.Enabled = false;
                            buttonDownload.Text = Strings.FormText.Download;
                            buttonPlay.Enabled = true;
                            buttonPlay.Text = Strings.FormText.Cancel;
                            buttonExit.Enabled = false;
                            forceStripMenuItem.Enabled = false;
                            refreshToolStripMenuItem.Enabled = false;
                            toolStripStatusLabel.Text = Strings.FormText.Status.WaitClient;
                            toolStripProgressBar.Value = toolStripProgressBar.Minimum;
                            toolStripProgressBar.Style = ProgressBarStyle.Blocks;
                            break;
                        case States.Applying:
                            comboBoxLanguages.Enabled = false;
                            buttonDownload.Enabled = false;
                            buttonDownload.Text = Strings.FormText.Download;
                            buttonPlay.Enabled = false;
                            buttonPlay.Text = Strings.FormText.Play;
                            buttonExit.Enabled = false;
                            forceStripMenuItem.Enabled = false;
                            refreshToolStripMenuItem.Enabled = false;
                            toolStripStatusLabel.Text = Strings.FormText.Status.ApplyFiles;
                            toolStripProgressBar.Value = toolStripProgressBar.Minimum;
                            toolStripProgressBar.Style = ProgressBarStyle.Marquee;
                            break;
                        case States.WaitingClose:
                            comboBoxLanguages.Enabled = false;
                            buttonDownload.Enabled = false;
                            buttonDownload.Text = Strings.FormText.Download;
                            buttonPlay.Enabled = false;
                            buttonPlay.Text = Strings.FormText.Play;
                            buttonExit.Enabled = false;
                            forceStripMenuItem.Enabled = false;
                            refreshToolStripMenuItem.Enabled = false;
                            toolStripStatusLabel.Text = Strings.FormText.Status.WaitClose;
                            toolStripProgressBar.Value = toolStripProgressBar.Minimum;
                            toolStripProgressBar.Style = ProgressBarStyle.Marquee;
                            WindowState = FormWindowState.Minimized;
                            break;
                    }

                    this.comboBoxLanguages_SelectedIndexChanged(null, null);
                    _state = value;
                }
            }
        }

        public MainForm()
        {
            this.SWFiles = new List<SWFile>();
            this.Downloader = new Downloader(SWFiles);
            this.Downloader.DownloaderProgressChanged += new DownloaderProgressChangedEventHandler(Downloader_DownloaderProgressChanged);
            this.Downloader.DownloaderCompleted += new DownloaderCompletedEventHandler(Downloader_DownloaderCompleted);
            this.Patcher = new Patcher(SWFiles);
            this.Patcher.PatcherProgressChanged += new PatcherProgressChangedEventHandler(Patcher_PatcherProgressChanged);
            this.Patcher.PatcherCompleted += new PatcherCompletedEventHandler(Patcher_PatcherCompleted);
            this.Worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            this.Worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            this.Worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            this.Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
            InitializeComponent();
            this.Text = AssemblyAccessor.Title + " " + AssemblyAccessor.Version;
        }

        private void Downloader_DownloaderProgressChanged(object sender, DownloaderProgressChangedEventArgs e)
        {
            if (this.State == States.Downloading)
            {
                this.toolStripStatusLabel.Text = String.Format("{0} {1} ({2}/{3})", Strings.FormText.Status.Download, e.FileName, e.FileNumber, e.FileCount);
                this.toolStripProgressBar.Value = e.Progress;
            }
        }

        private void Downloader_DownloaderCompleted(object sender, DownloaderCompletedEventArgs e)
        {
            if (e.Cancelled) { }
            else if (e.Error != null)
            {
                Error.Log(e.Error);
                MsgBox.Error(Error.ExeptionParser(e.Error));
            }
            else
            {
                this.State = States.Patching;
                this.Patcher.Run(e.Language);

                return;
            }

            this.State = 0;
        }

        private void Patcher_PatcherProgressChanged(object sender, PatcherProgressChangedEventArgs e)
        {
            if (this.State == States.Patching)
            {
                this.toolStripStatusLabel.Text = String.Format("{0} Step {1}/{2}", Strings.FormText.Status.Patch, e.FileNumber, e.FileCount);
                this.toolStripProgressBar.Value = e.Progress;
            }
        }

        private void Patcher_PatcherCompleted(object sender, PatcherCompletedEventArgs e)
        {
            if (e.Cancelled) { }
            else if (e.Error != null)
            {
                Error.Log(e.Error);
                MsgBox.Error(Error.ExeptionParser(e.Error));
                Methods.DeleteTmpFiles(e.Language);
            }
            else
            {
                IniFile ini = new IniFile(new IniOptions
                {
                    KeyDuplicate = IniDuplication.Ignored,
                    SectionDuplicate = IniDuplication.Ignored
                });
                ini.Load(Path.Combine(Paths.GameRoot, Strings.IniName.ClientVer));
                string clientVer = ini.Sections[Strings.IniName.Ver.Section].Keys[Strings.IniName.Ver.Key].Value;

                string iniPath = Path.Combine(Paths.PatcherRoot, e.Language.Lang, Strings.IniName.Translation);
                if (!File.Exists(iniPath))
                    File.Create(iniPath).Dispose();

                ini.Sections.Clear();
                ini.Load(iniPath);
                ini.Sections.Add(Strings.IniName.Patcher.Section);
                ini.Sections[Strings.IniName.Patcher.Section].Keys.Add(Strings.IniName.Pack.KeyDate);
                ini.Sections[Strings.IniName.Patcher.Section].Keys[Strings.IniName.Pack.KeyDate].Value = Methods.DateToString(e.Language.LastUpdate);
                ini.Sections[Strings.IniName.Patcher.Section].Keys.Add(Strings.IniName.Patcher.KeyVer);
                ini.Sections[Strings.IniName.Patcher.Section].Keys[Strings.IniName.Patcher.KeyVer].Value = clientVer;
                ini.Save(iniPath);
            }

            this.State = 0;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Language language = e.Argument as Language;

            if (Methods.IsNewerGameClientVersion())
                throw new Exception("Game client is not updated to the latest version.");

            if (Methods.IsTranslationOutdated(language))
            {
                e.Result = true; // force patch = true
                return;
            }

            if (this.SWFiles.Count == 0)
            {
                this.SWFiles.Clear();
                using (var client = new WebClient())
                using (var file = new TempFile())
                {
                    client.DownloadFile(Urls.PatcherGitHubHome + Strings.IniName.TranslationPackData, file.Path);
                    IniFile ini = new IniFile();
                    ini.Load(file.Path);

                    foreach (var section in ini.Sections)
                    {
                        string name = section.Name;
                        string path = section.Keys[Strings.IniName.Pack.KeyPath].Value;
                        string pathA = section.Keys[Strings.IniName.Pack.KeyPathInArchive].Value;
                        string pathD = section.Keys[Strings.IniName.Pack.KeyPathOfDownload].Value;
                        string format = section.Keys[Strings.IniName.Pack.KeyFormat].Value;
                        this.SWFiles.Add(new SWFile(name, path, pathA, pathD, format));

                        if (this.Worker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }

            Process clientProcess = null;

            this.Worker.ReportProgress((int)States.WaitingClient);
            while(true)
            {
                if (this.Worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                clientProcess = Methods.GetProcess(Strings.FileName.GameExe);

                if (clientProcess == null)
                    Thread.Sleep(1000);
                else
                    break;
            }

            this.Worker.ReportProgress((int)States.Applying);
            var archives = this.SWFiles.Where(f => !String.IsNullOrEmpty(f.PathA)).Select(f => f.Path).Distinct();

            foreach (var archive in archives) // backup and place *.v fils
            {
                string archivePath = Path.Combine(Paths.GameRoot, archive);
                string backupFilePath = Path.Combine(Paths.PatcherRoot, Strings.FolderName.Backup, archive);
                string backupFileDirectory = Path.GetDirectoryName(backupFilePath);

                if (!Directory.Exists(backupFileDirectory))
                    Directory.CreateDirectory(backupFileDirectory);

                File.Move(archivePath, backupFilePath);
                File.Move(Path.Combine(Paths.PatcherRoot, language.Lang, archive), archivePath);
            }

            var swFiles = this.SWFiles.Where(f => String.IsNullOrEmpty(f.PathA));

            foreach (var swFile in swFiles) // other files that weren't patched
            {
                string swFileName = Path.Combine(swFile.Path, Path.GetFileName(swFile.PathD));
                string swFilePath = Path.Combine(Paths.PatcherRoot, language.Lang, swFileName);
                string filePath = Path.Combine(Paths.GameRoot, swFileName);
                string backupFilePath = Path.Combine(Paths.PatcherRoot, Strings.FolderName.Backup, swFileName);
                string backupFileDirectory = Path.GetDirectoryName(backupFilePath);

                if (!Directory.Exists(backupFileDirectory))
                    Directory.CreateDirectory(backupFileDirectory);

                File.Move(filePath, backupFilePath);
                File.Move(swFilePath, filePath);
            }

            this.Worker.ReportProgress((int)States.WaitingClose);
            clientProcess.WaitForExit();
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.State = (States)e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled) { }
            else if (e.Error != null)
            {
                Error.Log(e.Error);
                MsgBox.Error(Error.ExeptionParser(e.Error));
            }
            else if (e.Result != null && Convert.ToBoolean(e.Result))
            {
                MsgBox.Error("Your translation files are outdated, force patching will now commence.");
                forceStripMenuItem_Click(null, null);

                return;
            }
            else
                notifyIcon_DoubleClick(null, null);

            try
            {
                Methods.RestoreBackup(this.comboBoxLanguages.SelectedItem as Language);
            }
            finally
            {
                this.State = 0;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Methods.RestoreBackup();
            this.comboBoxLanguages.DataSource = Methods.GetAvailableLanguages();
            //this.comboBoxLanguages.DataSource = new Language[0];

            if (String.IsNullOrEmpty(Paths.GameRoot))
                Paths.GameRoot = Methods.GetSwPathFromRegistry();

            if (!Methods.IsValidSwPatcherPath(Paths.PatcherRoot))
            {
                string error = "The program is in the same or in a sub folder as your game client.\nThis will cause malfunctions or data corruption on your game client.\nPlease move the patcher in another location or continue at your own risk.";

                Error.Log(error);
                MsgBox.Error(error);
            }
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            if (this.State == States.Downloading)
            {
                this.buttonDownload.Enabled = false;
                this.buttonDownload.Text = Strings.FormText.Cancelling;
                this.Downloader.Cancel();
            }
            else if (this.State == States.Patching)
            {
                this.buttonDownload.Enabled = false;
                this.buttonDownload.Text = Strings.FormText.Cancelling;
                this.Patcher.Cancel();
            }
            else if (this.State == States.Idle)
            {
                this.State = States.Downloading;
                this.Downloader.Run(this.comboBoxLanguages.SelectedItem as Language, false);
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (this.State == States.WaitingClient)
            {
                this.Worker.CancelAsync();
                this.State = 0;
            }
            else
            {
                this.State = States.Preparing;
                this.Worker.RunWorkerAsync(this.comboBoxLanguages.SelectedItem as Language);
            }
        }

        private void comboBoxLanguages_SelectedIndexChanged(object sender, EventArgs e)
        {
            Language language = this.comboBoxLanguages.SelectedItem as Language;

            if (language != null && Methods.HasNewTranslations(language))
                this.labelNewTranslations.Text = Strings.FormText.NewTranslations;
            else
                this.labelNewTranslations.Text = String.Empty;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(500);

                this.ShowInTaskbar = false;
                this.Hide();
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Show();

            notifyIcon.Visible = false;
        }

        private void forceStripMenuItem_Click(object sender, EventArgs e)
        {
            Language language = this.comboBoxLanguages.SelectedItem as Language;

            Methods.DeleteTranslationIni(language);
            this.labelNewTranslations.Text = Strings.FormText.NewTranslations;

            this.State = States.Downloading;
            this.Downloader.Run(language, true);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.ShowDialog(this);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Language language = this.comboBoxLanguages.SelectedItem as Language;
            Language[] languages = Methods.GetAvailableLanguages();
            comboBoxLanguages.DataSource = languages.Length > 0 ? languages : null;

            if (language != null && comboBoxLanguages.DataSource != null)
            {
                int index = comboBoxLanguages.Items.IndexOf(language);
                comboBoxLanguages.SelectedIndex = index == -1 ? 0 : index;
            }
        }

        private void openSWWebpageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("iexplore.exe", Urls.SoulWorkerHome);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog(this);
        }

        private void exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
