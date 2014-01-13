#region Using

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Ini;

#endregion

namespace TwitterPhotoDownloader
{
    public partial class FrmMain : Form
    {
        private readonly TwitterDownloader _twitterDownloader;
        private string _language = "en-GB";

        public FrmMain()
        {
            this.LoadSettings();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo( this._language );
            this.InitializeComponent();
            this._twitterDownloader = new TwitterDownloader();
            CheckForIllegalCrossThreadCalls = false;
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
                this.tmrProgress.Stop();
                this.lblInfo.Text = strings.Points;
                this.pb1.Value = 0;
                this.pb1.Style = ProgressBarStyle.Blocks;
                this.DisEnControls();
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
            tbUserName.Text = TwitterDownloader.FixUserName( tbUserName.Text );
            this.DisEnControls();
            Thread thread = new Thread( this.Work );
            thread.SetApartmentState( ApartmentState.STA );
            thread.Start();
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
        }

        private void lblSite_Click( object sender, EventArgs e )
        {
            Process.Start( "http://www.softez.pp.ua/" );
        }

        private void FrmMain_FormClosing( object sender, FormClosingEventArgs e )
        {
            this.SaveSettings();
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
            MessageBox.Show( strings.AboutInfo, strings.About, MessageBoxButtons.OK, MessageBoxIcon.Information );
        }
    }
}