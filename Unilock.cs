using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Unleasharp {
    /// <summary>
    /// Static class to perform lock operations on live variables by hashcode or unsafe pointers rather than value, 
    /// so mutable variables can be locked between operations even when they are modified inside the lock,
    /// without the need to specifically create empty object{} variables for lockage
    /// </summary>
    public static class Unilock {
        private static object                   __SelfLock   = new object {};
        private static Dictionary<int,  object> HashLocks    = new Dictionary<int,  object>();
        private static Dictionary<long, object> PointerLocks = new Dictionary<long, object>();


        /// <summary>
        /// Perform a variable lock by variable so 
        /// </summary>
        /// <param name="ToLock">Variable to lock by</param>
        /// <param name="Callback">Callback to invoke inside the lock</param>
        public static unsafe void Lock(dynamic ToLock, Action Callback) {
            if (!__HashLock(ToLock, Callback)) {
                __PointerLock(ToLock, Callback);
            }
        }

        /// <summary>
        /// Lock by HashChode
        /// </summary>
        /// <param name="ToLock">Variable to lock by</param>
        /// <param name="Callback">Callback to invoke inside the lock</param>
        private static bool __HashLock(dynamic ToLock, Action Callback) {
            int HashCode = int.MinValue;

            try {
                HashCode = ToLock.GetHashCode();
            }
            catch (Exception e) {
                return false;
            }

            lock(__SelfLock) {
                if (!HashLocks.ContainsKey(HashCode)) {
                    HashLocks[HashCode] = new object { };
                }
            }

            lock (HashLocks[HashCode]) {
                Callback.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Lock by unsafe pointer
        /// </summary>
        /// <param name="ToLock">Variable to lock by</param>
        /// <param name="Callback">Callback to invoke inside the lock</param>
        private static unsafe bool __PointerLock(dynamic ToLock, Action Callback) {
            void* ToLockPointer      = Unsafe.AsPointer(ref ToLock);
            long  ToLockPointerValue = (long) ToLockPointer;

            lock(__SelfLock) {
                if (!PointerLocks.ContainsKey(ToLockPointerValue)) {
                    PointerLocks[ToLockPointerValue] = new object { };
                }
            }
            
            lock (PointerLocks[ToLockPointerValue]) {
                Callback.Invoke();
            }

            return true;
        }


        /// <summary>
        /// Lock by reference
        /// </summary>
        /// <param name="ToLock">Variable to lock by</param>
        /// <param name="Callback">Callback to invoke inside the lock</param>
        private static void __Lock(ref object Lock, Action Callback) {
            lock (Lock) {
                Console.WriteLine($"[{Environment.CurrentManagedThreadId}] Locked");
                Callback.Invoke();
                Console.WriteLine($"[{Environment.CurrentManagedThreadId}] Unlocked");
            }
        }
    }
}
