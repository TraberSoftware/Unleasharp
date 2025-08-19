using System.Linq.Expressions;
using System.Reflection;
using Unleasharp.ExtensionMethods;

namespace Unleasharp {
    public class Renderable {
        public Renderable() { }

        public override string ToString() {
            MethodInfo RenderMethod = this.GetType().GetExtensionMethod("Render");

            if (RenderMethod != null) {
                object Rendered = null;
                switch (true) {
                    case true when RenderMethod.IsStatic:
                        Rendered = RenderMethod.Invoke(null, new object[] { this });
                        break;
                    case true when !RenderMethod.IsStatic:
                        Rendered = RenderMethod.Invoke(this, null);
                        break;
                }

                if (Rendered != null) {
                    return Rendered as string;
                }
            }

            return string.Empty;
        }
    }
}
