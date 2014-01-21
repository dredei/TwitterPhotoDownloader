#region Using

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gecko;
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
        public int Downloaded { get; set; }
        public ProgressType Type { get; set; }
    }

    public enum ProgressType
    {
        GettingImages,
        DownloadingImages
    }

    #endregion

    public sealed class TwitterDownloader : IDisposable
    {
        public ProgressC Progress;
        public List<string> ErrorsLinks;

        private GeckoWebBrowser _webBrowser;
        private Form _justForm;
        private Timer _loadingTimer;
        private WebClient _webClient;
        private bool _loading;
        private bool _disposed;

        public TwitterDownloader()
        {
            try
            {
                Xpcom.Initialize( Application.StartupPath + @"\xulrunner\" );
            }
            catch ( Exception exception )
            {
                throw new Exception( exception.Message );
            }

            this._webBrowser = new GeckoWebBrowser { Dock = DockStyle.Fill };
            this._webClient = new WebClient();
            this.Progress = new ProgressC();
            this._loadingTimer = new Timer { Interval = 6500 };
            this._loadingTimer.Tick += this._loadingTimer_Tick;
            this._loading = false;
            this.ErrorsLinks = new List<string>();

            #region Изврат

            // иначе не работает скролл :(
            this._justForm = new Form();
            this._justForm.Controls.Add( this._webBrowser );

            #endregion
        }

        private void _loadingTimer_Tick( object sender, EventArgs e )
        {
            this._loadingTimer.Stop();
            this._loading = false;
        }

        private async Task WaitForLoadingAsync()
        {
            while ( this._loading )
            {
                await TaskEx.Delay( 500 );
            }
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="fileUrl">URL to file</param>
        /// <param name="savePath">Where to save</param>
        private async Task DownloadFileAsync( string fileUrl, string savePath )
        {
            string fileName = savePath + "\\" + Path.GetFileName( fileUrl.Substring( 0, fileUrl.Length - 6 ) );
            try
            {
                Task task = TaskEx.Run( () => this._webClient.DownloadFileAsync( new Uri( fileUrl ), fileName ) );
                await task;
            }
            catch
            {
                this.ErrorsLinks.Add( fileUrl );
                return;
            }
            this.Progress.Downloaded++;
        }

        /// <summary>
        /// Get photos from account
        /// </summary>
        /// <param name="username">Twitter username</param>
        /// <param name="cancellToken">Cancel token</param>
        /// <returns></returns>
        [STAThread]
        private async Task<List<string>> GetPhotosAsync( string username, CancellationToken cancellToken )
        {
            // loading media page
            var photosUrls = new List<string>();
            this._loading = true;
            this._webBrowser.Navigate( "https://twitter.com/" + username + "/media" );
            this._loadingTimer.Start();
            await this.WaitForLoadingAsync();

            if ( this._webBrowser.Document == null || this._webBrowser.Document.Body == null ||
                 this._webBrowser.Window == null )
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
                oldHeight = this._webBrowser.Document.Body.ScrollHeight;
                this._webBrowser.Window.ScrollTo( 0, this._webBrowser.Document.Body.ScrollHeight );
                this._loading = true;
                this._loadingTimer.Start();
                await this.WaitForLoadingAsync();

                newHeight = this._webBrowser.Document.Body.ScrollHeight;
                Application.DoEvents();
                if ( cancellToken.IsCancellationRequested )
                {
                    cancellToken.ThrowIfCancellationRequested();
                }
            }
            while ( newHeight > oldHeight );

            var elements = this._webBrowser.Document.Body.GetElementsByTagName( "span" );
            foreach ( GeckoElement element in elements )
            {
                string attributeValue = element.GetAttribute( "data-resolved-url-large" );
                if ( attributeValue != null && attributeValue.IndexOf( ".jpg:large" ) >= 0 )
                {
                    photosUrls.Add( attributeValue );
                }
            }
            return photosUrls;
        }

        public async Task DownloadPhotosAsync( string username, string savePath, CancellationToken cancellToken )
        {
            if ( savePath[ savePath.Length - 1 ] == '\\' )
            {
                savePath = savePath.Remove( savePath.Length - 1, 1 );
            }
            this.Progress.CurrentProgress = 0;
            this.Progress.Type = ProgressType.GettingImages;
            List<string> photosUrls = await this.GetPhotosAsync( username, cancellToken );
            this.Progress.MaxProgress = photosUrls.Count;
            this.Progress.Type = ProgressType.DownloadingImages;
            if ( !Directory.Exists( savePath ) )
            {
                Directory.CreateDirectory( savePath );
            }
            for ( int i = 0; i < photosUrls.Count; i++ )
            {
                if ( cancellToken.IsCancellationRequested )
                {
                    cancellToken.ThrowIfCancellationRequested();
                }
                await this.DownloadFileAsync( photosUrls[ i ], savePath );
                this.Progress.CurrentProgress = i + 1;
                await TaskEx.Delay( 2500, cancellToken );
            }
            photosUrls.Clear();
        }

        #region Static methods

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

        #endregion

        #region Members

        public void Dispose()
        {
            this.Dispose( true );
            GC.SuppressFinalize( this );
        }

        private void Dispose( bool disposing )
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
                    if ( this._justForm != null )
                    {
                        this._justForm.Dispose();
                    }
                }

                // Indicate that the instance has been disposed.
                this._webBrowser = null;
                this._webClient = null;
                this._loadingTimer = null;
                this.Progress = null;
                this._justForm = null;
                this._disposed = true;
                this.ErrorsLinks.Clear();
                this.ErrorsLinks = null;
            }
        }

        #endregion
    }
}