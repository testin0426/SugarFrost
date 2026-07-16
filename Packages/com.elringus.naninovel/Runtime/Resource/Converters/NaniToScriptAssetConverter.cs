using System.Text;
using UnityEngine;

namespace Naninovel
{
    public class NaniToScriptAssetConverter : IResourceConverter
    {
        public bool Supports (string extension)
        {
            return extension == ".nani";
        }

        public Object Convert (byte[] bytes, string fullPath)
        {
            var localPath = fullPath.GetAfterFirst(ScriptsConfiguration.DefaultPathPrefix + "/");
            var scriptText = Encoding.UTF8.GetString(bytes);
            return Compiler.CompileScript(localPath, scriptText);
        }
    }
}
