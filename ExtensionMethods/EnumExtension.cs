using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Unleasharp.ExtensionMethods {
    public static class EnumExtension {
        public static string GetDescription(this Enum EnumValue) {
            FieldInfo              Field      = EnumValue.GetType().GetField(EnumValue.ToString());
            DescriptionAttribute[] Attributes = Field.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (Attributes != null && Attributes.Any()) {
                return Attributes.First().Description;
            }

            return EnumValue.ToString();
        }
    }
}
