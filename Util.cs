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
    /// <summary>
    /// Computes a hash value for the specified input string using the specified algorithm.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <param name="algorithm">The hashing algorithm to use (e.g., "SHA256", "MD5").</param>
    /// <returns>The hash value as a hexadecimal string, or an empty string if the algorithm is not available.</returns>
    public static string HashString(string input, string algorithm) {
        HashAlgorithm hasher = __GetHasher(algorithm);

        if (hasher != null) {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[]  hashBytes = hasher.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        return string.Empty;
    }

    /// <summary>
    /// Computes hash values for the specified file using multiple hashing algorithms.
    /// </summary>
    /// <param name="input">The path to the file to hash.</param>
    /// <param name="algorithms">An array of hashing algorithms to use (e.g., ["SHA256", "MD5"]).</param>
    /// <param name="bufferSize">The buffer size to use when reading the file (default is 4MB).</param>
    /// <returns>A dictionary mapping algorithm names to their corresponding hash values.</returns>
    public static Dictionary<string, string> HashFile(string input, string[] algorithms, int bufferSize = 4 * 1024 * 1024) {
        using (File.OpenRead(input)) {
            return HashStream(File.OpenRead(input), algorithms, bufferSize);
        }
    }

    /// <summary>
    /// Computes a hash value for the specified file using the specified algorithm.
    /// </summary>
    /// <param name="input">The path to the file to hash.</param>
    /// <param name="algorithm">The hashing algorithm to use (e.g., "SHA256", "MD5").</param>
    /// <param name="bufferSize">The buffer size to use when reading the file (default is 4MB).</param>
    /// <returns>The hash value as a hexadecimal string, or an empty string if the algorithm is not available.</returns>
    public static string HashFile(string input, string algorithm, int bufferSize = 4 * 1024 * 1024) {
        using (File.OpenRead(input)) {
            return HashStream(File.OpenRead(input), algorithm, bufferSize);
        }
    }

    /// <summary>
    /// Computes a hash value for the specified stream using the specified algorithm.
    /// </summary>
    /// <param name="input">The stream to hash.</param>
    /// <param name="algorithm">The hashing algorithm to use (e.g., "SHA256", "MD5").</param>
    /// <param name="bufferSize">The buffer size to use when reading the stream (default is 4MB).</param>
    /// <returns>The hash value as a hexadecimal string, or an empty string if the algorithm is not available.</returns>
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

    /// <summary>
    /// Computes hash values for the specified stream using multiple hashing algorithms.
    /// </summary>
    /// <param name="input">The stream to hash.</param>
    /// <param name="algorithms">An array of hashing algorithms to use (e.g., ["SHA256", "MD5"]).</param>
    /// <param name="bufferSize">The buffer size to use when reading the stream (default is 4MB).</param>
    /// <returns>A dictionary mapping algorithm names to their corresponding hash values.</returns>
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

    /// <summary>
    /// Determines whether the specified type implements the given interface or inherits from the given base type.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <param name="implemented">The interface or base type to check against.</param>
    /// <returns>true if the type implements the interface or inherits from the base type; otherwise, false.</returns>
    public static bool Implements<T>(Type implemented) {
        return implemented.IsAssignableFrom(typeof(T));
    }

    /// <summary>
    /// Determines whether the specified type implements the given interface or inherits from the given base type.
    /// </summary>
    /// <param name="mainType">The type to check.</param>
    /// <param name="implemented">The interface or base type to check against.</param>
    /// <returns>true if the type implements the interface or inherits from the base type; otherwise, false.</returns>
    public static bool Implements(Type mainType, Type implemented) {
        return implemented.IsAssignableFrom(mainType);
    }

    /// <summary>
    /// Gets a list of available hashing algorithms that can be used for hashing operations.
    /// </summary>
    /// <returns>A list of available hashing algorithm names.</returns>
    public static List<string> GetAvailableHashingAlgorithms() {
        __DetectAvailableHashingAlgorithms();

        return _hashAlgorithms.Keys.ToList();
    }

    /// <summary>
    /// Gets a hash algorithm instance by its name.
    /// </summary>
    /// <param name="name">The name of the hash algorithm to retrieve.</param>
    /// <returns>A HashAlgorithm instance, or null if the algorithm is not available.</returns>
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
    
    /// <summary>
    /// Detects and caches all available hashing algorithms in the current domain.
    /// </summary>
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
