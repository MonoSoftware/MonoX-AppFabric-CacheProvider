using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoSoftware.Core;
using MonoSoftware.Web.Caching;
using Microsoft.ApplicationServer.Caching;
using System.Collections;

namespace MonoSoftware.MonoX.Caching
{
    public class AppFabricCacheProvider : ICacheProvider
    {
        #region Fields
        protected static object padLock = new object();
        protected static object padLockInit = new object();
        protected static object padLockKeys = new object();
        protected static object padLockKeysAction = new object();

        /// <summary>
        /// Default AppFabric CacheName preset from AppSettings.
        /// </summary>
        protected string CacheName = ApplicationSettings.AppFabricCacheName;
        #endregion

        #region Properties
        protected static volatile DataCacheFactory _factory = null;
        protected static volatile DataCache _cache = null;
        /// <summary>
        /// Gets the AppFabric DataCache.
        /// </summary>
        protected virtual DataCache Cache
        {
            get
            {
                if (_cache == null)
                {
                    lock (padLockInit)
                    {
                        if (_cache == null)
                        {
                            DataCacheFactoryConfiguration configuration = new DataCacheFactoryConfiguration();
                            //Disable tracing to avoid informational/verbose messages on the web page
                            DataCacheClientLogManager.ChangeLogLevel(System.Diagnostics.TraceLevel.Off);
                            _factory = new DataCacheFactory(configuration);
                            //Get reference to named cache                             
                            _cache = _factory.GetCache(CacheName);                            
                        }
                    }
                }
                return _cache;
            }
            private set
            {
                _cache = value;
            }
        }

        private static List<string> _keys = null;
        private List<string> Keys
        {
            get
            {
                if (_keys == null)
                {
                    lock (padLockKeys)
                    {
                        if (_keys == null)
                        {
                            _keys = new List<string>();
                        }
                    }
                }
                return _keys;
            }            
        }

        private CacheItemPriorityLevel _priortiy = CacheItemPriorityLevel.Normal;
        /// <summary>
        /// Gets or sets cache item priority level.
        /// <para>
        /// Note: Default is <see cref="CacheItemPriorityLevel.AboveNormal"/>
        /// </para>
        /// </summary>
        public CacheItemPriorityLevel Priortiy
        {
            get
            {
                return _priortiy;
            }
            set
            {
                _priortiy = value;
            }
        } 

        private int _timeout = 0;
        /// <summary>
        /// Gets or sets the cache timeout period in seconds.
        /// <para>
        /// Note: Default is zero.
        /// </para>
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        } 
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        public AppFabricCacheProvider()            
        {
        }         
        #endregion              

        #region Callback Methods
        public DataCacheNotificationDescriptor AddItemLevelCallback(string key, DataCacheOperations filter, DataCacheNotificationCallback clientCallback)
        {
            return this.Cache.AddItemLevelCallback(key, filter, clientCallback);
        }

        public DataCacheNotificationDescriptor AddItemLevelCallback(string key, DataCacheOperations filter, DataCacheNotificationCallback clientCallback, string region)
        {
            return this.Cache.AddItemLevelCallback(key, filter, clientCallback, region);
        }

        public DataCacheNotificationDescriptor AddCacheLevelBulkCallback(DataCacheBulkNotificationCallback clientCallback)
        {
            return this.Cache.AddCacheLevelBulkCallback(clientCallback);
        }

        public DataCacheNotificationDescriptor AddCacheLevelCallback(DataCacheOperations filter, DataCacheNotificationCallback clientCallback)
        {
            return this.Cache.AddCacheLevelCallback(filter, clientCallback);
        }

        public DataCacheNotificationDescriptor AddFailureNotificationCallback(DataCacheFailureNotificationCallback clientCallback)
        {
            return this.Cache.AddFailureNotificationCallback(clientCallback);
        }

        public DataCacheNotificationDescriptor AddRegionLevelCallback(string region, DataCacheOperations filter, DataCacheNotificationCallback clientCallback)
        {
            return this.Cache.AddRegionLevelCallback(region, filter, clientCallback);
        } 
        #endregion

        #region ICacheProvider

        /// <summary>
        /// Stores the item in the repository based on the key.
        /// </summary>
        /// <param name="key">Key of the item that should be stored.</param>
        /// <param name="data">Item data</param>
        public void Store(string key, object data)
        {
            if (this.Timeout > 0 && data != null)
            {                
                TimeSpan expiresOnSlide = TimeSpan.FromSeconds(this.Timeout);
                lock (padLock)
                {
                    Cache.Put(key, data, expiresOnSlide);                    
                }
                lock (padLockKeysAction)
                {
                    if (!Keys.Contains(key))
                        Keys.Add(key);
                }
            }
        }

        /// <summary>
        /// Removes the item from the repository.
        /// </summary>
        /// <param name="key">Key of the item that should be removed.</param>
        public void Remove(string key)
        {
            lock (padLock)
            {
                Cache.Remove(key);                
            }
            lock (padLockKeysAction)
            {
                if (Keys.Contains(key))
                    Keys.Remove(key);
            }
        }

        /// <summary>
        /// Removes all the items from the repository.
        /// </summary>
        /// <param name="key">Key of the item that should be removed.</param>
        public void RemoveAll(string key)
        {            
            List<string> toRemove = new List<string>(Keys.Where(p => p.ToLowerInvariant().StartsWith(key.ToLowerInvariant())));

            foreach (string item in toRemove)
            {
                lock (padLock)
                {
                    Cache.Remove(item);
                }
                lock (padLockKeysAction)
                {
                    if (Keys.Contains(item))
                        Keys.Remove(item);
                }
            }
        }

        /// <summary>
        /// Retrieves the item from the repository.
        /// </summary>
        /// <typeparam name="T">Type of the item to be retrieved.</typeparam>
        /// <param name="key">Key of the item to be retrieved.</param>
        /// <returns>The object from the cache with the key that is passed as a parameter.</returns>
        public T Get<T>(string key)
        {
            object item = Get(key);
            if (item != null)
            {
                return item is T ? (T)item : default(T);
            }
            else
                return default(T);
        }

        /// <summary>
        /// Retrieves the item from the repository.
        /// </summary>
        /// <param name="key">Key of the item to be retrieved.</param>
        /// <returns>The object from the cache with the key that is passed as a parameter.</returns>
        public object Get(string key)
        {
            return Cache[key];
        }  
        #endregion
    }
}
