using System;
using System.Configuration;

namespace Unleasharp;

public static class Settings {
    private static Configuration _instance;
    private static object        _instanceLocker = new { };
    private static System.Threading.ReaderWriterLock _locker = new System.Threading.ReaderWriterLock();

    #region Private class members
    private static Configuration __GetInstance(bool reload = false) {
        lock (_instanceLocker) {
            if (_instance == null || reload) {
                _instance = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
        }

        return _instance;
    }

    private static void __Set(Configuration settings) {
        lock (_instanceLocker) {
            _instance = settings;
        }
    }
    private static void __Save() {
        Configuration settings = __GetInstance();

        _locker.AcquireWriterLock(int.MaxValue);
        settings.Save(ConfigurationSaveMode.Modified);
        _locker.ReleaseWriterLock();

        __Set(settings);
    }
    #endregion

    #region Public class functions
    /// <summary>
    /// Quick check of the existence of a given settings key
    /// </summary>
    /// <param name="key">Setting key to check</param>
    /// <returns></returns>
    public static bool Exists(string key) {
        Configuration settings = __GetInstance();

        return Get(key) != default
        ;
    }

    /// <summary>
    /// Retrieve the value of a given configuration Key.
    /// It will try to retrieve the value from environment variables first, then from the *.config file.
    /// </summary>
    /// <typeparam name="T">The data type to return the value of the setting into</typeparam>
    /// <param name="key">The configuration key to retrieve the value from</param>
    /// <returns></returns>
    public static T Get<T>(string key) {
        string value = __GetEnvironmentVariable(key);

        try {
            if (value == null) {
                Configuration settings = __GetInstance();

                if (settings.AppSettings.Settings[key] != null) {
                    value = settings.AppSettings.Settings[key].Value;
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
    /// <param name="key">The configuration key to retrieve the value from</param>
    /// <returns></returns>
    public static string Get(string key) {
        return Get<string>(key);
    }
    #endregion

    /// <summary>
    /// Retrieve config value from the environment variable. 
    /// The <c>Key</c> is case-insensitive unless the key is a combination of lower and upper case letters (SomethingLikeThis).
    /// </summary>
    /// <param name="key">The environment variable to retrieve the value from</param>
    /// <returns></returns>
    public static string __GetEnvironmentVariable(string key) {
        string value = Environment.GetEnvironmentVariable(key);
        if (value == null) {
            value = Environment.GetEnvironmentVariable(key.ToLowerInvariant());
        }
        if (value == null) {
            value = Environment.GetEnvironmentVariable(key.ToUpperInvariant());
        }

        return value;
    }

    /// <summary>
    /// Set the Value of a given configuration Key, permanently storing it 
    /// </summary>
    /// <param name="key">The configuration key to set the value to</param>
    /// <param name="value">The value to set</param>
    public static void Set(string key, string value) {
        Configuration settings = __GetInstance(true);

        if (settings.AppSettings.Settings[key] == null) {
            settings.AppSettings.Settings.Add(key, value);
        }
        settings.AppSettings.Settings[key].Value = value;

        __Set(settings);
        __Save();
    }
}
