using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Unleasharp;

/// <summary>
/// Static class to perform lock operations on live variables by hashcode or unsafe pointers rather than value, 
/// so mutable variables can be locked between operations even when they are modified inside the lock,
/// without the need to specifically create empty object{} variables for lockage
/// </summary>
public static class Unilock {
    private static object                   _selfLock   = new object {};
    private static Dictionary<int,  object> _hashLocks    = new Dictionary<int,  object>();
    private static Dictionary<long, object> _pointerLocks = new Dictionary<long, object>();


    /// <summary>
    /// Perform a variable lock by variable so 
    /// </summary>
    /// <param name="toLock">Variable to lock by</param>
    /// <param name="callback">Callback to invoke inside the lock</param>
    public static unsafe void Lock(dynamic toLock, Action callback) {
        if (!__HashLock(toLock, callback)) {
            __PointerLock(toLock, callback);
        }
    }

    /// <summary>
    /// Lock by HashChode
    /// </summary>
    /// <param name="toLock">Variable to lock by</param>
    /// <param name="callback">Callback to invoke inside the lock</param>
    private static bool __HashLock(dynamic toLock, Action callback) {
        int hashCode = int.MinValue;

        try {
            hashCode = toLock.GetHashCode();
        }
        catch (Exception e) {
            return false;
        }

        lock(_selfLock) {
            if (!_hashLocks.ContainsKey(hashCode)) {
                _hashLocks[hashCode] = new object { };
            }
        }

        lock (_hashLocks[hashCode]) {
            callback.Invoke();
        }

        return true;
    }

    /// <summary>
    /// Lock by unsafe pointer
    /// </summary>
    /// <param name="toLock">Variable to lock by</param>
    /// <param name="callback">Callback to invoke inside the lock</param>
    private static unsafe bool __PointerLock(dynamic toLock, Action callback) {
        void* toLockPointer      = Unsafe.AsPointer(ref toLock);
        long  toLockPointerValue = (long) toLockPointer;

        lock(_selfLock) {
            if (!_pointerLocks.ContainsKey(toLockPointerValue)) {
                _pointerLocks[toLockPointerValue] = new object { };
            }
        }
        
        lock (_pointerLocks[toLockPointerValue]) {
            callback.Invoke();
        }

        return true;
    }


    /// <summary>
    /// Lock by reference
    /// </summary>
    /// <param name="ToLock">Variable to lock by</param>
    /// <param name="callback">Callback to invoke inside the lock</param>
    private static void __Lock(ref object @lock, Action callback) {
        lock (@lock) {
            Console.WriteLine($"[{Environment.CurrentManagedThreadId}] Locked");
            callback.Invoke();
            Console.WriteLine($"[{Environment.CurrentManagedThreadId}] Unlocked");
        }
    }
}
