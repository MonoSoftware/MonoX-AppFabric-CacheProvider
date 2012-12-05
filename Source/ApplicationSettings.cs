using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using MonoSoftware.Core;

namespace MonoSoftware.MonoX.Caching
{
    /// <summary>
    /// Application settings.
    /// </summary>
    public static class ApplicationSettings
    {
        #region Properties
        private static string _appFabricCacheName = String.Empty;
        /// <summary>
        /// Gets the administrator roles (comma separated list).
        /// </summary>
        public static string AppFabricCacheName
        {
            get
            {
                if (String.IsNullOrEmpty(_appFabricCacheName))
                    try
                    {
                        _appFabricCacheName = WebConfigurationManager.AppSettings["AppFabricCacheName"];
                    }
                    catch {}                    
                return _appFabricCacheName;
            }
        }
        #endregion
    }
}