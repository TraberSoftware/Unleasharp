using System;
using System.Configuration;

namespace Unleasharp {
    public static class Settings {
        private static Configuration __Instance;
        private static object        __InstanceLocker = new { };
        private static System.Threading.ReaderWriterLock __Locker = new System.Threading.ReaderWriterLock();

        #region Private class members
        private static Configuration __GetInstance(bool Reload = false) {
            lock (__InstanceLocker) {
                if (__Instance == null || Reload) {
                    __Instance = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                }
            }

            return __Instance;
        }

        private static void __Set(Configuration Settings) {
            lock (__InstanceLocker) {
                __Instance = Settings;
            }
        }
        private static void __Save() {
            Configuration Settings = __GetInstance();

            __Locker.AcquireWriterLock(int.MaxValue);
            Settings.Save(ConfigurationSaveMode.Modified);
            __Locker.ReleaseWriterLock();

            __Set(Settings);
        }
        #endregion

        #region Public class functions
        /// <summary>
        /// Quick check of the existence of a given settings key
        /// </summary>
        /// <param name="Key">Setting key to check</param>
        /// <returns></returns>
        public static bool Exists(string Key) {
            Configuration Settings = __GetInstance();

            return Get(Key) != default
            ;
        }

        /// <summary>
        /// Retrieve the value of a given configuration Key.
        /// It will try to retrieve the value from environment variables first, then from the *.config file.
        /// </summary>
        /// <typeparam name="T">The data type to return the value of the setting into</typeparam>
        /// <param name="Key">The configuration key to retrieve the value from</param>
        /// <returns></returns>
        public static T Get<T>(string Key) {
            string value = __GetEnvironmentVariable(Key);

            try {
                if (value == null) {
                    Configuration Settings = __GetInstance();

                    if (Settings.AppSettings.Settings[Key] != null) {
                        value = Settings.AppSettings.Settings[Key].Value;
                    }
                }

                if (value != null) {
                    return (T)Convert.ChangeType(
                        value,
                        typeof(T)
                    );
                }
            }
            catch { }

            return default(T);
        }

        /// <summary>
        /// Retrieve the value of a given configuration Key as a string
        /// It will try to retrieve the value from environment variables first, then from the *.config file.
        /// </summary>
        /// <param name="Key">The configuration key to retrieve the value from</param>
        /// <returns></returns>
        public static string Get(string Key) {
            return Get<string>(Key);
        }
        #endregion

        /// <summary>
        /// Retrieve config value from the environment variable. 
        /// The <c>Key</c> is case-insensitive unless the key is a combination of lower and upper case letters (SomethingLikeThis).
        /// </summary>
        /// <param name="Key">The environment variable to retrieve the value from</param>
        /// <returns></returns>
        public static string __GetEnvironmentVariable(string Key) {
            string value = Environment.GetEnvironmentVariable(Key);
            if (value == null) {
                value = Environment.GetEnvironmentVariable(Key.ToLowerInvariant());
            }
            if (value == null) {
                value = Environment.GetEnvironmentVariable(Key.ToUpperInvariant());
            }

            return value;
        }

        /// <summary>
        /// Set the Value of a given configuration Key, permanently storing it 
        /// </summary>
        /// <param name="Key">The configuration key to set the value to</param>
        /// <param name="Value">The value to set</param>
        public static void Set(string Key, string Value) {
            Configuration Settings = __GetInstance(true);

            if (Settings.AppSettings.Settings[Key] == null) {
                Settings.AppSettings.Settings.Add(Key, Value);
            }
            Settings.AppSettings.Settings[Key].Value = Value;

            __Set(Settings);
            __Save();
        }
    }
}
