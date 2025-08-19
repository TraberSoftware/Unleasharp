using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp {
    public static class Serializer {
        public static T Serialize<T>(object Input) {
            return Activator.CreateInstance<T>();
        }
    }
}
