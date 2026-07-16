using System.IO;
using System.Text;
using UnityEngine;

namespace Naninovel
{
    public class TxtToTextAssetConverter : IResourceConverter
    {
        public bool Supports (string extension)
        {
            return extension == ".txt";
        }

        public Object Convert (byte[] bytes, string fullPath)
        {
            var text = Encoding.UTF8.GetString(bytes);
            var textAsset = new TextAsset(text);
            textAsset.name = Path.GetFileName(fullPath);
            return textAsset;
        }
    }
}
