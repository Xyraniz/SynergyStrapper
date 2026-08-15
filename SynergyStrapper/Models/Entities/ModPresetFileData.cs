using System.Security.Cryptography;
using System.Windows.Markup;

namespace SynergyStrapper.Models.Entities
{
    public class ModPresetFileData
    {
        public string FilePath { get; private set; }

        public string FullFilePath => Path.Combine(Paths.Modifications, FilePath);

        public FileStream FileStream => File.OpenRead(FullFilePath);

        public string ResourceIdentifier { get; private set; }

        public bool IsExternalResource => Path.IsPathRooted(ResourceIdentifier);

        public Stream ResourceStream => IsExternalResource
            ? File.OpenRead(ResourceIdentifier)
            : Resource.GetStream(ResourceIdentifier);

        public byte[] ResourceHash { get; private set; }

        public ModPresetFileData(string contentPath, string resource)
        {
            FilePath = contentPath;
            ResourceIdentifier = resource;

            if (IsExternalResource && !File.Exists(ResourceIdentifier))
            {
                ResourceHash = Array.Empty<byte>();
                return;
            }

            using var stream = ResourceStream;
            ResourceHash = App.MD5Provider.ComputeHash(stream);
        }

        public bool HashMatches()
        {
            if (!File.Exists(FullFilePath))
                return false;

            using var fileStream = FileStream;
            var fileHash = App.MD5Provider.ComputeHash(fileStream);

            byte[] expectedHash;
            if (IsExternalResource)
            {
                if (!File.Exists(ResourceIdentifier))
                    return false;

                using var resourceFileStream = File.OpenRead(ResourceIdentifier);
                expectedHash = App.MD5Provider.ComputeHash(resourceFileStream);
            }
            else
            {
                expectedHash = ResourceHash;
            }

            return fileHash.SequenceEqual(expectedHash);
        }
    }
}
