using System;
using System.Collections.Generic;
using System.Text;

namespace Unleasharp;

public static class Serializer {
    public static T Serialize<T>(object input) {
        return Activator.CreateInstance<T>();
    }
}
