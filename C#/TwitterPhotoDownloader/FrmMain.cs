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
        public FrmMain()
        {
            InitializeComponent();
        }

        public void Work()
        {
            var tDownloader = new TwitterDownloader();
            tDownloader.DownloadPhotos( "beamng", tbSavePath.Text );
        }

        private void btnStart_Click( object sender, EventArgs e )
        {
            Thread thread = new Thread( Work );
            thread.SetApartmentState( ApartmentState.STA );
            thread.Start();
        }
    }
}
