﻿#region Using

using System;
using System.Windows.Forms;

#endregion

namespace TwitterPhotoDownloader
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        private static void Main( string[] args )
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            Application.Run( new FrmMain( args ) );
        }
    }
}