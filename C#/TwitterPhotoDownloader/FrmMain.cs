#region Using

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows7.DesktopIntegration.WindowsForms;
using ExtensionMethods;
using Ini;

#endregion

namespace TwitterPhotoDownloader
{
    public partial class FrmMain : Form
    {
        private readonly TwitterDownloader _twitterDownloader;
        private string _language = "en-GB";
        private Thread _checkInternetThread;
        private readonly bool _possibleProgressInTaskBar;
        private readonly Version _version = Version.Parse( "1.1.3" );
        private CancellationTokenSource _cancellationTokenSource;

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
            this.tbSavePath.Text = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) +
                                   "\\TwitterPhotoDownloader";
            this._twitterDownloader = new TwitterDownloader();
        }

        private void AutoPosLabels()
        {
            this.lblImgDownloaded.Left = this.lblImgFound.Right + 6;
            this.lblImgErrors.Left = this.lblImgDownloaded.Right + 6;
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

        private async Task WorkAsync()
        {
            this.LoadSettings();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo( this._language );
            try
            {
                this._cancellationTokenSource = new CancellationTokenSource();
                await
                    this._twitterDownloader.DownloadPhotosAsync( this.tbUserName.Text, this.tbSavePath.Text,
                        this._cancellationTokenSource.Token );
                this.tmrProgress.Stop();
                MessageBox.Show( strings.Done, strings.Information, MessageBoxButtons.OK, MessageBoxIcon.Information );
                if ( this._twitterDownloader.ErrorsLinks.Count > 0 )
                {
                    DialogResult dr = MessageBox.Show( strings.CopyToClipboard, strings.Error, MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question );
                    if ( dr == DialogResult.Yes )
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach ( string s in this._twitterDownloader.ErrorsLinks )
                        {
                            sb.AppendLine( s );
                        }
                        Clipboard.SetText( sb.ToString() );
                    }
                }
            }
            catch ( Exception exception )
            {
                if ( exception is OperationCanceledException )
                {
                    return;
                }
                MessageBox.Show( exception.Message, strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
            this.Invoke( new MethodInvoker( delegate
            {
                this.tmrProgress.Stop();
                this.lblInfo.Text = strings.Points;
                this.pb1.Value = 0;
                this.pb1.Style = ProgressBarStyle.Blocks;
                this.DisEnControls();
                if ( this._possibleProgressInTaskBar )
                {
                    this.pb1.SetTaskbarProgress();
                }
            } ) );
            GC.Collect();
        }

        private void DisEnControls()
        {
            this.tbSavePath.Enabled = !this.tbSavePath.Enabled;
            this.tbUserName.Enabled = !this.tbUserName.Enabled;
            this.btnSelectDir.Enabled = !this.btnSelectDir.Enabled;
            this.btnStart.Enabled = !this.btnStart.Enabled;
        }

        private async void btnStart_ClickAsync( object sender, EventArgs e )
        {
            if ( string.IsNullOrEmpty( this.tbSavePath.Text ) || string.IsNullOrEmpty( this.tbUserName.Text ) )
            {
                MessageBox.Show( strings.FillTheFields, strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error );
                return;
            }
            this.tbUserName.Text = TwitterDownloader.FixUserName( this.tbUserName.Text );
            this.DisEnControls();
            this.tmrProgress.Start();
            await this.WorkAsync();
        }

        private void tmrProgress_Tick( object sender, EventArgs e )
        {
            if ( this._twitterDownloader != null )
            {
                ProgressC progress = this._twitterDownloader.Progress;
                switch ( progress.Type )
                {
                    case ProgressType.GettingImages:
                        this.lblInfo.Text = string.Format( strings.GettingImages, progress.Page );
                        this.pb1.Style = ProgressBarStyle.Marquee;
                        break;

                    case ProgressType.DownloadingImages:
                        this.lblInfo.Text = strings.DownloadingImages;
                        this.pb1.Style = ProgressBarStyle.Blocks;
                        this.pb1.Maximum = progress.MaxProgress;
                        this.pb1.Value = progress.CurrentProgress;
                        break;
                }
                this.lblImgFound.Text = strings.Found + progress.MaxProgress;
                this.lblImgDownloaded.Text = strings.Downloaded + progress.Downloaded;
                this.lblImgErrors.Text = strings.Errors + this._twitterDownloader.ErrorsLinks.Count;
            }
            this.AutoPosLabels();
            if ( this._possibleProgressInTaskBar )
            {
                this.pb1.SetTaskbarProgress();
            }
        }

        private void lblSite_Click( object sender, EventArgs e )
        {
            Process.Start( "http://www.softez.pp.ua/" );
        }

        private async void FrmMain_FormClosing( object sender, FormClosingEventArgs e )
        {
            this.SaveSettings();
            if ( this._cancellationTokenSource != null )
            {
                this._cancellationTokenSource.Cancel();
            }
            if ( this._checkInternetThread != null )
            {
                this._checkInternetThread.Abort();
            }
            try
            {
                await this._twitterDownloader.Exit();
            }
            catch
            {
                return;
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
            MessageBox.Show( strings.AboutInfo.FixNewLines() + this._version, strings.About, MessageBoxButtons.OK,
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
                    this.tbUserName.Focus();
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

        private void lblRep_Click( object sender, EventArgs e )
        {
            Process.Start( "https://github.com/dredei/TwitterPhotoDownloader" );
        }
    }
}