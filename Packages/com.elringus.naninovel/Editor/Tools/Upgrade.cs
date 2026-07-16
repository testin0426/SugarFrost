using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naninovel.Metadata;
using Naninovel.Syntax;
using UnityEditor;

namespace Naninovel
{
    public static class Upgrade
    {
        [MenuItem(MenuPath.Root + "/Upgrade/v1.20 to v1.21")]
        private static void Upgrade120To121 ()
        {
            if (!EditorUtility.DisplayDialog("Perform upgrade?",
                    "Are you sure you want to perform v1.20-v1.21 upgrade? Script assets will be modified. Make sure to perform a backup before confirming.",
                    "Upgrade", "Cancel")) return;

            // Set default choice button path prefix
            var choiceCfg = Configuration.GetOrDefault<ChoiceHandlersConfiguration>();
            choiceCfg.ChoiceButtonLoader.PathPrefix = ChoiceHandlersConfiguration.DefaultButtonPathPrefix;
            EditorUtility.SetDirty(choiceCfg);

            try
            {
                var meta = new MetadataProvider(MetadataGenerator.GenerateProjectMetadata());
                var errs = new CompileErrorHandler { Errors = new List<CompileError>() };
                var parser = new ScriptParser(new() { Handlers = new() { ErrorHandler = errs } });
                var fmt = new Syntax.ScriptFormatter(meta.Symbols);
                var resolver = new EndpointValueResolver(meta);
                var scriptGuids = ScriptAssets.GetAllGuids();
                var idx = 0;
                foreach (var scriptGuid in scriptGuids)
                {
                    var progress = idx++ / (float)scriptGuids.Count;
                    var path = AssetDatabase.GUIDToAssetPath(scriptGuid);
                    if (string.IsNullOrEmpty(path)) continue;
                    EditorUtility.DisplayProgressBar("Upgrading project to Naninovel v1.21", $"Processing '{path}'...", progress);
                    var text = File.ReadAllText(path);
                    var modified = false;
                    var lines = default(List<IScriptLine>);
                    try { lines = parser.ParseText(text); }
                    catch (Exception e)
                    {
                        Engine.Err($"Failed to upgrade '{path}' script: {e}. Parse errors: {string.Join('\n', errs.Errors.Select(e => e.ToString(path)))}");
                        errs.Errors.Clear();
                        continue;
                    }
                    foreach (var err in errs.Errors)
                        Engine.Err(err.ToString(path));
                    errs.Errors.Clear();

                    for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
                    {
                        var line = lines[lineIdx];
                        UpgradeEndpointSyntax();
                        UpgradeChoices();

                        void UpgradeEndpointSyntax () // 'script.label' -> 'script#label'
                        {
                            if (line is Syntax.CommandLine { Command: { } cmd, Indent: var indent })
                            {
                                for (var pIdx = 0; pIdx < cmd.Parameters.Count; pIdx++)
                                    if (resolver.TryResolve(cmd.Parameters[pIdx], cmd.Identifier, out var value) && value.Contains('.') && !value.Contains("#"))
                                        lines[lineIdx] = new Syntax.CommandLine(EditValue(cmd, pIdx, value.Replace(".", "#")), indent);
                            }
                            else if (line is Syntax.GenericLine { Content: { } content, Indent: var genericIndent })
                            {
                                for (int contentIdx = 0; contentIdx < content.Count; contentIdx++)
                                    if (content[contentIdx] is InlinedCommand { Command: { } c })
                                        for (var pIdx = 0; pIdx < c.Parameters.Count; pIdx++)
                                            if (resolver.TryResolve(c.Parameters[pIdx], c.Identifier, out var value) && value.Contains('.') && !value.Contains("#"))
                                            {
                                                var newContent = content.ToArray();
                                                newContent[contentIdx] = new InlinedCommand(EditValue(c, pIdx, value.Replace(".", "#")));
                                                lines[lineIdx] = new Syntax.GenericLine(newContent, genericIndent);
                                            }
                            }
                        }

                        void UpgradeChoices () // '@choice' -> '@addChoice' (@addChoice works as @choice before)
                        {
                            if (line is Syntax.CommandLine { Command: { Identifier: { Text: "choice" } } choice, Indent: var indent })
                            {
                                lines[lineIdx] = new Syntax.CommandLine(new("addChoice", choice.Parameters), indent);
                                LogChange(line, lines[lineIdx]);
                                modified = true;
                            }
                        }

                        Syntax.Command EditValue (Syntax.Command cmd, int paramIdx, string value)
                        {
                            var p = cmd.Parameters[paramIdx];
                            var paras = cmd.Parameters.ToArray();
                            var newValue = new MixedValue(new IValueComponent[] { new PlainText(value) });
                            paras[paramIdx] = new Syntax.Parameter(p.Identifier, newValue, p.Quoted);
                            LogChange(p, paras[paramIdx]);
                            modified = true;
                            return new Syntax.Command(cmd.Identifier, paras);
                        }

                        void LogChange (object from, object to) =>
                            Engine.Log($"Upgrade: Changed '{from}' to '{to}' at '{StringUtils.BuildAssetLink(path, lineIdx)}'.");
                    }
                    if (modified) File.WriteAllText(path, fmt.Format(lines));
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }
}
