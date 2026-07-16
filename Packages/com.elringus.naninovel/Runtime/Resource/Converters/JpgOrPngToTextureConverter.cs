using System.IO;
using UnityEngine;

namespace Naninovel
{
    public class JpgOrPngToTextureConverter : IResourceConverter
    {
        public bool Supports (string extension)
        {
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
        }

        public Object Convert (byte[] bytes, string fullPath)
        {
            var texture = new Texture2D(2, 2);
            texture.name = Path.GetFileName(fullPath);
            texture.LoadImage(bytes, true);
            return texture;
        }
    }
}
