using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Naninovel.Metadata;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Naninovel.ReflectionUtils;

namespace Naninovel
{
    /// <summary>
    /// Allows generating metadata to be used in external tools, eg IDE extension.
    /// </summary>
    public static class MetadataGenerator
    {
        /// <summary>
        /// Generates project metadata (actors, resources, custom commands, etc).
        /// </summary>
        public static Project GenerateProjectMetadata ()
        {
            try
            {
                var providers = UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(IMetadataProvider));
                var provider = providers.Count == 1 ? providers[0] :
                    providers.First(t => t != typeof(DefaultMetadataProvider));
                return ((IMetadataProvider)Activator.CreateInstance(provider)).GetMetadata();
            }
            catch (Exception e)
            {
                Engine.Err($"Failed to generate Naninovel project metadata: {e}");
                return new();
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        /// <summary>
        /// Generates metadata for well-known command groups.
        /// </summary>
        public static CommandGroup[] GenerateCommandGroupMetadata ()
        {
            return new[] {
                CommandGroups.Actors,
                CommandGroups.Text,
                CommandGroups.Playback,
                CommandGroups.Branching,
                CommandGroups.Visuals,
                CommandGroups.Audio,
                CommandGroups.UI
            };
        }

        /// <summary>
        /// Generates metadata for all the commands available in the project.
        /// </summary>
        public static Metadata.Command[] GenerateCommandsMetadata ()
        {
            return GenerateCommandsMetadata(Command.CommandTypes.Values);
        }

        /// <summary>
        /// Generates metadata for the specified command types.
        /// </summary>
        /// <param name="commands">Command types to generate metadata for.</param>
        public static Metadata.Command[] GenerateCommandsMetadata (IReadOnlyCollection<Type> commands)
        {
            using var _ = ListPool<Metadata.Command>.Rent(out var commandsMeta);
            foreach (var cmd in commands)
            {
                if (cmd.GetCustomAttributes<Command.IgnoreAttribute>().Any(a => a.ParameterId is null)) continue;
                Compiler.Commands.TryGetValue(cmd.Name, out var locale);
                var commandDoc = new Documentation {
                    Summary = GetAttributeValue<DocAttribute, string>(cmd, 0),
                    Remarks = GetAttributeValue<DocAttribute, string>(cmd, 1),
                    Examples = GetAttributeValue<DocAttribute, string[]>(cmd, 2)
                };
                var metadata = new Metadata.Command {
                    Id = cmd.Name,
                    Alias = !string.IsNullOrWhiteSpace(locale.Alias) ? locale.Alias
                        : GetAttributeValue<Command.AliasAttribute, string>(cmd, 0),
                    Group = ResolveGroupMeta(cmd),
                    Icon = GetAttributeValue<Command.IconAttribute, string>(cmd, 0),
                    Localizable = typeof(Command.ILocalizable).IsAssignableFrom(cmd),
                    Nest = ResolveNestMeta(cmd),
                    Branch = ResolveBranchMeta(cmd),
                    InlineOnly = cmd.GetCustomAttributes<Command.InlineOnlyAttribute>().Any(),
                    Documentation = CreateDocsMeta(
                        !string.IsNullOrWhiteSpace(locale.Summary) ? locale.Summary : commandDoc.Summary,
                        !string.IsNullOrWhiteSpace(locale.Remarks) ? locale.Remarks : commandDoc.Remarks,
                        locale.Examples?.Length > 0 ? locale.Examples : commandDoc.Examples),
                    Parameters = GenerateParametersMetadata(cmd, locale)
                };
                commandsMeta.Add(metadata);
            }
            return commandsMeta.OrderBy(c => string.IsNullOrEmpty(c.Alias) ? c.Id : c.Alias).ToArray();
        }

        /// <summary>
        /// Generated constants metadata based on <see cref="ConstantContextAttribute"/> assigned to the commands
        /// and expression functions in the project.
        /// </summary>
        public static Constant[] GenerateConstantsMetadata ()
        {
            return GenerateConstantsMetadata(Command.CommandTypes.Values, ExpressionFunctions.Resolve());
        }

        /// <summary>
        /// Generated constants metadata based on <see cref="ConstantContextAttribute"/> assigned to the commands
        /// of the specified types and expression functions (enums only).
        /// </summary>
        public static Constant[] GenerateConstantsMetadata (IEnumerable<Type> commands,
            IEnumerable<ExpressionFunction> functions)
        {
            using var _ = SetPool<Type>.Rent(out var enumTypes);
            foreach (var command in commands)
            {
                if (command.GetCustomAttribute<ConstantContextAttribute>() is { EnumType: { } cmdType })
                    enumTypes.Add(cmdType);
                foreach (var param in GetParameterFields(command))
                    if (param.GetCustomAttribute<ConstantContextAttribute>() is { EnumType: { } paramType })
                        enumTypes.Add(paramType);
            }
            foreach (var fn in functions)
            foreach (var param in fn.Method.GetParameters())
                if (param.GetCustomAttribute<ConstantContextAttribute>() is { EnumType: { } paramType })
                    enumTypes.Add(paramType);

            using var __ = ListPool<Constant>.Rent(out var constants);
            foreach (var type in enumTypes)
            {
                var values = Enum.GetNames(type);
                if (Compiler.Constants.TryGetValue(type.Name, out var l10n))
                    for (int i = 0; i < values.Length; i++)
                        if (l10n.Values.FirstOrDefault(v => v.Value.EqualsIgnoreCase(values[i])) is var cv)
                            if (!string.IsNullOrWhiteSpace(cv.Alias))
                                values[i] = cv.Alias;
                constants.Add(new() { Name = type.Name, Values = values });
            }

            var chars = Configuration.GetOrDefault<CharactersConfiguration>();
            constants.Add(CreatePoseConstant(Constants.CharacterType, Constants.WildcardType,
                chars.SharedPoses.Select(p => p.Name)));
            foreach (var kv in chars.Metadata.ToDictionary())
                if (kv.Value.Poses.Count > 0)
                    constants.Add(CreatePoseConstant(Constants.CharacterType, kv.Key,
                        kv.Value.Poses.Select(p => p.Name)));

            var backs = Configuration.GetOrDefault<BackgroundsConfiguration>();
            constants.Add(CreatePoseConstant(Constants.BackgroundType, Constants.WildcardType,
                backs.SharedPoses.Select(p => p.Name)));
            foreach (var kv in backs.Metadata.ToDictionary())
                if (kv.Value.Poses.Count > 0)
                    constants.Add(CreatePoseConstant(Constants.BackgroundType, kv.Key,
                        kv.Value.Poses.Select(p => p.Name)));

            return constants.ToArray();

            Constant CreatePoseConstant (string actorType, string actorId, IEnumerable<string> poses)
            {
                var name = $"Poses/{actorType}/{actorId}";
                return new() { Name = name, Values = poses.ToArray() };
            }
        }

        /// <summary>
        /// Generates metadata for the resources registered via editor and addressable providers.
        /// </summary>
        public static Metadata.Resource[] GenerateResourcesMetadata ()
        {
            using var _ = ListPool<Metadata.Resource>.Rent(out var meta);
            using var __ = Assets.Rent(out var assets);
            foreach (var ass in assets)
                meta.Add(new() { Type = ass.Prefix, Path = ass.Path, AssetId = ass.Guid });
            return meta.DistinctBy(r => HashCode.Combine(r.Path, r.Type, r.AssetId)).ToArray();
        }

        /// <summary>
        /// Generates metadata for the actors registered via editor and addressable providers.
        /// </summary>
        public static Actor[] GenerateActorsMetadata ()
        {
            using var _ = ListPool<Actor>.Rent(out var actors);
            using var __ = Assets.Rent(out var assets);
            var guidByPath = new Dictionary<string, string>();
            foreach (var ass in assets)
                guidByPath[ass.FullPath] = ass.Guid;
            var chars = Configuration.GetOrDefault<CharactersConfiguration>().Metadata.ToDictionary();
            var backs = Configuration.GetOrDefault<BackgroundsConfiguration>().Metadata.ToDictionary();
            var choices = Configuration.GetOrDefault<ChoiceHandlersConfiguration>().Metadata.ToDictionary();
            var printers = Configuration.GetOrDefault<TextPrintersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in chars)
                actors.Add(new Actor {
                    Id = kv.Key,
                    Name = kv.Value.HasName ? kv.Value.DisplayName : "",
                    Type = kv.Value.Loader.PathPrefix,
                    Appearances = FindAppearances(kv.Key, kv.Value.Loader.PathPrefix, kv.Value.Implementation)
                });
            foreach (var kv in backs)
                actors.Add(new Actor {
                    Id = kv.Key,
                    Type = kv.Value.Loader.PathPrefix,
                    Appearances = FindAppearances(kv.Key, kv.Value.Loader.PathPrefix, kv.Value.Implementation)
                });
            foreach (var kv in choices)
                actors.Add(new() { Id = kv.Key, Type = kv.Value.Loader.PathPrefix });
            foreach (var kv in printers)
                actors.Add(new Actor { Id = kv.Key, Type = kv.Value.Loader.PathPrefix });
            return actors.ToArray();

            string[] FindAppearances (string actorId, string pathPrefix, [NotNull] string impl)
            {
                var prefabPath = guidByPath.Keys.FirstOrDefault(p => p.EndsWithOrdinal($"{pathPrefix}/{actorId}"));
                var assetGUID = prefabPath != null ? guidByPath.GetValueOrDefault(prefabPath) : null;
                var assetPath = assetGUID != null ? AssetDatabase.GUIDToAssetPath(assetGUID) : null;
                var prefab = assetPath != null ? AssetDatabase.LoadMainAssetAtPath(assetPath) : null;
                if (impl == typeof(PlaceholderCharacter).AssemblyQualifiedName)
                    return chars[actorId].GetCustomData<PlaceholderCharacterMetadata>()?
                        .PlaceholderAppearances ?? Array.Empty<string>();
                if (impl == typeof(PlaceholderBackground).AssemblyQualifiedName)
                    return backs[actorId].GetCustomData<PlaceholderBackgroundMetadata>()?
                        .PlaceholderAppearances?.Select(a => a.Name).ToArray() ?? Array.Empty<string>();
                if (prefab && impl.Contains("Layered"))
                {
                    var beh = (prefab as GameObject)?.GetComponent<LayeredActorBehaviour>();
                    return beh ? beh.GetCompositionMap().Keys.ToArray() : Array.Empty<string>();
                }
                if (prefab && (impl.Contains("Generic") || impl.Contains("Live2D")))
                {
                    var animator = (prefab as GameObject)?.GetComponentInChildren<Animator>();
                    var controller = animator ? animator.runtimeAnimatorController as AnimatorController : null;
                    return controller
                        ? controller.parameters.Where(p => p.type == AnimatorControllerParameterType.Trigger).Select(p => p.name).ToArray()
                        : Array.Empty<string>();
                }
                #if SPRITE_DICING_AVAILABLE
                if (prefab && impl.Contains("Diced"))
                {
                    return (prefab as SpriteDicing.DicedSpriteAtlas)?.Sprites.Select(s => s.name).ToArray() ?? Array.Empty<string>();
                }
                #endif
                #if NANINOVEL_ENABLE_SPINE
                if (prefab && impl.Contains("Spine"))
                {
                    var skeleton = (prefab as GameObject)?.GetComponentInChildren<Spine.Unity.SkeletonRenderer>();
                    var data = skeleton?.skeletonDataAsset?.GetSkeletonData(true);
                    return data?.Animations?.Select(p => p.Name).ToArray() ?? Array.Empty<string>();
                }
                #endif
                {
                    var multiPrefix = $"{pathPrefix}/{actorId}/";
                    return guidByPath.Keys.Where(p => p.Contains(multiPrefix)).Select(p => p.GetAfter(multiPrefix)).ToArray();
                }
            }
        }

        /// <summary>
        /// Generates metadata for custom variables assigned in configuration menu.
        /// </summary>
        public static string[] GenerateVariablesMetadata ()
        {
            var config = Configuration.GetOrDefault<CustomVariablesConfiguration>();
            return config.PredefinedVariables.Select(p => p.Name).ToArray();
        }

        /// <summary>
        /// Generates metadata for all the expression functions in the project.
        /// </summary>
        public static Function[] GenerateFunctionsMetadata ()
        {
            var functions = ExpressionFunctions.Resolve();
            return GenerateFunctionsMetadata(functions);
        }

        /// <summary>
        /// Generates metadata for specified expression functions.
        /// </summary>
        public static Function[] GenerateFunctionsMetadata (IEnumerable<ExpressionFunction> functions)
        {
            return functions.Select(fn => new Function {
                Name = fn.Id,
                Documentation = CreateDocsMeta(fn.Summary, fn.Remarks, fn.Examples),
                Parameters = fn.Method.GetParameters().Select(GenerateParameterMetadata).ToArray()
            }).ToArray();

            FunctionParameter GenerateParameterMetadata (System.Reflection.ParameterInfo info)
            {
                return new() {
                    Name = info.Name,
                    Type = ResolveParameterType(info.ParameterType),
                    Context = GetContext(info),
                    Variadic = info.IsDefined(typeof(ParamArrayAttribute))
                };
            }

            Metadata.ValueType ResolveParameterType (Type valueType)
            {
                if (valueType.IsArray) valueType = valueType.GetElementType();
                if (valueType == typeof(string)) return Metadata.ValueType.String;
                if (valueType == typeof(bool)) return Metadata.ValueType.Boolean;
                if (valueType == typeof(int)) return Metadata.ValueType.Integer;
                return Metadata.ValueType.Decimal;
            }

            ValueContext GetContext (System.Reflection.ParameterInfo info)
            {
                var attr = info.GetCustomAttribute<ParameterContextAttribute>();
                if (attr is null) return null;
                return new() {
                    Type = attr.Type,
                    SubType = attr.SubType
                };
            }
        }

        private static Parameter[] GenerateParametersMetadata (Type cmd, CommandLocalization locale)
        {
            using var _ = ListPool<Parameter>.Rent(out var result);
            foreach (var fieldInfo in GetParameterFields(cmd))
                if (!IsIgnored(fieldInfo))
                    result.Add(ExtractParameterMetadata(locale, fieldInfo));
            return result.ToArray();

            bool IsIgnored (FieldInfo i) => IsIgnoredViaField(i) || IsIgnoredViaClass(i);
            bool IsIgnoredViaField (FieldInfo i) => i.GetCustomAttribute<Command.IgnoreAttribute>() != null;
            bool IsIgnoredViaClass (FieldInfo i) => i.ReflectedType?.GetCustomAttributes<Command.IgnoreAttribute>().Any(a => a.ParameterId == i.Name) ?? false;
        }

        private static FieldInfo[] GetParameterFields (Type cmd)
        {
            return cmd.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .Where(f => f.FieldType.GetInterface(nameof(ICommandParameter)) != null).ToArray();
        }

        private static Parameter ExtractParameterMetadata (CommandLocalization locale, FieldInfo field)
        {
            var l10n = locale.Parameters?.FirstOrDefault(p => p.Id == field.Name);
            var nullableName = typeof(INullable<>).Name;
            var namedName = typeof(INamed<>).Name;
            var meta = new Parameter {
                Id = field.Name,
                Alias = string.IsNullOrWhiteSpace(l10n?.Alias)
                    ? GetAttributeValue<Command.AliasAttribute, string>(field, 0)
                    : l10n.Value.Alias,
                Required = GetAttributeData<Command.RequiredParameterAttribute>(field) != null,
                Localizable = field.FieldType == typeof(LocalizableTextParameter),
                DefaultValue = GetAttributeValue<Command.ParameterDefaultValueAttribute, string>(field, 0),
                MutuallyExclusiveWith = GetAttributeValues<Command.MutuallyExclusiveWithAttribute, string>(field, 0).ToArray(),
                ValueContext = GetValueContext(field),
                Documentation = CreateDocsMeta(!string.IsNullOrWhiteSpace(l10n?.Summary)
                    ? l10n.Value.Summary
                    : GetAttributeValue<DocAttribute, string>(field, 0), null, null)
            };
            meta.Nameless = meta.Alias == Command.NamelessParameterAlias;
            if (TryResolveValueType(field.FieldType, out var valueType))
                meta.ValueContainerType = ValueContainerType.Single;
            else if (GetInterface(nameof(IEnumerable)) != null) SetListValue();
            else SetNamedValue();
            meta.ValueType = valueType;
            return meta;

            Type GetInterface (string name) => field.FieldType.GetInterface(name);

            Type GetNullableType () => GetInterface(nullableName).GetGenericArguments()[0];

            void SetListValue ()
            {
                var elementType = GetNullableType().GetGenericArguments()[0];
                var namedElementType = elementType.BaseType?.GetGenericArguments()[0];
                if (namedElementType?.GetInterface(nameof(INamedValue)) != null)
                {
                    meta.ValueContainerType = ValueContainerType.NamedList;
                    var namedType = namedElementType.GetInterface(namedName).GetGenericArguments()[0];
                    TryResolveValueType(namedType, out valueType);
                }
                else
                {
                    meta.ValueContainerType = ValueContainerType.List;
                    TryResolveValueType(elementType, out valueType);
                }
            }

            void SetNamedValue ()
            {
                meta.ValueContainerType = ValueContainerType.Named;
                var namedType = GetNullableType().GetInterface(namedName).GetGenericArguments()[0];
                TryResolveValueType(namedType, out valueType);
            }
        }

        private static ValueContext[] GetValueContext (MemberInfo member)
        {
            var valueAttr = FindAttribute(false);
            if (valueAttr is null) return null;
            return FindAttribute(true) is { } namedValueAttr
                ? new[] { GetValue(valueAttr), GetValue(namedValueAttr) }
                : new[] { GetValue(valueAttr) };

            ValueContext GetValue (ParameterContextAttribute attr) =>
                new() { Type = attr.Type, SubType = attr.SubType };
            ParameterContextAttribute FindAttribute (bool namedValue) =>
                FindFieldLevelContext(namedValue) ?? FindClassLevelContext(namedValue);
            ParameterContextAttribute FindClassLevelContext (bool namedValue) =>
                member.ReflectedType?.GetCustomAttributes<ParameterContextAttribute>()
                    .Where(a => a.ParameterId == member.Name).FirstOrDefault(a => OfSingleOrNamed(a, namedValue));
            ParameterContextAttribute FindFieldLevelContext (bool namedValue) =>
                member.GetCustomAttributes<ParameterContextAttribute>().FirstOrDefault(a => OfSingleOrNamed(a, namedValue));
            bool OfSingleOrNamed (ParameterContextAttribute a, bool namedValue) => !namedValue && a.Index < 0 || a.Index == (namedValue ? 1 : 0);
        }

        private static bool TryResolveValueType (Type type, out Metadata.ValueType result)
        {
            var nullableName = typeof(INullable<>).Name;
            var valueTypeName = type.GetInterface(nullableName)?.GetGenericArguments()[0].Name;
            switch (valueTypeName)
            {
                case nameof(String):
                case nameof(NullableString):
                case nameof(LocalizableText):
                    result = Metadata.ValueType.String;
                    return true;
                case nameof(Int32):
                case nameof(NullableInteger):
                    result = Metadata.ValueType.Integer;
                    return true;
                case nameof(Single):
                case nameof(NullableFloat):
                    result = Metadata.ValueType.Decimal;
                    return true;
                case nameof(Boolean):
                case nameof(NullableBoolean):
                    result = Metadata.ValueType.Boolean;
                    return true;
            }
            result = default;
            return false;
        }

        [CanBeNull]
        private static string ResolveGroupMeta (Type cmd)
        {
            if (GetAttributeValue<Command.GroupAttribute, string>(cmd, 0) is { } groupId) return groupId;
            if (cmd.GetCustomAttribute<Command.ActorsGroupAttribute>() != null) return CommandGroups.Actors.Id;
            if (cmd.GetCustomAttribute<Command.TextGroupAttribute>() != null) return CommandGroups.Text.Id;
            if (cmd.GetCustomAttribute<Command.PlaybackGroupAttribute>() != null) return CommandGroups.Playback.Id;
            if (cmd.GetCustomAttribute<Command.BranchingGroupAttribute>() != null) return CommandGroups.Branching.Id;
            if (cmd.GetCustomAttribute<Command.VisualsGroupAttribute>() != null) return CommandGroups.Visuals.Id;
            if (cmd.GetCustomAttribute<Command.AudioGroupAttribute>() != null) return CommandGroups.Audio.Id;
            if (cmd.GetCustomAttribute<Command.UIGroupAttribute>() != null) return CommandGroups.UI.Id;
            return null;
        }

        [CanBeNull]
        private static Nest ResolveNestMeta (Type cmd)
        {
            if (!typeof(Command.INestedHost).IsAssignableFrom(cmd)) return null;
            return new() { Required = cmd.GetCustomAttribute<RequireNestedAttribute>() != null };
        }

        [CanBeNull]
        private static Branch ResolveBranchMeta (Type cmd)
        {
            var branch = cmd.GetCustomAttribute<BranchAttribute>();
            if (branch is null) return null;
            return new() { Traits = branch.Traits, SwitchRoots = branch.SwitchRoots, Endpoint = branch.Endpoint };
        }

        [CanBeNull]
        private static Documentation CreateDocsMeta (string summary, string remarks, string[] examples)
        {
            if (string.IsNullOrWhiteSpace(summary) && string.IsNullOrWhiteSpace(remarks) && examples?.Length > 0)
                return null;
            return new() {
                Summary = FormatContent(summary),
                Remarks = FormatContent(remarks),
                Examples = examples?.Select(e => e.TrimFull()).ToArray()
            };
        }

        private static string FormatContent (string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            content = Regex.Replace(content, @"\[@(\w+?)]", m => $"[@{m.Groups[1].Value}](https://naninovel.com/api/#{m.Groups[1].Value.ToLowerInvariant()})");
            content = Regex.Replace(content, @"(?<!\n)\n(?!\n)", " ");
            content = content.Replace("\n", "<br>");
            content = content.Replace("](/", "](https://naninovel.com/");
            content = content.TrimFull();
            return content;
        }
    }
}
