using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.ExtensionMethods {
    public static class Object {
        public static bool TryConvert<T>(this object input, out T converted) {
            converted = default (T);

            if (input != null) {
                try {
                    converted = (T)Convert.ChangeType(input, typeof(T));
                    return true;
                }
                catch (Exception ex) { }
            }

            return false;
        }

        public static bool IsNull(this object input) {
            return
                input == null
                ||
                input == default
            ;
        }
    }
}
