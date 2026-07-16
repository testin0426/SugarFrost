using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    /// <summary>
    /// Allows adding standard asset references to the <see cref="Assets"/> registry.
    /// </summary>
    public static class StandardAssets
    {
        /// <summary>
        /// Checks whether the assets registry is empty and adds the standard assets if that's the case.
        /// </summary>
        public static void Initialize ()
        {
            // TODO: Remove in 1.22 together with the Legacy/EditorResources.
            if (EditorResources.GetAssetToMigrate() is { } assets)
            {
                Assets.RegisterMany(assets);
                EditorResources.SetMigrated(true);
                return;
            }

            if (Assets.Find(_ => true) is null) Register();
        }

        /// <summary>
        /// Adds the standard assets to the registry.
        /// </summary>
        public static void Register ()
        {
            var assets = new List<Asset>();
            var cfg = default(ActorManagerConfiguration);

            // Make sure all actor configurations are written into assets;
            // otherwise actor records fail to sync with the transient configurations.
            ConfigurationSettings.LoadOrDefaultAndSave<BackgroundsConfiguration>();
            ConfigurationSettings.LoadOrDefaultAndSave<CharactersConfiguration>();

            cfg = ConfigurationSettings.LoadOrDefaultAndSave<TextPrintersConfiguration>();
            assets.AddRange(new[] {
                Actor(cfg, "Prefabs/TextPrinters/Dialogue"),
                Actor(cfg, "Prefabs/TextPrinters/Fullscreen"),
                Actor(cfg, "Prefabs/TextPrinters/Wide"),
                Actor(cfg, "Prefabs/TextPrinters/Chat"),
                Actor(cfg, "Prefabs/TextPrinters/Bubble"),
            });

            cfg = ConfigurationSettings.LoadOrDefaultAndSave<ChoiceHandlersConfiguration>();
            assets.AddRange(new[] {
                Actor(cfg, "Prefabs/ChoiceHandlers/ButtonList"),
                Actor(cfg, "Prefabs/ChoiceHandlers/ButtonArea"),
                Actor(cfg, "Prefabs/ChoiceHandlers/ChatReply"),
            });

            assets.AddRange(new[] {
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/Animate"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/DepthOfField"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/DigitalGlitch"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/Rain"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/ShakeBackground"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/ShakeCamera"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/ShakeCharacter"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/ShakePrinter"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/Snow"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/SunShafts"),
                Asset(SpawnConfiguration.DefaultPathPrefix, "Prefabs/FX/Blur"),
            });

            assets.AddRange(new[] {
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/ClickThroughPanel"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/BacklogUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/CGGalleryUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/ConfirmationUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/ContinueInputUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/ExternalScriptsUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/LoadingUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/MovieUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/RollbackUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/SaveLoadUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/SceneTransitionUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/SettingsUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/TipsUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/TitleUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/VariableInputUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/PauseUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/ToastUI"),
                Asset(UIConfiguration.DefaultUIPathPrefix, "Prefabs/DefaultUI/ScriptNavigatorUI"),
            });

            Assets.RegisterMany(assets);
        }

        private static Asset Actor (ActorManagerConfiguration cfg, string relativePrefabPath)
        {
            var assetPath = $"{PackagePath.PackageRootPath}/{relativePrefabPath}.prefab";
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var actorId = relativePrefabPath.GetAfter("/");
            var actorMeta = cfg.GetMetadataOrDefault(actorId);
            var prefix = actorMeta.Loader.PathPrefix;
            var group = $"{actorMeta.Loader.PathPrefix}/{actorMeta.Guid}";
            return new(guid, prefix, actorId, group);
        }

        private static Asset Asset (string prefix, string relativePrefabPath)
        {
            var assetPath = $"{PackagePath.PackageRootPath}/{relativePrefabPath}.prefab";
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var resourcePath = relativePrefabPath.GetAfter("/");
            return new(assetGuid, prefix, resourcePath);
        }
    }
}
