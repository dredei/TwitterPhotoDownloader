﻿#region Using

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Windows7.DesktopIntegration.WindowsForms;
using ExtensionMethods;
using Ini;

#endregion

namespace TwitterPhotoDownloader
{
    public partial class FrmMain : Form
    {
        private TwitterDownloader _twitterDownloader;
        private string _language = "en-GB";
        private const string Version = "1.0.1";
        private Thread _thread;
        private Thread _checkInternetThread;
        private readonly bool _possibleProgressInTaskBar;

        public FrmMain()
        {
            this.LoadSettings();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo( this._language );
            this.InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            if ( Environment.OSVersion.Version >= new Version( 6, 1 ) ) // if version current version >= win7
            {
                this._possibleProgressInTaskBar = true;
            }
        }

        private void ChangeLanguage( string lang )
        {
            this._language = lang;
            this.SaveSettings();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo( this._language );
            DialogResult res = MessageBox.Show( strings.RestartApp, strings.Warning, MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning );
            if ( res == DialogResult.Yes )
            {
                Application.Restart();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var ini = new IniFile( Application.StartupPath + @"\Settings.ini" );
                ini.Write( "Lang", this._language, "Options" );
            }
            catch
            {
            }
        }

        private void LoadSettings()
        {
            try
            {
                var ini = new IniFile( Application.StartupPath + @"\Settings.ini" );
                this._language = ini.Read( "Lang", "Options", this._language );
            }
            catch
            {
            }
        }

        private void Work()
        {
            this.LoadSettings();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo( this._language );
            try
            {
                this._twitterDownloader = new TwitterDownloader();
                this._twitterDownloader.DownloadPhotos( this.tbUserName.Text, this.tbSavePath.Text );
                this.tmrProgress.Stop();
                MessageBox.Show( strings.Done, strings.Information, MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
            catch ( Exception exception )
            {
                MessageBox.Show( exception.Message, strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
            finally
            {
                this.Invoke( new MethodInvoker( delegate()
                {
                    this.tmrProgress.Stop();
                    this.lblInfo.Text = strings.Points;
                    this.pb1.Value = 0;
                    this.pb1.Style = ProgressBarStyle.Blocks;
                    this.DisEnControls();
                } ) );
            }
        }

        private void DisEnControls()
        {
            this.tbSavePath.Enabled = !this.tbSavePath.Enabled;
            this.tbUserName.Enabled = !this.tbUserName.Enabled;
            this.btnSelectDir.Enabled = !this.btnSelectDir.Enabled;
            this.btnStart.Enabled = !this.btnStart.Enabled;
        }

        private void btnStart_Click( object sender, EventArgs e )
        {
            if ( string.IsNullOrEmpty( this.tbSavePath.Text ) || string.IsNullOrEmpty( this.tbUserName.Text ) )
            {
                MessageBox.Show( strings.FillTheFields, strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error );
                return;
            }
            this.tbUserName.Text = TwitterDownloader.FixUserName( this.tbUserName.Text );
            this.DisEnControls();
            this._thread = new Thread( this.Work );
            this._thread.SetApartmentState( ApartmentState.STA );
            this._thread.Start();
            this.tmrProgress.Start();
        }

        private void tmrProgress_Tick( object sender, EventArgs e )
        {
            switch ( this._twitterDownloader.Progress.Type )
            {
                case ProgressType.GettingImages:
                    this.lblInfo.Text = string.Format( strings.GettingImages, this._twitterDownloader.Progress.Page );
                    this.pb1.Style = ProgressBarStyle.Marquee;
                    break;

                case ProgressType.DownloadingImages:
                    this.lblInfo.Text = strings.DownloadingImages;
                    this.pb1.Style = ProgressBarStyle.Blocks;
                    this.pb1.Maximum = this._twitterDownloader.Progress.MaxProgress;
                    this.pb1.Value = this._twitterDownloader.Progress.CurrentProgress;
                    break;
            }
            if ( this._possibleProgressInTaskBar )
            {
                this.pb1.SetTaskbarProgress();
            }
        }

        private void lblSite_Click( object sender, EventArgs e )
        {
            Process.Start( "http://www.softez.pp.ua/" );
        }

        private void FrmMain_FormClosing( object sender, FormClosingEventArgs e )
        {
            this.SaveSettings();
            if ( this._thread != null )
            {
                this._thread.Abort();
            }
            if ( this._checkInternetThread != null )
            {
                this._checkInternetThread.Abort();
            }
        }

        private void tsmiEng_Click( object sender, EventArgs e )
        {
            this.ChangeLanguage( "en-GB" );
        }

        private void tsmiRus_Click( object sender, EventArgs e )
        {
            this.ChangeLanguage( "ru-RU" );
        }

        private void aboutToolStripMenuItem_Click( object sender, EventArgs e )
        {
            MessageBox.Show( strings.AboutInfo.FixNewLines() + Version, strings.About, MessageBoxButtons.OK,
                MessageBoxIcon.Information );
        }

        private void btnSelectDir_Click( object sender, EventArgs e )
        {
            if ( this.fbd1.ShowDialog() == DialogResult.OK )
            {
                this.tbSavePath.Text = this.fbd1.SelectedPath;
            }
        }

        private void tbUserName_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Enter )
            {
                this.btnStart.PerformClick();
            }
        }

        private void tbSavePath_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Enter )
            {
                this.btnStart.PerformClick();
            }
        }

        private void tmrCheckInternet_Tick( object sender, EventArgs e )
        {
            this.tmrCheckInternet.Stop();
            this._checkInternetThread = new Thread( delegate()
            {
                if ( TwitterDownloader.CheckForInternetConnection() )
                {
                    this.btnStart.Enabled = true;
                }
                else
                {
                    this.tmrCheckInternet.Interval = 5000;
                    MessageBox.Show( strings.UnableAccess, strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error );
                    this.tmrCheckInternet.Start();
                }
            } ) { Priority = ThreadPriority.Lowest };
            this._checkInternetThread.Start();
        }
    }
}