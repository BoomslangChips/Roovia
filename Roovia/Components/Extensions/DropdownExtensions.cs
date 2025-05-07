using Roovia.Components.Elements;

namespace Roovia.Components.Extensions
{
    /// <summary>
    /// Extension methods to help with dropdown functionality
    /// </summary>
    public static class DropdownExtensions
    {
        /// <summary>
        /// Creates a convenient helper method to select an item and close the dropdown in one operation
        /// </summary>
        /// <typeparam name="T">The type of the value being selected</typeparam>
        /// <param name="dropdown">The RVDropdown component reference</param>
        /// <param name="value">The value to select</param>
        /// <param name="onSelect">The callback to invoke with the selected value</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task SelectAndClose<T>(this RVDropdown dropdown, T value, Func<T, Task> onSelect)
        {
            if (dropdown != null)
            {
                // First invoke the selection callback
                if (onSelect != null)
                {
                    await onSelect(value);
                }

                // Then close the dropdown
                await dropdown.Close();
            }
        }
    }
}