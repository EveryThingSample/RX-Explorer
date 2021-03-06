﻿using RX_Explorer.Class;
using RX_Explorer.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using TabViewItem = Microsoft.UI.Xaml.Controls.TabViewItem;

namespace RX_Explorer
{
    public sealed partial class ThisPC : Page
    {
        private Frame Nav;
        private TabViewItem TabItem;
        private QuickStartItem CurrentSelectedItem;
        private StorageFolder OpenTargetFolder;

        public ThisPC()
        {
            InitializeComponent();
            LibraryGrid.ItemsSource = TabViewContainer.ThisPage.LibraryFolderList;
            DeviceGrid.ItemsSource = TabViewContainer.ThisPage.HardDeviceList;
            QuickStartGridView.ItemsSource = TabViewContainer.ThisPage.QuickStartList;
            WebGridView.ItemsSource = TabViewContainer.ThisPage.HotWebList;
            Loaded += ThisPC_Loaded;
        }

        private void ThisPC_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ThisPC_Loaded;

            if (OpenTargetFolder != null)
            {
                if (AnimationController.Current.IsEnableAnimation)
                {
                    Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, OpenTargetFolder, this), new DrillInNavigationTransitionInfo());
                }
                else
                {
                    Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, OpenTargetFolder, this), new SuppressNavigationTransitionInfo());
                }
                OpenTargetFolder = null;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Tuple<TabViewItem, StorageFolder> Parameters)
            {
                Nav = Parameters.Item1.Content as Frame;
                TabItem = Parameters.Item1;
                TabItem.Header = Globalization.GetString("MainPage_PageDictionary_ThisPC_Label");
                OpenTargetFolder = Parameters.Item2;
            }
        }

        private void DeviceGrid_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (AnimationController.Current.IsEnableAnimation)
            {
                ProgressBar ProBar = args.ItemContainer.FindChildOfType<ProgressBar>();
                Storyboard Story = new Storyboard();
                DoubleAnimation Animation = new DoubleAnimation()
                {
                    To = (args.Item as HardDeviceInfo).Percent,
                    From = 0,
                    EnableDependentAnimation = true,
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut },
                    Duration = new TimeSpan(0, 0, 0, 0, 800)
                };
                Storyboard.SetTarget(Animation, ProBar);
                Storyboard.SetTargetProperty(Animation, "Value");
                Story.Children.Add(Animation);
                Story.Begin();
            }
        }

        private void DeviceGrid_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            try
            {
                if (SettingControl.IsInputFromPrimaryButton && (e.OriginalSource as FrameworkElement)?.DataContext is HardDeviceInfo Device)
                {
                    if (AnimationController.Current.IsEnableAnimation)
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Device.Folder, this), new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Device.Folder, this), new SuppressNavigationTransitionInfo());
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private void LibraryGrid_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            try
            {
                if (SettingControl.IsInputFromPrimaryButton && (e.OriginalSource as FrameworkElement)?.DataContext is LibraryFolder Library)
                {
                    if (AnimationController.Current.IsEnableAnimation)
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Library.Folder, this), new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Library.Folder, this), new SuppressNavigationTransitionInfo());
                    }
                }

            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private async void QuickStartGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is QuickStartItem Item && Item.ProtocalUri != null)
            {
                await Launcher.LaunchUriAsync(Item.ProtocalUri);
            }
            else
            {
                QuickStartModifiedDialog dialog = new QuickStartModifiedDialog(QuickStartType.Application);
                _ = await dialog.ShowAsync().ConfigureAwait(true);
            }
        }

        private async void WebGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is QuickStartItem Item && Item.ProtocalUri != null)
            {
                await Launcher.LaunchUriAsync(Item.ProtocalUri);
            }
            else
            {
                QuickStartModifiedDialog dialog = new QuickStartModifiedDialog(QuickStartType.WebSite);
                _ = await dialog.ShowAsync().ConfigureAwait(true);
            }
        }

        private async void AppDelete_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectedItem != null)
            {
                TabViewContainer.ThisPage.QuickStartList.Remove(CurrentSelectedItem);
                await SQLite.Current.DeleteQuickStartItemAsync(CurrentSelectedItem).ConfigureAwait(false);
            }
        }

        private async void AppEdit_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectedItem != null)
            {
                QuickStartModifiedDialog dialog = new QuickStartModifiedDialog(QuickStartType.UpdateApp, CurrentSelectedItem);
                _ = await dialog.ShowAsync().ConfigureAwait(true);
            }
        }

        private async void WebEdit_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectedItem != null)
            {
                QuickStartModifiedDialog dialog = new QuickStartModifiedDialog(QuickStartType.UpdateWeb, CurrentSelectedItem);
                _ = await dialog.ShowAsync().ConfigureAwait(true);
            }
        }

        private async void WebDelete_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectedItem != null)
            {
                TabViewContainer.ThisPage.HotWebList.Remove(CurrentSelectedItem);
                await SQLite.Current.DeleteQuickStartItemAsync(CurrentSelectedItem).ConfigureAwait(false);
            }
        }

        private void QuickStartGridView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is QuickStartItem Item)
                {
                    CurrentSelectedItem = Item;

                    if (Item == null || Item.ProtocalUri == null)
                    {
                        QuickStartGridView.ContextFlyout = null;
                    }
                    else
                    {
                        QuickStartGridView.ContextFlyout = AppFlyout;
                    }
                }
            }
        }

        private void WebGridView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is QuickStartItem Item)
                {
                    CurrentSelectedItem = Item;

                    if (Item == null || Item.ProtocalUri == null)
                    {
                        WebGridView.ContextFlyout = null;
                    }
                    else
                    {
                        WebGridView.ContextFlyout = WebFlyout;
                    }
                }
            }
        }

        private async void Attribute_Click(object sender, RoutedEventArgs e)
        {
            DeviceInfoDialog Dialog = new DeviceInfoDialog(DeviceGrid.SelectedItem as HardDeviceInfo);
            _ = await Dialog.ShowAsync().ConfigureAwait(true);
        }

        private void DeviceGrid_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is HardDeviceInfo Context)
                {
                    DeviceGrid.SelectedIndex = TabViewContainer.ThisPage.HardDeviceList.IndexOf(Context);
                }
                else
                {
                    DeviceGrid.SelectedIndex = -1;
                }
            }
        }

        private void DeviceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceBackground.ContextFlyout = DeviceGrid.SelectedItem != null ? DeviceFlyout : EmptyFlyout;
        }

        private void OpenDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DeviceGrid.SelectedItem is HardDeviceInfo Device)
                {
                    if (AnimationController.Current.IsEnableAnimation)
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Device.Folder, this), new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Device.Folder, this), new SuppressNavigationTransitionInfo());
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private void DeviceGrid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!((e.OriginalSource as FrameworkElement)?.DataContext is HardDeviceInfo))
            {
                DeviceGrid.SelectedIndex = -1;
            }
        }

        private void LibraryGrid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!((e.OriginalSource as FrameworkElement)?.DataContext is LibraryFolder))
            {
                LibraryGrid.SelectedIndex = -1;
            }
        }

        private void Grid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            DeviceGrid.SelectedIndex = -1;
            LibraryGrid.SelectedIndex = -1;
        }

        private void LibraryGrid_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is LibraryFolder Context)
                {
                    LibraryGrid.SelectedItem = Context;
                    if (Context.Source == LibrarySource.UserCustom)
                    {
                        LibraryGrid.ContextFlyout = UserLibraryFlyout;
                    }
                    else
                    {
                        LibraryGrid.ContextFlyout = SystemLibraryFlyout;
                    }
                }
                else
                {
                    LibraryGrid.ContextFlyout = null;
                }
            }
        }

        private void OpenSystemLibrary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LibraryGrid.SelectedItem is LibraryFolder Library)
                {
                    if (AnimationController.Current.IsEnableAnimation)
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Library.Folder, this), new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Library.Folder, this), new SuppressNavigationTransitionInfo());
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private void OpenUserLibrary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LibraryGrid.SelectedItem is LibraryFolder Library)
                {
                    if (AnimationController.Current.IsEnableAnimation)
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Library.Folder, this), new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Library.Folder, this), new SuppressNavigationTransitionInfo());
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private async void RemovePin_Click(object sender, RoutedEventArgs e)
        {
            if (LibraryGrid.SelectedItem is LibraryFolder Library)
            {
                TabViewContainer.ThisPage.LibraryFolderList.Remove(Library);
                await SQLite.Current.DeleteFolderLibraryAsync(Library.Folder.Path).ConfigureAwait(false);
            }
        }

        private async void LibraryProperties_Click(object sender, RoutedEventArgs e)
        {
            if (LibraryGrid.SelectedItem is LibraryFolder Library)
            {
                PropertyDialog Dialog = new PropertyDialog(Library.Folder);
                _ = await Dialog.ShowAsync().ConfigureAwait(true);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            TabViewContainer.ThisPage.HardDeviceList.Clear();

            Dictionary<string, bool> VisibilityMap = await SQLite.Current.GetDeviceVisibilityMapAsync().ConfigureAwait(true);

            foreach (string DriveRootPath in DriveInfo.GetDrives()
                                                      .Where((Drives) => Drives.DriveType == DriveType.Fixed || Drives.DriveType == DriveType.Removable || Drives.DriveType == DriveType.Ram || Drives.DriveType == DriveType.Network)
                                                      .Select((Drive) => Drive.RootDirectory.FullName))
            {
                if (VisibilityMap.ContainsKey(DriveRootPath))
                {
                    if (!VisibilityMap[DriveRootPath])
                    {
                        continue;
                    }
                }

                StorageFolder Device = await StorageFolder.GetFolderFromPathAsync(DriveRootPath);
                if (TabViewContainer.ThisPage.HardDeviceList.All((Drive) => Drive.Folder.Path != Device.Path))
                {
                    BasicProperties Properties = await Device.GetBasicPropertiesAsync();
                    IDictionary<string, object> PropertiesRetrieve = await Properties.RetrievePropertiesAsync(new string[] { "System.Capacity", "System.FreeSpace" });
                    TabViewContainer.ThisPage.HardDeviceList.Add(new HardDeviceInfo(Device, await Device.GetThumbnailBitmapAsync().ConfigureAwait(true), PropertiesRetrieve));
                }
            }

            foreach (string AdditionalDrivePath in VisibilityMap.Where((Item) => Item.Value && TabViewContainer.ThisPage.HardDeviceList.All((Device) => Item.Key != Device.Folder.Path)).Select((Result) => Result.Key))
            {
                try
                {
                    StorageFolder Device = await StorageFolder.GetFolderFromPathAsync(AdditionalDrivePath);

                    BasicProperties Properties = await Device.GetBasicPropertiesAsync();
                    IDictionary<string, object> PropertiesRetrieve = await Properties.RetrievePropertiesAsync(new string[] { "System.Capacity", "System.FreeSpace" });

                    TabViewContainer.ThisPage.HardDeviceList.Add(new HardDeviceInfo(Device, await Device.GetThumbnailBitmapAsync().ConfigureAwait(true), PropertiesRetrieve));
                }
                catch (Exception)
                {
                    await SQLite.Current.SetDeviceVisibilityAsync(AdditionalDrivePath, false).ConfigureAwait(true);
                }
            }
        }

        private void DeviceGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (!SettingControl.IsDoubleClickEnable)
                {
                    if (e.ClickedItem is HardDeviceInfo Device)
                    {
                        if (AnimationController.Current.IsEnableAnimation)
                        {
                            Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Device.Folder, this), new DrillInNavigationTransitionInfo());
                        }
                        else
                        {
                            Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Device.Folder, this), new SuppressNavigationTransitionInfo());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private void LibraryGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (!SettingControl.IsDoubleClickEnable)
                {
                    if (e.ClickedItem is LibraryFolder Library)
                    {
                        if (AnimationController.Current.IsEnableAnimation)
                        {
                            Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Library.Folder, this), new DrillInNavigationTransitionInfo());
                        }
                        else
                        {
                            Nav.Navigate(typeof(FileControl), new Tuple<TabViewItem, StorageFolder, ThisPC>(TabItem, Library.Folder, this), new SuppressNavigationTransitionInfo());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private async void AddDevice_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker Picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.Thumbnail
            };
            Picker.FileTypeFilter.Add("*");

            StorageFolder Device = await Picker.PickSingleFolderAsync();

            if (Device != null)
            {
                if (Device.Path == Path.GetPathRoot(Device.Path))
                {
                    if (TabViewContainer.ThisPage.HardDeviceList.All((Item) => Item.Folder.Path != Device.Path))
                    {
                        BasicProperties Properties = await Device.GetBasicPropertiesAsync();
                        IDictionary<string, object> PropertiesRetrieve = await Properties.RetrievePropertiesAsync(new string[] { "System.Capacity", "System.FreeSpace" });
                        TabViewContainer.ThisPage.HardDeviceList.Add(new HardDeviceInfo(Device, await Device.GetThumbnailBitmapAsync().ConfigureAwait(true), PropertiesRetrieve));
                        await SQLite.Current.SetDeviceVisibilityAsync(Device.Path, true).ConfigureAwait(false);
                    }
                    else
                    {
                        QueueContentDialog Dialog = new QueueContentDialog
                        {
                            Title = Globalization.GetString("Common_Dialog_TipTitle"),
                            Content = Globalization.GetString("QueueDialog_DeviceExist_Content"),
                            CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                        };
                        _ = await Dialog.ShowAsync().ConfigureAwait(true);
                    }
                }
                else
                {
                    QueueContentDialog Dialog = new QueueContentDialog
                    {
                        Title = Globalization.GetString("Common_Dialog_TipTitle"),
                        Content = Globalization.GetString("QueueDialog_DeviceSelectError_Content"),
                        CloseButtonText = Globalization.GetString("Common_Dialog_TipTitle")
                    };
                    _ = await Dialog.ShowAsync().ConfigureAwait(true);
                }
            }
        }

        private async void HideDevice_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceGrid.SelectedItem is HardDeviceInfo Device)
            {
                TabViewContainer.ThisPage.HardDeviceList.Remove(Device);
                await SQLite.Current.SetDeviceVisibilityAsync(Device.Folder.Path, false).ConfigureAwait(false);
            }
        }

        private void QuickStartGridView_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == HoldingState.Started)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is QuickStartItem Item)
                {
                    CurrentSelectedItem = Item;

                    if (Item == null || Item.ProtocalUri == null)
                    {
                        QuickStartGridView.ContextFlyout = null;
                    }
                    else
                    {
                        QuickStartGridView.ContextFlyout = AppFlyout;
                    }
                }
            }
        }

        private void WebGridView_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == HoldingState.Started)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is QuickStartItem Item)
                {
                    CurrentSelectedItem = Item;

                    if (Item == null || Item.ProtocalUri == null)
                    {
                        WebGridView.ContextFlyout = null;
                    }
                    else
                    {
                        WebGridView.ContextFlyout = WebFlyout;
                    }
                }
            }
        }

        private void LibraryGrid_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == HoldingState.Started)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is LibraryFolder Context)
                {
                    LibraryGrid.SelectedItem = Context;
                    if (Context.Source == LibrarySource.UserCustom)
                    {
                        LibraryGrid.ContextFlyout = UserLibraryFlyout;
                    }
                    else
                    {
                        LibraryGrid.ContextFlyout = SystemLibraryFlyout;
                    }
                }
                else
                {
                    LibraryGrid.ContextFlyout = null;
                }
            }
        }

        private void DeviceGrid_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == HoldingState.Started)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is HardDeviceInfo Context)
                {
                    DeviceGrid.SelectedIndex = TabViewContainer.ThisPage.HardDeviceList.IndexOf(Context);
                }
                else
                {
                    DeviceGrid.SelectedIndex = -1;
                }
            }
        }
    }
}
