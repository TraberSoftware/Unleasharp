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

namespace Unleasharp {
    public static class Util {
        public static List<MethodInfo> GetExtensionMethods(Type T) {
            List<MethodInfo> Methods = new List<MethodInfo>();

            MethodInfo[] Caca = T.GetExtensionMethods();

            foreach (MethodInfo Method in T.GetMethods((BindingFlags)(-1))) {

            }

            return Methods;
        }

        public static MethodInfo GetExtensionMethod(Type T, string MethodName) {
            foreach (MethodInfo Method in GetExtensionMethods(T)) {
                if (Method.Name == MethodName) {
                    return Method;
                }
            }

            return null;
        }


        public static string HashString(string Input, string Algorithm) {
            HashAlgorithm Hasher = __GetHasher(Algorithm);

            if (Hasher != null) {
                byte[] InputBytes = System.Text.Encoding.ASCII.GetBytes(Input);
                byte[]  HashBytes = Hasher.ComputeHash(InputBytes);

                return BitConverter.ToString(HashBytes).Replace("-", "").ToLowerInvariant();
            }

            return string.Empty;
        }

        public static Dictionary<string, string> HashFile(string Input, string[] Algorithms, int BufferSize = 4 * 1024 * 1024) {
            using (File.OpenRead(Input)) {
                return HashStream(File.OpenRead(Input), Algorithms, BufferSize);
            }
        }

        public static string HashFile(string Input, string Algorithm, int BufferSize = 4 * 1024 * 1024) {
            using (File.OpenRead(Input)) {
                return HashStream(File.OpenRead(Input), Algorithm, BufferSize);
            }
        }

        public static string HashStream(Stream Input, string Algorithm, int BufferSize = 4 * 1024 * 1024) {
            HashAlgorithm Hasher = __GetHasher(Algorithm);

            // File reading parameters
            byte[] Buffer     = new byte[BufferSize];
            int    BytesRead  = 0;
            
            // Hash block by block
            while ((BytesRead = Input.Read(Buffer, 0, Buffer.Length)) != 0) {
                Hasher.TransformBlock(Buffer, 0, BytesRead, null, 0);
            }

            // Finalize the hasher
            Hasher.TransformFinalBlock(Buffer, 0, 0);

            return BitConverter.ToString(Hasher.Hash).Replace("-", "").ToLowerInvariant();
        }

        public static Dictionary<string, string> HashStream(Stream Input, string[] Algorithms, int BufferSize = 4 * 1024 * 1024) {
            Dictionary<string, string> Result = new Dictionary<string, string>();

            Dictionary<string, HashAlgorithm> Hashers = new Dictionary<string, HashAlgorithm>();
            foreach (string Algorithm in Algorithms) {
                HashAlgorithm Hasher = __GetHasher(Algorithm);

                if (Hasher != null) {
                    Hashers.Add(Algorithm, Hasher);
                }
            }

            // File reading parameters
            byte[] Buffer     = new byte[BufferSize];
            int    BytesRead  = 0;
            
            // Hash block by block
            while ((BytesRead = Input.Read(Buffer, 0, Buffer.Length)) != 0) {
                foreach (HashAlgorithm Hasher in Hashers.Values) {
                    Hasher.TransformBlock(Buffer, 0, BytesRead, null, 0);
                }
            }

            // Finalize all hashers
            foreach (HashAlgorithm Hasher in Hashers.Values) {
                Hasher.TransformFinalBlock(Buffer, 0, 0);
            }

            foreach (KeyValuePair<string, HashAlgorithm> AlgorithmHasher in Hashers) {
                Result.Add(AlgorithmHasher.Key, BitConverter.ToString(AlgorithmHasher.Value.Hash).Replace("-", "").ToLowerInvariant());
            }

            return Result;
        }

        public static bool Implements<T>(Type Implemented) {
            return Implemented.IsAssignableFrom(typeof(T));
        }

        public static bool Implements(Type MainType, Type Implemented) {
            return Implemented.IsAssignableFrom(MainType);
        }

        public static List<string> GetAvailableHashingAlgorithms() {
            __DetectAvailableHashingAlgorithms();

            return __HashAlgorithms.Keys.ToList();
        }

        private static HashAlgorithm __GetHasher(string Name) {
            __DetectAvailableHashingAlgorithms();

            if (__HashAlgorithms.ContainsKey(Name.ToUpperInvariant())) {
                Type HashAlgorithmType = __HashAlgorithms[Name.ToUpperInvariant()];

                MethodInfo CreateMethod = HashAlgorithmType.GetMethod("Create", new Type[0]);
                if (CreateMethod != null) {
                    try {
                        return (HashAlgorithm)CreateMethod.Invoke(null, null);
                    }
                    catch (Exception e) { }
                }

                try {
                    return (HashAlgorithm)Activator.CreateInstance(HashAlgorithmType);
                }
                catch (Exception e) { }
            }

            return null;
        }

        private static Dictionary<string, Type> __HashAlgorithms = null;
        private static void __DetectAvailableHashingAlgorithms() {
            if (__HashAlgorithms == null) {
                __HashAlgorithms = new Dictionary<string, Type>();

                foreach (Assembly LocalAssembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    foreach (Type LocalAssemblyType in LocalAssembly.GetTypes()) {
                        if (Implements(LocalAssemblyType, typeof(HashAlgorithm))) {
                            if (
                                LocalAssemblyType.Name != "HashAlgorithm"                  // We skip the interface
                                &&
                                LocalAssemblyType.Name != "Implementation"                 // We skip dummy implementations of hashing algorithms
                                &&
                                !Implements(LocalAssemblyType, typeof(KeyedHashAlgorithm)) // We skipped keyed hash algorithms as they need specific keying and can't be generalized
                            ) {
                                __HashAlgorithms.Add(LocalAssemblyType.Name.ToUpperInvariant(), LocalAssemblyType);
                            }
                        }
                    }
                }
                
                // Add custom HashAlgorithm implementations created within the executable
                foreach (Type MainAssemblyType in Assembly.GetEntryAssembly().GetTypes()) {
                    if (Implements(MainAssemblyType, typeof(HashAlgorithm))) {
                        __HashAlgorithms.Add(MainAssemblyType.Name.ToUpperInvariant(), MainAssemblyType);
                    }
                }
            }
        }
    }
}
