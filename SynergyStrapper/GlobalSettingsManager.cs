using System.Xml.Linq;
using System.Xml.XPath;

namespace SynergyStrapper
{
    public sealed class GlobalSettingsManager
    {
        private static readonly IReadOnlyDictionary<string, (string Element, string Name)> SettingDefinitions = new Dictionary<string, (string, string)>
        {
            ["Rendering.FramerateCap"] = ("int", "FramerateCap"),
            ["Rendering.SavedQualityLevel"] = ("token", "SavedQualityLevel"),
            ["User.MouseSensitivity"] = ("float", "MouseSensitivity"),
            ["User.VREnabled"] = ("bool", "VREnabled"),
            ["UI.Transparency"] = ("float", "PreferredTransparency"),
            ["UI.ReducedMotion"] = ("bool", "ReducedMotion"),
            ["UI.FontSize"] = ("token", "PreferredTextSize")
        };

        private XDocument? _document;
        private bool _previousReadOnlyState;

        public string FileLocation => Path.Combine(Paths.Roblox, "GlobalBasicSettings_13.xml");

        public bool Loaded => _document is not null;

        public bool Exists => File.Exists(FileLocation);

        public bool Load()
        {
            if (!File.Exists(FileLocation))
                return false;

            try
            {
                _document = XDocument.Load(FileLocation, LoadOptions.PreserveWhitespace);
                _previousReadOnlyState = File.GetAttributes(FileLocation).HasFlag(FileAttributes.ReadOnly);
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("GlobalSettingsManager::Load", ex);
                _document = null;
                return false;
            }
        }

        public string? Get(string key)
        {
            if (!SettingDefinitions.ContainsKey(key))
                return null;

            if (_document is null && !Load())
                return null;

            var (_, name) = SettingDefinitions[key];
            return GetPropertiesElement()?.XPathSelectElement($"*[@name='{name}']")?.Value;
        }

        public bool Set(string key, string value)
        {
            if (!SettingDefinitions.TryGetValue(key, out var definition))
                return false;

            if (_document is null && !Load())
                return false;

            XElement? properties = GetPropertiesElement();
            if (properties is null)
                return false;

            XElement? setting = properties.XPathSelectElement($"*[@name='{definition.Name}']");
            if (setting is null)
            {
                setting = new XElement(definition.Element, new XAttribute("name", definition.Name));
                properties.Add(setting);
            }

            setting.Value = value;
            return true;
        }

        public bool Save()
        {
            if (_document is null)
                return false;

            try
            {
                File.SetAttributes(FileLocation, File.GetAttributes(FileLocation) & ~FileAttributes.ReadOnly);
                _document.Save(FileLocation, SaveOptions.DisableFormatting);
                File.SetAttributes(FileLocation, _previousReadOnlyState
                    ? File.GetAttributes(FileLocation) | FileAttributes.ReadOnly
                    : File.GetAttributes(FileLocation) & ~FileAttributes.ReadOnly);
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("GlobalSettingsManager::Save", ex);
                try
                {
                    File.SetAttributes(FileLocation, _previousReadOnlyState
                        ? File.GetAttributes(FileLocation) | FileAttributes.ReadOnly
                        : File.GetAttributes(FileLocation) & ~FileAttributes.ReadOnly);
                }
                catch
                {
                    // Preserve the original save failure without masking it.
                }

                return false;
            }
        }

        private XElement? GetPropertiesElement()
            => _document?.XPathSelectElement("//Item[@class='UserGameSettings']/Properties");
    }
}
