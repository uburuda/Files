﻿using System;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.UI.Xaml.Media.Imaging;
using Files.Filesystem;
using Files.SettingsInterfaces;
using System.Collections.Generic;
using Windows.Storage.FileProperties;
using Windows.Storage;
using Files.Helpers;
using Windows.UI.Xaml;
using Files.Views;
using Windows.UI.Xaml.Media;

namespace Files.ViewModels.Bundles
{
    public class BundleItemViewModel : ObservableObject, IDisposable
    {
        #region Singleton

        private IBundlesSettings BundlesSettings => App.BundlesSettings;

        #endregion

        #region Private Members

        private IShellPage associatedInstance;

        #endregion

        #region Actions

        public Action<BundleItemViewModel> NotifyItemRemoved { get; set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// The name of a bundle this item is contained within
        /// </summary>
        public string ParentBundleName { get; set; }

        public string Path { get; set; }

        public string Name
        {
            get => System.IO.Path.GetFileName(this.Path);
        }

        public FilesystemItemType TargetType { get; set; } = FilesystemItemType.File;

        private ImageSource icon;
        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public SvgImageSource FolderIcon { get; } = new SvgImageSource()
        {
            RasterizePixelHeight = 128,
            RasterizePixelWidth = 128,
            UriSource = new Uri("ms-appx:///Assets/FolderIcon.svg"),
        };

        public Visibility OpenInNewTabVisibility
        {
            get => TargetType == FilesystemItemType.Directory ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Commands

        public ICommand OpenItemCommand { get; private set; }

        public ICommand OpenInNewTabCommand { get; private set; }

        public ICommand OpenItemLocationCommand { get; private set; }

        public ICommand RemoveItemCommand { get; private set; }

        #endregion

        #region Constructor

        public BundleItemViewModel(IShellPage associatedInstance, string path, FilesystemItemType targetType)
        {
            this.associatedInstance = associatedInstance;
            this.Path = path;
            this.TargetType = targetType;

            // Create commands
            OpenItemCommand = new RelayCommand(OpenItem);
            OpenInNewTabCommand = new RelayCommand(OpenInNewTab);
            OpenItemLocationCommand = new RelayCommand(OpenItemLocation);
            RemoveItemCommand = new RelayCommand(RemoveItem);

            SetIcon();
        }

        #endregion

        #region Command Implementation

        private async void OpenItem()
        {
            await associatedInstance.InteractionOperations.OpenPath(Path, TargetType);
        }

        private async void OpenInNewTab()
        {
            await MainPage.AddNewTabByPathAsync(typeof(PaneHolderPage), Path);
        }

        private async void OpenItemLocation()
        {
            await associatedInstance.InteractionOperations.OpenPath(System.IO.Path.GetDirectoryName(Path), FilesystemItemType.Directory);
        }

        private void RemoveItem()
        {
            if (BundlesSettings.SavedBundles.ContainsKey(ParentBundleName))
            {
                Dictionary<string, List<string>> allBundles = BundlesSettings.SavedBundles; // We need to do it this way for Set() to be called
                allBundles[ParentBundleName].Remove(Path);
                BundlesSettings.SavedBundles = allBundles;
                NotifyItemRemoved(this);
            }
        }

        #endregion

        #region Private Helpers

        private async void SetIcon()
        {
            if (TargetType == FilesystemItemType.Directory) // OpenDirectory
            {
                Icon = FolderIcon;
            }
            else // NotADirectory
            {
                try
                {
                    StorageFile file = await StorageItemHelpers.ToStorageItem<StorageFile>(Path, associatedInstance);

                    if (file == null) // No file found
                    {
                        Icon = new BitmapImage();
                        return;
                    }

                    BitmapImage icon = new BitmapImage();
                    StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.ListView, 24u, ThumbnailOptions.UseCurrentScale);

                    if (thumbnail != null)
                    {
                        await icon.SetSourceAsync(thumbnail);

                        Icon = icon;
                        OnPropertyChanged(nameof(Icon));
                    }
                }
                catch
                {
                    Icon = new BitmapImage(); // Set here no file image
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Path = null;
            Icon = null;

            OpenItemCommand = null;
            OpenInNewTabCommand = null;
            OpenItemLocationCommand = null;
            RemoveItemCommand = null;

            associatedInstance = null;
        }

        #endregion
    }
}
