using System.Linq.Expressions;
using System.Reflection;
using Unleasharp.ExtensionMethods;

namespace Unleasharp;

public class Renderable {
    public Renderable() { }

    public override string ToString() {
        MethodInfo renderMethod = this.GetType().GetExtensionMethod("Render");

        if (renderMethod != null) {
            object rendered = null;
            switch (true) {
                case true when renderMethod.IsStatic:
                    rendered = renderMethod.Invoke(null, new object[] { this });
                    break;
                case true when !renderMethod.IsStatic:
                    rendered = renderMethod.Invoke(this, null);
                    break;
            }

            if (rendered != null) {
                return rendered as string;
            }
        }

        return string.Empty;
    }
}
