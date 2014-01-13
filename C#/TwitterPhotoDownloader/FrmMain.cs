#region Using

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

#endregion

namespace TwitterPhotoDownloader
{
    public partial class FrmMain : Form
    {
        private readonly TwitterDownloader _twitterDownloader;

        public FrmMain()
        {
            this.InitializeComponent();
            this._twitterDownloader = new TwitterDownloader();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void Work()
        {
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
    }
}