using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;

using CommunityToolkit.Mvvm.Input;

using SynergyStrapper.Models.SettingTasks;
using SynergyStrapper.AppData;

namespace SynergyStrapper.UI.ViewModels.Settings
{
    public class ModsViewModel : NotifyPropertyChangedViewModel
    {
        private void OpenModsFolder() => Process.Start("explorer.exe", Paths.Modifications);

        private readonly Dictionary<string, byte[]> FontHeaders = new()
        {
            { "ttf", new byte[4] { 0x00, 0x01, 0x00, 0x00 } },
            { "otf", new byte[4] { 0x4F, 0x54, 0x54, 0x4F } },
            { "ttc", new byte[4] { 0x74, 0x74, 0x63, 0x66 } }
        };

        public ModsViewModel()
        {
            EnsureManagedCustomCursor();
            RefreshCursorPreview();
        }

        private void EnsureManagedCustomCursor()
        {
            string configuredPath = App.Settings.Prop.CustomCursorLocation;

            if (File.Exists(Paths.CustomCursor))
            {
                App.Settings.Prop.CustomCursorLocation = Paths.CustomCursor;
                return;
            }

            if (String.IsNullOrWhiteSpace(configuredPath)
                || String.Equals(configuredPath, Paths.CustomCursor, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(configuredPath))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Paths.Modifications);
                File.Copy(configuredPath, Paths.CustomCursor, true);
                App.Settings.Prop.CustomCursorLocation = Paths.CustomCursor;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::EnsureManagedCustomCursor", ex);
            }
        }

        private void ManageCustomFont()
        {
            if (!String.IsNullOrEmpty(TextFontTask.NewState))
            {
                TextFontTask.NewState = "";
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = $"{Strings.Menu_FontFiles}|*.ttf;*.otf;*.ttc"
                };

                if (dialog.ShowDialog() != true)
                    return;

                string type = dialog.FileName.Substring(dialog.FileName.Length - 3, 3).ToLowerInvariant();

                if (!FontHeaders.ContainsKey(type)
                    || !FontHeaders.Any(x => File.ReadAllBytes(dialog.FileName).Take(4).SequenceEqual(x.Value)))
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Misc_CustomFont_Invalid, MessageBoxImage.Error);
                    return;
                }

                TextFontTask.NewState = dialog.FileName;
            }

            OnPropertyChanged(nameof(ChooseCustomFontVisibility));
            OnPropertyChanged(nameof(DeleteCustomFontVisibility));
        }

        public ICommand OpenModsFolderCommand => new RelayCommand(OpenModsFolder);

        public Visibility ChooseCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DeleteCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Visible : Visibility.Collapsed;

        public ICommand ManageCustomFontCommand => new RelayCommand(ManageCustomFont);

        public ModPresetTask OldAvatarBackgroundTask { get; } = new("OldAvatarBackground", @"ExtraContent\places\Mobile.rbxl", "OldAvatarBackground.rbxl");

        public ModPresetTask OldCharacterSoundsTask { get; } = new("OldCharacterSounds", new()
        {
            { @"content\sounds\action_footsteps_plastic.mp3",           "Sounds.OldWalk.mp3"  },
            { @"content\sounds\action_jump.mp3",                       "Sounds.OldJump.mp3"  },
            { @"content\sounds\action_get_up.mp3",                     "Sounds.OldGetUp.mp3" },
            { @"content\sounds\action_falling.mp3",                    "Sounds.Empty.mp3"    },
            { @"content\sounds\action_jump_land.mp3",                  "Sounds.Empty.mp3"    },
            { @"content\sounds\action_swim.mp3",                       "Sounds.Empty.mp3"    },
            { @"content\sounds\impact_water.mp3",                      "Sounds.Empty.mp3"    }
        });

        public EmojiModPresetTask EmojiFontTask { get; } = new();

        public EnumModPresetTask<Enums.CursorType> CursorTypeTask { get; } = new("CursorType", new()
        {
            {
                Enums.CursorType.From2006, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2006.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2006.ArrowFarCursor.png" }
                }
            },
            {
                Enums.CursorType.From2013, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2013.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2013.ArrowFarCursor.png" }
                }
            },
            {
                Enums.CursorType.CompetitiveCircle, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.Competitive.Circle.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.Competitive.Circle.png" }
                }
            },
            {
                Enums.CursorType.CompetitiveCrosshair, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.Competitive.Crosshair.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.Competitive.Crosshair.png" }
                }
            },
            {
                Enums.CursorType.Custom, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    Paths.CustomCursor },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", Paths.CustomCursor }
                }
            }
        });

        public ImageSource? CursorPreview { get; private set; }

        public string CustomCursorText => File.Exists(Paths.CustomCursor)
            ? $"Managed cursor: {Path.GetFileName(Paths.CustomCursor)}"
            : "No custom cursor selected";

        public void RefreshCursorPreview()
        {
            CursorPreview = CursorTypeTask.NewState switch
            {
                Enums.CursorType.From2006 => LoadResourceImage("Cursor.From2006.ArrowCursor.png"),
                Enums.CursorType.From2013 => LoadResourceImage("Cursor.From2013.ArrowCursor.png"),
                Enums.CursorType.CompetitiveCircle => LoadResourceImage("Cursor.Competitive.Circle.png"),
                Enums.CursorType.CompetitiveCrosshair => LoadResourceImage("Cursor.Competitive.Crosshair.png"),
                Enums.CursorType.Custom => LoadFileImage(Paths.CustomCursor),
                _ => null
            };

            OnPropertyChanged(nameof(CursorPreview));
            OnPropertyChanged(nameof(CustomCursorText));
        }

        private static ImageSource? LoadFileImage(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                using FileStream stream = File.OpenRead(path);
                return LoadImage(stream);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::LoadFileImage", ex);
                return null;
            }
        }

        private static ImageSource? LoadResourceImage(string name)
        {
            try
            {
                using Stream stream = Resource.GetStream(name);
                return LoadImage(stream);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::LoadResourceImage", ex);
                return null;
            }
        }

        private static ImageSource LoadImage(Stream stream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private void ManageCustomCursor()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PNG cursor image (*.png)|*.png",
                Title = "Choose a custom Roblox cursor"
            };

            if (dialog.ShowDialog() != true)
                return;

            ImageSource? preview = LoadFileImage(dialog.FileName);
            if (preview is not BitmapSource bitmap || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
            {
                Frontend.ShowMessageBox("The selected image could not be loaded as a cursor.", MessageBoxImage.Error);
                return;
            }

            if (bitmap.PixelWidth > 512 || bitmap.PixelHeight > 512)
            {
                Frontend.ShowMessageBox("Choose a PNG cursor no larger than 512x512 pixels.", MessageBoxImage.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(Paths.Modifications);
                File.Copy(dialog.FileName, Paths.CustomCursor, true);
                App.Settings.Prop.CustomCursorLocation = Paths.CustomCursor;
                CursorTypeTask.NewState = Enums.CursorType.Custom;
                CursorTypeTask.Execute();
                RefreshCursorPreview();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::ManageCustomCursor", ex);
                Frontend.ShowMessageBox($"The custom cursor could not be applied: {ex.Message}", MessageBoxImage.Error);
            }
        }

        public ICommand ManageCustomCursorCommand => new RelayCommand(ManageCustomCursor);

        public FontModPresetTask TextFontTask { get; } = new();

        private void OpenCompatSettings()
        {
            string path = new RobloxPlayerData().ExecutablePath;

            if (File.Exists(path))
                PInvoke.SHObjectProperties(HWND.Null, SHOP_TYPE.SHOP_FILEPATH, path, "Compatibility");
            else
                Frontend.ShowMessageBox(Strings.Common_RobloxNotInstalled, MessageBoxImage.Error);
        }
    }
}
