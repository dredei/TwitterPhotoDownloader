using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace TwitterPhotoDownloader
{
    public class TwitterDownloader
    {
        private WebBrowser _webBrowser;
        private Timer _loadingTimer;
        private bool _loading;

        public TwitterDownloader()
        {
            this._webBrowser = new WebBrowser();
            this._loadingTimer = new Timer { Interval = 3500 };
            this._loadingTimer.Tick += _loadingTimer_Tick;
            this._loading = false;
        }

        private void _loadingTimer_Tick( object sender, EventArgs e )
        {
            this._loadingTimer.Stop();
            this._loading = false;
        }

        #region IE methods

        private void DisableIEImages()
        {
            RegistryKey RegKey = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\Internet Explorer\Main", true );
            if ( RegKey != null )
            {
                RegKey.SetValue( "Display Inline Images", "no" );
            }
        }

        private void EnableIEImages()
        {
            RegistryKey RegKey = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\Internet Explorer\Main", true );
            if ( RegKey != null )
            {
                RegKey.SetValue( "Display Inline Images", "yes" );
            }
        }

        #endregion

        private void WaitForLoading()
        {
            while ( this._loading )
            {
                Application.DoEvents();
            }
        }

        private void DownloadFile( string fileUrl, string savePath )
        {

        }

        private List<string> GetPhotos( string username )
        {
            var photosUrls = new List<string>();
            this._loading = true;
            _webBrowser.Navigate( "https://twitter.com/" + username + "/media" );
            this._loadingTimer.Start();
            this.WaitForLoading();
            if ( this._webBrowser.Document == null || this._webBrowser.Document.Body == null || this._webBrowser.Document.Window == null )
            {
                return photosUrls;
            }
            int oldHeight = 0;
            int newHeight = 0;
            do
            {
                oldHeight = this._webBrowser.Document.Body.ScrollRectangle.Height;
                this._webBrowser.Document.Window.ScrollTo( 0, this._webBrowser.Document.Body.ScrollRectangle.Height );
                this._loading = true;
                this._loadingTimer.Start();
                this.WaitForLoading();
                newHeight = this._webBrowser.Document.Body.ScrollRectangle.Height;
                Application.DoEvents();
            }
            while ( newHeight > oldHeight );

            var elements = this._webBrowser.Document.Body.GetElementsByTagName( "span" );
            foreach ( HtmlElement element in elements )
            {
                string attributeValue = element.GetAttribute( "data-resolved-url-large" );
                if ( attributeValue.IndexOf( ".jpg" ) >= 0 )
                {
                    photosUrls.Add( attributeValue );
                }
            }
            return photosUrls;
        }

        public void DownloadPhotos( string username, string savePath )
        {
            List<string> photosUrls = this.GetPhotos( username );
        }
    }
}
