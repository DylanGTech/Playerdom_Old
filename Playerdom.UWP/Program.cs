using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.IO;
using Playerdom.Shared;

namespace Playerdom.Shared
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var factory = new MonoGame.Framework.GameFrameworkViewSource<GameLevel>();
            Windows.ApplicationModel.Core.CoreApplication.Run(factory);
        }
    }
}
