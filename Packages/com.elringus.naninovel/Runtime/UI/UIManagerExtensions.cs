using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.UI;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IUIManager"/>.
    /// </summary>
    public static class UIManagerExtensions
    {
        /// <summary>
        /// Attempts to retrieve managed UI of the specified type <typeparamref name="T"/>.
        /// </summary>
        public static bool TryGetUI<T> (this IUIManager manager, out T ui) where T : class, IManagedUI
        {
            return (ui = manager?.GetUI<T>()) != null;
        }

        /// <summary>
        /// Attempts to retrieve managed UI with the specified name.
        /// </summary>
        public static bool TryGetUI (this IUIManager manager, string name, out IManagedUI ui)
        {
            return (ui = manager?.GetUI(name)) != null;
        }

        /// <summary>
        /// Returns managed UI of the specified UI resource name or throws when not available.
        /// </summary>
        public static IManagedUI GetUIOrErr (this IUIManager manager, string name)
        {
            return manager?.GetUI(name) ?? throw new Error($"UI with '{name}' name is not available.");
        }

        /// <summary>
        /// Returns managed UI of the specified type <typeparamref name="T"/> or throws when not available.
        /// </summary>
        public static T GetUIOrErr<T> (this IUIManager manager) where T : class, IManagedUI
        {
            return manager?.GetUI<T>() ?? throw new Error($"UI of '{typeof(T)}' type is not available.");
        }

        /// <summary>
        /// Rents a pooled list and collects all the managed UI instances.
        /// </summary>
        public static IDisposable RentUIs (this IUIManager manager, out List<IManagedUI> uis)
        {
            var rent = ListPool<IManagedUI>.Rent(out uis);
            manager.CollectUIs(uis);
            return rent;
        }

        /// <summary>
        /// Selects <see cref="ScriptableUIBehaviour.FocusObject"/> of the topmost visible managed UI
        /// or null (blurs the focus) when non found.
        /// </summary>
        public static void FocusTop (this IUIManager manager)
        {
            using var _ = manager.RentUIs(out var uis);
            var top = uis.OfType<CustomUI>()
                .Where(ui => ui.Visible && ui.FocusObject && ui.FocusObject.activeInHierarchy)
                .OrderByDescending(ui => ui.SortingOrder).FirstOrDefault();
            EventUtils.Select(top ? top.FocusObject : null);
        }

        /// <summary>
        /// Whether the default font is currently selected.
        /// </summary>
        public static bool IsDefaultFontSelected (this IUIManager manager)
        {
            return string.IsNullOrEmpty(manager.FontName);
        }

        /// <summary>
        /// Whether the default font size is currently selected.
        /// </summary>
        public static bool IsDefaultSizeSelected (this IUIManager manager)
        {
            return manager.FontSize == -1;
        }
    }
}
