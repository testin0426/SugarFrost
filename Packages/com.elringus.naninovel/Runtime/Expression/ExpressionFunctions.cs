using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Built-in expression functions.
    /// </summary>
    public static class ExpressionFunctions
    {
        [ExpressionFunction("random")]
        [Doc("Return a random integer number between min [inclusive] and max [inclusive].", examples: "random(0, 100)")]
        public static int Random (int min, int max) => UnityEngine.Random.Range(min, max + 1);

        [ExpressionFunction("random")]
        [Doc("Return a random decimal number between min [inclusive] and max [inclusive].", examples: "random(0.5, 1.5)")]
        public static float Random (float min, float max) => UnityEngine.Random.Range(min, max);

        [ExpressionFunction("random")]
        [Doc("Return a string chosen from one of the specified strings.", examples: "random(\"foo\", \"bar\", \"baz\")")]
        public static string Random (params string[] args) => args.Random();

        [ExpressionFunction("calculateProgress")]
        [Doc("Returns scenario completion ratio, in 0.0 to 1.0 range, where 1.0 means all the script lines were executed at least once.", examples: "calculateProgress()")]
        public static float CalculateProgress ()
        {
            var scriptManager = Engine.GetServiceOrErr<IScriptManager>();
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            if (scriptManager.TotalCommandsCount == 0)
            {
                Engine.Warn("'calculateProgress' expression function was used, while the total number of script commands is zero, which indicates project stats were never updated. The stats are updated automatically on build or manually when running 'Naninovel/Show Project Stats' editor menu.");
                return 0;
            }
            return player.PlayedCommandsCount / (float)scriptManager.TotalCommandsCount;
        }

        [ExpressionFunction("isUnlocked")]
        [Doc("Checks whether an unlockable item with the specified ID is currently unlocked.", examples: "isUnlocked(\"Tips/MyTip\")")]
        public static bool IsUnlocked ([ResourceContext(UnlockablesConfiguration.DefaultPathPrefix)] string id)
        {
            return Engine.GetServiceOrErr<IUnlockableManager>()?.ItemUnlocked(id) ?? false;
        }

        [ExpressionFunction("hasPlayed")]
        [Doc("Checks whether currently played command has ever been played before.", examples: "hasPlayed()")]
        public static bool HasPlayed ()
        {
            if (ExpressionEvaluator.Context?.Parameter is not { Parameter: { PlaybackSpot: { } spot } })
            {
                Engine.Err("Failed to resolve played index: expression parameter context is missing.");
                return false;
            }

            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            using var _ = player.RentTracks(out var tracks);
            foreach (var track in tracks)
                if (track.Playlist?.IndexOf(spot) is { } idx && idx >= 0)
                    return player.HasPlayed(spot.ScriptPath, idx);

            Engine.Err("Failed to resolve played index: script playlist not found.");
            return false;
        }

        [ExpressionFunction("hasPlayed")]
        [Doc("Checks whether script with the specified path has ever been played before.", examples: "hasPlayed(\"MyScript\")")]
        public static bool HasPlayed ([ResourceContext(ScriptsConfiguration.DefaultPathPrefix)] string scriptPath)
        {
            return Engine.GetServiceOrErr<IScriptPlayer>().HasPlayed(scriptPath);
        }

        [ExpressionFunction("getName")]
        [Doc("Returns author name of a character actor with the specified ID.", examples: "getName(\"Kohaku\")")]
        public static string GetName ([ActorContext(CharactersConfiguration.DefaultPathPrefix)] string characterId)
        {
            return Engine.GetServiceOrErr<ICharacterManager>().GetAuthorName(characterId);
        }

        [ExpressionFunction("pow")]
        [Doc("Returns num raised to power.", examples: "pow(2, 3)")]
        public static float Pow (float num, float pow) => Mathf.Pow(num, pow);

        [ExpressionFunction("sqrt")]
        [Doc("Returns square root of num.", examples: "sqrt(2)")]
        public static float Sqrt (float num) => Mathf.Sqrt(num);

        [ExpressionFunction("cos")]
        [Doc("Returns the cosine of angle.", examples: "cos(180)")]
        public static float Cos (float num) => Mathf.Cos(num);

        [ExpressionFunction("sin")]
        [Doc("Returns the sine of angle.", examples: "sin(90)")]
        public static float Sin (float num) => Mathf.Sin(num);

        [ExpressionFunction("log")]
        [Doc("Returns the natural (base e) logarithm of a specified number.", examples: "log(0.5)")]
        public static float Log (float num) => Mathf.Log(num);

        [ExpressionFunction("abs")]
        [Doc("Returns the absolute value of num.", examples: "abs(0.5)")]
        public static float Abs (float num) => Mathf.Abs(num);

        [ExpressionFunction("max")]
        [Doc("Returns largest of two or more values.", examples: "max(1, 10, -9)")]
        public static float Max (params float[] nums) => Mathf.Max(nums.Select(n => n).ToArray());

        [ExpressionFunction("min")]
        [Doc("Returns the smallest of two or more values.", examples: "min(1, 10, -9)")]
        public static float Min (params float[] nums) => Mathf.Min(nums.Select(n => n).ToArray());

        [ExpressionFunction("round")]
        [Doc("Returns num rounded to the nearest integer.", examples: "round(0.9)")]
        public static float Round (float num) => Mathf.Round(num);

        [ExpressionFunction("approx")]
        [Doc("Compares two floating point values and returns true if they are similar.", examples: "approx(0.15, 0.15)")]
        public static bool Approximately (float a, float b) => Mathf.Approximately(a, b);

        [ExpressionFunction("approx")]
        [Doc("Compares two strings ignoring case.", examples: "approx(\"abc\", \"ABC\")")]
        public static bool Approximately (string a, string b) => a.EqualsIgnoreCase(b);

        /// <summary>
        /// Finds expression functions authored in the project.
        /// </summary>
        public static List<ExpressionFunction> Resolve ()
        {
            var functions = new List<ExpressionFunction>();
            foreach (var type in Engine.Types.ExpressionFunctionHosts)
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                if (method.GetCustomAttribute<ExpressionFunctionAttribute>() is { } fn)
                    functions.Add(BuildFunction(method, fn));
            return functions;
        }

        private static ExpressionFunction BuildFunction (MethodInfo method, ExpressionFunctionAttribute fn)
        {
            Compiler.Functions.TryGetValue(method.Name, out var l10n);
            var doc = method.GetCustomAttribute<DocAttribute>();
            var id = !string.IsNullOrWhiteSpace(l10n.Alias) ? l10n.Alias : fn.Alias ?? method.Name;
            return new(id, method, doc?.Summary, doc?.Remarks, doc?.Examples);
        }
    }
}
