using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class AboutWindow : EditorWindow
    {
        public static string InstalledVersion { get => PlayerPrefs.GetString(installedVersionKey); set => PlayerPrefs.SetString(installedVersionKey, value); }

        private const string installedVersionKey = "Naninovel." + nameof(AboutWindow) + "." + nameof(InstalledVersion);
        private const string guideUri = "https://naninovel.com/guide/getting-started.html";
        private const string commandsUri = "https://naninovel.com/api/";
        private const string discordUri = "https://discord.gg/BfkNqem";
        private const string supportUri = "https://naninovel.com/support/";
        private const string reviewUri = "https://assetstore.unity.com/packages/templates/systems/naninovel-visual-novel-engine-135453#reviews";
        private string releaseUri;
        private string versionLabel;

        private const int windowWidth = 328;
        private const int windowHeight = 445;

        private EngineVersion engineVersion;
        private GUIContent logoContent;

        private void OnEnable ()
        {
            engineVersion = EngineVersion.LoadFromResources();
            InstalledVersion = engineVersion.Version;
            var logoPath = Path.Combine(PackagePath.EditorResourcesPath, "NaninovelLogo.png");
            logoContent = new(AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath));
            releaseUri = $"https://{(engineVersion.Preview ? "pre." : "")}naninovel.com/releases/{engineVersion.Version}";
            versionLabel = $"v{engineVersion.Version}-{(engineVersion.Preview ? "preview" : "stable")} build {engineVersion.Build}";
        }

        public void OnGUI ()
        {
            var rect = new Rect(5, 10, windowWidth - 10, windowHeight);
            GUILayout.BeginArea(rect);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logoContent, GUIStyle.none, GUILayout.Width(204), GUILayout.Height(148));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(25);
            GUILayout.FlexibleSpace();
            EditorGUILayout.SelectableLabel(versionLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Release Notes", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Find the list of changes and new features associated with the installed version on the release page.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Release Notes")) Application.OpenURL(releaseUri);

            GUILayout.Space(7);

            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Online Resources", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Please read getting started and command guides. Contact support if you have any issues or questions.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Get Started")) Application.OpenURL(guideUri);
            if (GUILayout.Button("Commands")) Application.OpenURL(commandsUri);
            if (GUILayout.Button("Discord")) Application.OpenURL(discordUri);
            if (GUILayout.Button("Support")) Application.OpenURL(supportUri);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(7);

            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Rate Naninovel", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("We hope you enjoy Naninovel! If you feel like it, please leave a review on the Asset Store.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Review on Asset Store")) Application.OpenURL(reviewUri);

            GUILayout.EndArea();
        }

        internal static void FirstTimeSetup ()
        {
            EditorApplication.delayCall += ExecuteFirstTimeSetup;
        }

        private static void ExecuteFirstTimeSetup ()
        {
            // First time ever launch.
            if (string.IsNullOrWhiteSpace(InstalledVersion))
            {
                // Disable domain reload, otherwise Story Editor crashes Unity editor when entering play mode.
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions =
                    EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
                PlayerSettings.runInBackground = true;

                // Make sure TMPro essentials are imported (otherwise errors on entering play mode).
                if (AssetDatabase.FindAssets("t:TMP_Settings") is not { Length: > 0 })
                    TMP_PackageResourceImporter.ImportResources(true, false, false);

                // Open story editor to pre-fetch binaries.
                StoryEditor.Window.DockWithInspector();
            }

            // Both first-time ever and first time after upgrade launch.
            if (string.IsNullOrWhiteSpace(InstalledVersion) ||
                EngineVersion.LoadFromResources()?.Version != InstalledVersion)
            {
                // Write example scripts.
                if (AssetDatabase.FindAssets("t:Naninovel.Script").Length == 0)
                {
                    Directory.CreateDirectory(PackagePath.ScenarioRoot);
                    File.WriteAllText($"{PackagePath.ScenarioRoot}/Title.nani", TITLE_SCRIPT);
                    File.WriteAllText($"{PackagePath.ScenarioRoot}/Entry.nani", ENTRY_SCRIPT);
                }

                OpenWindow();
            }
        }

        [MenuItem(MenuPath.Root + "/About", priority = 0)]
        private static void OpenWindow ()
        {
            var position = new Rect(100, 100, windowWidth, windowHeight);
            GetWindowWithRect<AboutWindow>(position, true, "About Naninovel", true);
        }

        private const string TITLE_SCRIPT = @"; Played in the title menu.
; Use to set up the menu's appearance—background, music, FX, etc.
; Title script path can be changed in the scripts configuration.

; For example, let's set 'Title' background.
@back Title

; Note that since we don't have a real background resource yet,
; Naninovel renders it as a placeholder with abstract visuals.

; Show the title menu.
@showUI TitleUI

; Click ""NEW GAME"" to play the entry script.
";

        private const string ENTRY_SCRIPT = @"; Played when a new game starts.

; Reveal a 'Mist' background over 1.5 seconds and wait for the animation.
@back Mist.CircleReveal time:1.5 wait!

; Print some text.
New game started.

; Show a placeholder character with 'Felix' ID and 'Excited' appearance.
@char Felix.Excited

; Print text authored by Felix.
Felix: Hello there!

; Hide the dialogue box.
@hidePrinter

; Move Felix and make it look right.
@char Felix pos:15 look:Right

; Print text while changing the author appearance and don't wait CTC.
Felix.Curious: Have you already checked out the getting started guide?[>]

; Show a choice.
@choice ""Sure thing!""
    ; Shake the dialogue box.
    @shake
    Felix.Happy: Awesome! In that case, you probably won't need this brief intro.[-] Feel free to remove it.
@choice ""What's that?""
    ; Shake Felix.
    @shake Felix
    Felix.Surprised: It's the essential guide that walks you through everything you need to get started with Naninovel.[-] You can find it at <b>naninovel.com/guide</b>.
    Felix.Persuasive: I highly recommend taking a look — it'll save you a lot of time.
    Felix: And if you prefer learning by example, you can import the 'Visual Novel' and 'Dialogue Mode' samples directly from Unity's Package Manager.

Felix.Excited: Anyway, I truly hope you'll find Naninovel helpful and enjoyable to use.[-] If you run into any issues or have questions, feel free to reach out to support at <b>naninovel.com/support</b>.

; Hide everything and exit to the title menu.
@hideAll wait!
@title
";
    }
}
