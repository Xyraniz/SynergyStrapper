using System.Reflection;

namespace SynergyStrapper
{
    static class Resource
    {
        static readonly Assembly assembly = Assembly.GetExecutingAssembly();
        static readonly string[] resourceNames = assembly.GetManifestResourceNames();

        public static Stream GetStream(string name)
        {
            string? path = resourceNames.FirstOrDefault(str => str.EndsWith(name, StringComparison.OrdinalIgnoreCase));
            if (path is null)
                throw new System.Resources.MissingManifestResourceException($"Embedded resource '{name}' was not found.");

            return assembly.GetManifestResourceStream(path)
                ?? throw new System.Resources.MissingManifestResourceException($"Embedded resource '{path}' could not be opened.");
        }

        public static async Task<byte[]> Get(string name)
        {
            using var stream = GetStream(name);
            using var memoryStream = new MemoryStream();
            
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public static async Task<string> GetString(string name)
        {
            return Encoding.UTF8.GetString(await Get(name));
        }
    }
}
