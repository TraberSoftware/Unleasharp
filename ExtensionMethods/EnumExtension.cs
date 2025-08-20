using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Unleasharp.ExtensionMethods;

public static class EnumExtension {
    public static string GetDescription(this Enum enumValue) {
        FieldInfo              field      = enumValue.GetType().GetField(enumValue.ToString());
        DescriptionAttribute[] attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

        if (attributes != null && attributes.Any()) {
            return attributes.First().Description;
        }

        return enumValue.ToString();
    }
}
