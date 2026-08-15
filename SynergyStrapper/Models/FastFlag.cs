using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SynergyStrapper
{
    public class FastFlag : INotifyPropertyChanged
    {
        private string _name = String.Empty;
        private string _value = String.Empty;
        private IReadOnlyList<string> _tags = Array.Empty<string>();
        private bool _hasPreset;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                if (String.Equals(_name, value, StringComparison.Ordinal))
                    return;

                _name = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (String.Equals(_value, value, StringComparison.Ordinal))
                    return;

                _value = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<string> Tags
        {
            get => _tags;
            private set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        public bool HasPreset
        {
            get => _hasPreset;
            private set
            {
                if (_hasPreset == value)
                    return;

                _hasPreset = value;
                OnPropertyChanged();
            }
        }

        public void UpdateMetadata(IReadOnlyList<string> tags, bool hasPreset)
        {
            Tags = tags;
            HasPreset = hasPreset;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
