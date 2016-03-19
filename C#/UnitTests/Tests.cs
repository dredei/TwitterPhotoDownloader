#region Using

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace UnitTests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void DownloadPhotos()
        {
            const int filesCount = 48;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string username = "TwitPhotoDownlo";
            string savePath = baseDir + @"\Photos";
            string arguments = $"username={username} savePath={savePath} autostart=true silent=true";
            if ( Directory.Exists( savePath ) )
            {
                Array.ForEach( Directory.GetFiles( savePath, "*.*" ), File.Delete );
            }
            Process.Start( baseDir + @"\TwitterPhotoDownloader.exe", arguments )?.WaitForExit();

            Assert.IsTrue( Directory.GetFiles( savePath, "*.*" ).Length == filesCount, "Number of files doesn't match" );

            bool isAllTestFileExists = Enumerable
                .Range( 1, filesCount )
                .Select( num => savePath + "\\" + num + ".jpg" )
                .All( File.Exists );
            Assert.IsTrue( isAllTestFileExists );
        }
    }
}