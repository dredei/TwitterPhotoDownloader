#region Using

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

#endregion

namespace TwitterPhotoDownloader
{

    #region Additional classes

    public class ProgressC
    {
        public int CurrentProgress { get; set; }
        public int MaxProgress { get; set; }
        public int Page { get; set; }
        public ProgressType Type { get; set; }
    }

    public enum ProgressType
    {
        GettingImages,
        DownloadingImages
    }

    #endregion

    public class TwitterDownloader : IDisposable
    {
        public ProgressC Progress;

        private WebBrowser _webBrowser;
        private Timer _loadingTimer;
        private WebClient _webClient;
        private bool _loading;
        private bool _disposed;

        public TwitterDownloader()
        {
            this._webBrowser = new WebBrowser();
            this._webClient = new WebClient();
            this.Progress = new ProgressC();
            this._loadingTimer = new Timer { Interval = 3500 };
            this._loadingTimer.Tick += this._loadingTimer_Tick;
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

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="fileUrl">URL to file</param>
        /// <param name="savePath">Where to save</param>
        private void DownloadFile( string fileUrl, string savePath )
        {
            string fileName = savePath + "\\" + Path.GetFileName( fileUrl.Substring( 0, fileUrl.Length - 6 ) );
            try
            {
                this._webClient.DownloadFile( fileUrl, fileName );
            }
            catch
            {
            }
            finally
            {
                Thread.Sleep( 700 );
            }
        }

        /// <summary>
        /// Get photos from account
        /// </summary>
        /// <param name="username">Twitter username</param>
        /// <returns></returns>
        private List<string> GetPhotos( string username )
        {
            // loading media page
            var photosUrls = new List<string>();
            this._loading = true;
            this._webBrowser.Navigate( "https://twitter.com/" + username + "/media" );
            this._loadingTimer.Start();
            this.WaitForLoading();

            if ( this._webBrowser.Document == null || this._webBrowser.Document.Body == null ||
                 this._webBrowser.Document.Window == null )
            {
                return photosUrls;
            }
            int oldHeight;
            int newHeight;
            int page = 0;
            // scrolling down the page until all photos won't be loaded (oldHeight == newHeight)
            do
            {
                page++;
                this.Progress.Page = page;
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
                if ( attributeValue.IndexOf( ".jpg:large" ) >= 0 )
                {
                    photosUrls.Add( attributeValue );
                }
            }
            return photosUrls;
        }

        public void DownloadPhotos( string username, string savePath )
        {
            if ( savePath[ savePath.Length - 1 ] == '\\' )
            {
                savePath = savePath.Remove( savePath.Length - 1, 1 );
            }
            this.DisableIEImages();
            this.Progress.CurrentProgress = 0;
            this.Progress.Type = ProgressType.GettingImages;
            List<string> photosUrls = this.GetPhotos( username );
            this.Progress.MaxProgress = photosUrls.Count;
            this.Progress.Type = ProgressType.DownloadingImages;
            if ( !Directory.Exists( savePath ) )
            {
                Directory.CreateDirectory( savePath );
            }
            this.EnableIEImages();
            for ( int i = 0; i < photosUrls.Count; i++ )
            {
                this.DownloadFile( photosUrls[ i ], savePath );
                this.Progress.CurrentProgress = i + 1;
            }
            photosUrls.Clear();
        }

        public static string FixUserName( string url )
        {
            var regex = new Regex( "http(s)?://twitter\\.com/(\\w+)(/media)?" );
            Match match = regex.Match( url );
            return match.Success ? match.Groups[ 2 ].ToString() : url;
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using ( var client = new WebClient() )
                {
                    using ( var stream = client.OpenRead( "https://twitter.com/" ) )
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        #region Members

        public void Dispose()
        {
            this.Dispose( true );
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose( bool disposing )
        {
            if ( !this._disposed )
            {
                if ( disposing )
                {
                    if ( this._webBrowser != null )
                    {
                        this._webBrowser.Dispose();
                    }
                    if ( this._webClient != null )
                    {
                        this._webClient.Dispose();
                    }
                    if ( this._loadingTimer != null )
                    {
                        this._loadingTimer.Dispose();
                    }
                }

                // Indicate that the instance has been disposed.
                this._webBrowser = null;
                this._webClient = null;
                this._loadingTimer = null;
                this.Progress = null;
                this._disposed = true;
            }
        }

        #endregion
    }
}