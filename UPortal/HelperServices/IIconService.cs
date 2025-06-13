using Microsoft.FluentUI.AspNetCore.Components;

namespace UPortal.HelperServices
{
    public interface IIconService
    {
        /// <summary>
        /// Gets an enumerable list of all available icon names, sorted alphabetically.
        /// </summary>
        IEnumerable<string> GetAvailableIconNames();

        /// <summary>
        /// Gets a FluentUI Icon instance based on its name and specified size.
        /// </summary>
        /// <param name="name">The case-sensitive name of the icon.</param>
        /// <param name="size">The desired icon size (e.g., Size24, Size32).</param>
        /// <returns>An Icon object instance.</returns>
        Icon GetIcon(string name, IconSize size = IconSize.Size24);
    }
}