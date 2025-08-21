using System.Linq.Expressions;
using System.Reflection;
using Unleasharp.ExtensionMethods;
namespace Unleasharp;

/// <summary>
/// Represents a base class that provides rendering capabilities through extension methods.
/// When ToString() is called, it attempts to invoke a Render extension method on the instance.
/// </summary>
public class Renderable {
    /// <summary>
    /// Initializes a new instance of the <see cref="Renderable"/> class.
    /// </summary>
    public Renderable() { }

    /// <summary>
    /// Returns a string representation of this instance by invoking a Render extension method if available.
    /// </summary>
    /// <returns>The rendered string representation, or an empty string if no Render method is found.</returns>
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
