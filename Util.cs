using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Unleasharp.ExtensionMethods;

namespace Unleasharp;

public static class Util {
    public static List<MethodInfo> GetExtensionMethods(Type T) {
        List<MethodInfo> methods = new List<MethodInfo>();

        MethodInfo[] caca = T.GetExtensionMethods();

        foreach (MethodInfo method in T.GetMethods((BindingFlags)(-1))) {

        }

        return methods;
    }

    public static MethodInfo GetExtensionMethod(Type T, string methodName) {
        foreach (MethodInfo method in GetExtensionMethods(T)) {
            if (method.Name == methodName) {
                return method;
            }
        }

        return null;
    }


    public static string HashString(string input, string algorithm) {
        HashAlgorithm hasher = __GetHasher(algorithm);

        if (hasher != null) {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[]  hashBytes = hasher.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        return string.Empty;
    }

    public static Dictionary<string, string> HashFile(string input, string[] algorithms, int bufferSize = 4 * 1024 * 1024) {
        using (File.OpenRead(input)) {
            return HashStream(File.OpenRead(input), algorithms, bufferSize);
        }
    }

    public static string HashFile(string input, string algorithm, int bufferSize = 4 * 1024 * 1024) {
        using (File.OpenRead(input)) {
            return HashStream(File.OpenRead(input), algorithm, bufferSize);
        }
    }

    public static string HashStream(Stream input, string algorithm, int bufferSize = 4 * 1024 * 1024) {
        HashAlgorithm hasher = __GetHasher(algorithm);

        // File reading parameters
        byte[] buffer     = new byte[bufferSize];
        int    bytesRead  = 0;
        
        // Hash block by block
        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) != 0) {
            hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        // Finalize the hasher
        hasher.TransformFinalBlock(buffer, 0, 0);

        return BitConverter.ToString(hasher.Hash).Replace("-", "").ToLowerInvariant();
    }

    public static Dictionary<string, string> HashStream(Stream input, string[] algorithms, int bufferSize = 4 * 1024 * 1024) {
        Dictionary<string, string> result = new Dictionary<string, string>();

        Dictionary<string, HashAlgorithm> hashers = new Dictionary<string, HashAlgorithm>();
        foreach (string algorithm in algorithms) {
            HashAlgorithm hasher = __GetHasher(algorithm);

            if (hasher != null) {
                hashers.Add(algorithm, hasher);
            }
        }

        // File reading parameters
        byte[] buffer     = new byte[bufferSize];
        int    bytesRead  = 0;
        
        // Hash block by block
        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) != 0) {
            foreach (HashAlgorithm hasher in hashers.Values) {
                hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
        }

        // Finalize all hashers
        foreach (HashAlgorithm hasher in hashers.Values) {
            hasher.TransformFinalBlock(buffer, 0, 0);
        }

        foreach (KeyValuePair<string, HashAlgorithm> algorithmHasher in hashers) {
            result.Add(algorithmHasher.Key, BitConverter.ToString(algorithmHasher.Value.Hash).Replace("-", "").ToLowerInvariant());
        }

        return result;
    }

    public static bool Implements<T>(Type implemented) {
        return implemented.IsAssignableFrom(typeof(T));
    }

    public static bool Implements(Type mainType, Type implemented) {
        return implemented.IsAssignableFrom(mainType);
    }

    public static List<string> GetAvailableHashingAlgorithms() {
        __DetectAvailableHashingAlgorithms();

        return _hashAlgorithms.Keys.ToList();
    }

    private static HashAlgorithm __GetHasher(string name) {
        __DetectAvailableHashingAlgorithms();

        if (_hashAlgorithms.ContainsKey(name.ToUpperInvariant())) {
            Type hashAlgorithmType = _hashAlgorithms[name.ToUpperInvariant()];

            MethodInfo createMethod = hashAlgorithmType.GetMethod("Create", new Type[0]);
            if (createMethod != null) {
                try {
                    return (HashAlgorithm)createMethod.Invoke(null, null);
                }
                catch (Exception e) { }
            }

            try {
                return (HashAlgorithm)Activator.CreateInstance(hashAlgorithmType);
            }
            catch (Exception e) { }
        }

        return null;
    }

    private static Dictionary<string, Type> _hashAlgorithms = null;
    private static void __DetectAvailableHashingAlgorithms() {
        if (_hashAlgorithms == null) {
            _hashAlgorithms = new Dictionary<string, Type>();

            foreach (Assembly localAssembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (Type localAssemblyType in localAssembly.GetTypes()) {
                    if (Implements(localAssemblyType, typeof(HashAlgorithm))) {
                        if (
                            localAssemblyType.Name != "HashAlgorithm"                  // We skip the interface
                            &&
                            localAssemblyType.Name != "Implementation"                 // We skip dummy implementations of hashing algorithms
                            &&
                            !Implements(localAssemblyType, typeof(KeyedHashAlgorithm)) // We skipped keyed hash algorithms as they need specific keying and can't be generalized
                        ) {
                            _hashAlgorithms.Add(localAssemblyType.Name.ToUpperInvariant(), localAssemblyType);
                        }
                    }
                }
            }
            
            // Add custom HashAlgorithm implementations created within the executable
            foreach (Type mainAssemblyType in Assembly.GetEntryAssembly().GetTypes()) {
                if (Implements(mainAssemblyType, typeof(HashAlgorithm))) {
                    _hashAlgorithms.Add(mainAssemblyType.Name.ToUpperInvariant(), mainAssemblyType);
                }
            }
        }
    }
}
