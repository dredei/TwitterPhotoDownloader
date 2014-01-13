using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TwitterPhotoDownloader
{
    public partial class FrmMain : Form
    {
        private readonly TwitterDownloader _twitterDownloader;

        public FrmMain()
        {
            InitializeComponent();
            this._twitterDownloader = new TwitterDownloader();
        }

        private void Work()
        {
            this._twitterDownloader.DownloadPhotos( tbUserName.Text, tbSavePath.Text );
            MessageBox.Show( "OK" );
        }

        private void btnStart_Click( object sender, EventArgs e )
        {
            Thread thread = new Thread( Work );
            thread.SetApartmentState( ApartmentState.STA );
            thread.Start();
        }
    }
}
