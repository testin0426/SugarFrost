using System;

namespace Naninovel
{
    [Serializable]
    public class PlaceholderBackgroundMetadata : CustomMetadata<PlaceholderBackground>
    {
        public PlaceholderBackgroundAppearance[] PlaceholderAppearances = Array.Empty<PlaceholderBackgroundAppearance>();
    }
}
