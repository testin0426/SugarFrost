using System;

namespace Naninovel
{
    [Serializable]
    public class PlaceholderCharacterMetadata : CustomMetadata<PlaceholderCharacter>
    {
        public string[] PlaceholderAppearances = Array.Empty<string>();

        public static string[] GetDefaultAppearances () => new[] {
            "Neutral",
            "Happy",
            "Sad",
            "Angry",
            "Surprised",
            "Confused",
            "Excited",
            "Shy",
            "Embarrassed",
            "Thinking",
            "Serious",
            "Sleepy",
            "Tired",
            "Worried",
            "Scared",
            "Laughing",
            "Crying",
            "Blushing",
            "Determined"
        };
    }
}
