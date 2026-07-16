using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Naninovel.PlaceholderBackgroundAppearance;

namespace Naninovel
{
    [ActorResources(null, false)]
    public class PlaceholderBackground : PlaceholderActor<PlaceholderBackgroundBehaviour, BackgroundMetadata>, IBackgroundActor
    {
        protected override string ResourcePath { get; } = "Placeholder/Background";
        protected virtual IReadOnlyDictionary<string, PlaceholderBackgroundAppearance> AppearanceByName { get; }
        protected virtual PlaceholderBackgroundAppearance[] DefaultAppearances { get; } = { PlaceholderBackgroundAppearance.Light, Dark, City, Desert, Snow, Mist, Cosmos };

        public PlaceholderBackground (string id, BackgroundMetadata meta) : base(id, meta)
        {
            meta.PixelsPerUnit = 2048;
            meta.MatchMode = AspectMatchMode.Disable;
            AppearanceByName = meta.GetCustomData<PlaceholderBackgroundMetadata>()?.PlaceholderAppearances?.ToDictionary(a => a.Name, a => a) ?? new();
        }

        public override Awaitable ChangeAppearance (string appearance, Tween tween, Transition? transition = default, AsyncToken token = default)
        {
            Behaviour.SetAppearance(GetPlaceholderAppearance(appearance));
            return base.ChangeAppearance(appearance, tween, transition, token);
        }

        protected virtual PlaceholderBackgroundAppearance GetPlaceholderAppearance (string name) =>
            name != null && AppearanceByName.TryGetValue(name, out var appearance)
                ? appearance : GetDefaultAppearance(name);

        private PlaceholderBackgroundAppearance GetDefaultAppearance (string name)
        {
            if (string.IsNullOrEmpty(name)) return WithName(Dark, name);
            if (name.EqualsIgnoreCase("Black")) return Black;
            if (name.EqualsIgnoreCase("White")) return White;
            foreach (var app in DefaultAppearances)
                if (app.Name.EqualsIgnoreCase(name))
                    return app;
            var index = Mathf.Abs(name.GetHashCode()) % DefaultAppearances.Length;
            return WithName(DefaultAppearances[index], name);
        }
    }
}
