﻿using RX_Explorer.Class;
using RX_Explorer.Dialog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using AnimationController = RX_Explorer.Class.AnimationController;

namespace RX_Explorer
{
    public sealed partial class SettingControl : UserControl
    {
        private ObservableCollection<FeedBackItem> FeedBackCollection = new ObservableCollection<FeedBackItem>();

        private ObservableCollection<BackgroundPicture> PictureList = new ObservableCollection<BackgroundPicture>();

        private readonly string UserName = ApplicationData.Current.LocalSettings.Values["SystemUserName"].ToString();

        private readonly string UserID = ApplicationData.Current.LocalSettings.Values["SystemUserID"].ToString();

        public static bool IsDoubleClickEnable { get; set; } = true;

        public static bool IsDetachTreeViewAndPresenter { get; set; } = false;

        public static bool IsQuicklookAvailable { get; set; }

        public static bool IsQuicklookEnable { get; set; }

        public static bool IsInputFromPrimaryButton { get; set; } = true;

        private int EnterAndExitLock = 0;

        public bool IsOpened { get; private set; } = false;

        public SettingControl()
        {
            InitializeComponent();

            Version.Text = string.Format("Version: {0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

            EmptyFeedBack.Text = Globalization.GetString("Progress_Tip_Loading");

            Loading += SettingPage_Loading;
            Loaded += SettingPage_Loaded;
        }

        public async Task ExcuteWhenShown()
        {
            AutoBoot.Toggled -= AutoBoot_Toggled;

            switch ((await StartupTask.GetAsync("RXExplorer")).State)
            {
                case StartupTaskState.DisabledByPolicy:
                case StartupTaskState.DisabledByUser:
                case StartupTaskState.Disabled:
                    {
                        AutoBoot.IsOn = false;
                        break;
                    }
                default:
                    {
                        AutoBoot.IsOn = true;
                        break;
                    }
            }

            AutoBoot.Toggled += AutoBoot_Toggled;


            await Task.Delay(1000).ConfigureAwait(true);

            if (PictureMode.IsChecked.GetValueOrDefault() && PictureGirdView.SelectedItem != null)
            {
                PictureGirdView.ScrollIntoViewSmoothly(PictureGirdView.SelectedItem);
            }
        }

        public async Task Show()
        {
            if (!IsOpened && Interlocked.Exchange(ref EnterAndExitLock, 1) == 0)
            {
                IsOpened = true;

                Visibility = Visibility.Visible;

                if (AnimationController.Current.IsEnableAnimation)
                {
                    Scroll.ChangeView(null, 0, null, true);

                    ActivateAnimation(Gr, TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(200), 200, false);
                    ActivateAnimation(LeftPanel, TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(300), 150, false);
                    ActivateAnimation(RightPanel, TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(300), 150, false);
                }

                await ExcuteWhenShown().ConfigureAwait(false);
            }
        }

        public async Task Hide()
        {
            if (IsOpened)
            {
                IsOpened = false;

                if (AnimationController.Current.IsEnableAnimation)
                {
                    ActivateAnimation(LeftPanel, TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(200), 150, true);
                    ActivateAnimation(RightPanel, TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(200), 150, true);
                    ActivateAnimation(Gr, TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(300), 200, true);

                    await Task.Delay(1400).ConfigureAwait(true);
                }

                Visibility = Visibility.Collapsed;

                _ = Interlocked.Exchange(ref EnterAndExitLock, 0);
            }
        }

        private void ActivateAnimation(UIElement Element, TimeSpan Duration, TimeSpan DelayTime, float VerticalOffset, bool IsReverse)
        {
            Visual Visual = ElementCompositionPreview.GetElementVisual(Element);

            Vector3KeyFrameAnimation EntranceAnimation = Visual.Compositor.CreateVector3KeyFrameAnimation();
            ScalarKeyFrameAnimation FadeAnimation = Visual.Compositor.CreateScalarKeyFrameAnimation();

            EntranceAnimation.Target = nameof(Visual.Offset);
            EntranceAnimation.InsertKeyFrame(0, new Vector3(Visual.Offset.X, VerticalOffset, Visual.Offset.Z));
            EntranceAnimation.InsertKeyFrame(1, new Vector3(Visual.Offset.X, 0, Visual.Offset.Z), Visual.Compositor.CreateCubicBezierEasingFunction(new Vector2(.1f, .9f), new Vector2(.2f, 1)));
            EntranceAnimation.Duration = Duration;
            EntranceAnimation.DelayTime = DelayTime;
            EntranceAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

            FadeAnimation.Target = nameof(Visual.Opacity);
            FadeAnimation.InsertKeyFrame(0, 0);
            FadeAnimation.InsertKeyFrame(1, 1);
            FadeAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
            FadeAnimation.DelayTime = DelayTime;
            FadeAnimation.Duration = Duration;

            if (IsReverse)
            {
                EntranceAnimation.Direction = AnimationDirection.Reverse;
                FadeAnimation.Direction = AnimationDirection.Reverse;
            }
            else
            {
                EntranceAnimation.Direction = AnimationDirection.Normal;
                FadeAnimation.Direction = AnimationDirection.Normal;
            }

            CompositionAnimationGroup AnimationGroup = Visual.Compositor.CreateAnimationGroup();
            AnimationGroup.Add(EntranceAnimation);
            AnimationGroup.Add(FadeAnimation);

            Visual.StartAnimationGroup(AnimationGroup);
        }

        private void SettingPage_Loading(FrameworkElement sender, object args)
        {
            UIMode.Items.Add(Globalization.GetString("Setting_UIMode_Recommand"));
            UIMode.Items.Add(Globalization.GetString("Setting_UIMode_SolidColor"));
            UIMode.Items.Add(Globalization.GetString("Setting_UIMode_Custom"));

            LanguageComboBox.Items.Add("中文(简体)");
            LanguageComboBox.Items.Add("English (United States)");
            LanguageComboBox.Items.Add("Français");
            LanguageComboBox.SelectedIndex = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values["LanguageOverride"]);

            LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;

            if (ApplicationData.Current.LocalSettings.Values["UIDisplayMode"] is int ModeIndex)
            {
                UIMode.SelectedIndex = ModeIndex;
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values["UIDisplayMode"] = 0;
                UIMode.SelectedIndex = 0;
            }

            if (ApplicationData.Current.LocalSettings.Values["IsLeftAreaOpen"] is bool Enable)
            {
                OpenLeftArea.IsOn = Enable;
            }

            if (ApplicationData.Current.LocalSettings.Values["IsDoubleClickEnable"] is bool IsDoubleClick)
            {
                FolderOpenMethod.IsOn = IsDoubleClick;
            }

            if (ApplicationData.Current.LocalSettings.Values["EnablePreLaunch"] is bool PreLaunch)
            {
                EnablePreLaunch.IsOn = PreLaunch;
            }

            if (AppThemeController.Current.Theme == ElementTheme.Light)
            {
                CustomFontColor.IsOn = true;
            }

            TreeViewDetach.IsOn = !IsDetachTreeViewAndPresenter;
            this.EnableQuicklook.IsEnabled = IsQuicklookAvailable;

            if (ApplicationData.Current.LocalSettings.Values["EnableQuicklook"] is bool EnableQuicklook)
            {
                this.EnableQuicklook.IsOn = EnableQuicklook;
                IsQuicklookEnable = EnableQuicklook;
            }
        }

        private async void SettingPage_Loaded(object sender, RoutedEventArgs e)
        {
            FeedBackCollection.CollectionChanged += (s, t) =>
            {
                if (FeedBackCollection.Count == 0)
                {
                    EmptyFeedBack.Text = Globalization.GetString("Progress_Tip_NoFeedback");
                    SubmitIssueOnGithub.Visibility = Visibility.Visible;
                    EmptyFeedBackArea.Visibility = Visibility.Visible;
                    FeedBackList.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyFeedBackArea.Visibility = Visibility.Collapsed;
                    SubmitIssueOnGithub.Visibility = Visibility.Collapsed;
                    FeedBackList.Visibility = Visibility.Visible;
                }
            };

            try
            {
                await foreach (FeedBackItem FeedBackItem in MySQL.Current.GetAllFeedBackAsync())
                {
                    if (FeedBackItem.Title.StartsWith("@"))
                    {
                        FeedBackItem.UpdateTitleAndSuggestion(FeedBackItem.Title, await FeedBackItem.Suggestion.TranslateAsync().ConfigureAwait(true));
                    }
                    else
                    {
                        FeedBackItem.UpdateTitleAndSuggestion(await FeedBackItem.Title.TranslateAsync().ConfigureAwait(true), await FeedBackItem.Suggestion.TranslateAsync().ConfigureAwait(true));
                    }

                    FeedBackCollection.Add(FeedBackItem);
                }
            }
            catch { }
            finally
            {
                if (FeedBackCollection.Count == 0)
                {
                    EmptyFeedBack.Text = Globalization.GetString("Progress_Tip_NoFeedback");
                    SubmitIssueOnGithub.Visibility = Visibility.Visible;
                }
                else
                {
                    FeedBackList.UpdateLayout();

                    await Task.Delay(500).ConfigureAwait(true);

                    FeedBackList.ScrollIntoViewSmoothly(FeedBackCollection.Last());
                }
            }
        }

        private void Like_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            LikeSymbol.Foreground = new SolidColorBrush(Colors.Yellow);
            LikeText.Foreground = new SolidColorBrush(Colors.Yellow);
        }

        private void Like_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            LikeSymbol.Foreground = new SolidColorBrush(Colors.White);
            LikeText.Foreground = new SolidColorBrush(Colors.White);
        }

        private async void Like_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _ = await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?productid=9N88QBQKF2RS"));
            ApplicationData.Current.RoamingSettings.Values["IsRated"] = true;
        }

        private async void FlyoutContinue_Click(object sender, RoutedEventArgs e)
        {
            ConfirmFly.Hide();
            await SQLite.Current.ClearSearchHistoryRecord().ConfigureAwait(true);

            QueueContentDialog dialog = new QueueContentDialog
            {
                Title = Globalization.GetString("Common_Dialog_TipTitle"),
                Content = Globalization.GetString("QueueDialog_ClearHistory_Content"),
                CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
            };
            _ = await dialog.ShowAsync().ConfigureAwait(true);
        }

        private void FlyoutCancel_Click(object sender, RoutedEventArgs e)
        {
            ConfirmFly.Hide();
        }

        private async void ClearUp_Click(object sender, RoutedEventArgs e)
        {
            ResetDialog Dialog = new ResetDialog();
            if ((await Dialog.ShowAsync().ConfigureAwait(true)) == ContentDialogResult.Primary)
            {
                if (Dialog.IsClearSecureFolder)
                {
                    SQLite.Current.Dispose();
                    MySQL.Current.Dispose();
                    try
                    {
                        await ApplicationData.Current.ClearAsync();
                    }
                    catch (Exception)
                    {
                        ApplicationData.Current.LocalSettings.Values.Clear();
                        await ApplicationData.Current.LocalFolder.DeleteAllSubFilesAndFolders().ConfigureAwait(true);
                        await ApplicationData.Current.TemporaryFolder.DeleteAllSubFilesAndFolders().ConfigureAwait(true);
                        await ApplicationData.Current.LocalCacheFolder.DeleteAllSubFilesAndFolders().ConfigureAwait(true);
                    }

                    Window.Current.Activate();
                    switch (await CoreApplication.RequestRestartAsync(string.Empty))
                    {
                        case AppRestartFailureReason.InvalidUser:
                        case AppRestartFailureReason.NotInForeground:
                        case AppRestartFailureReason.Other:
                            {
                                QueueContentDialog Dialog1 = new QueueContentDialog
                                {
                                    Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                                    Content = Globalization.GetString("QueueDialog_RestartFail_Content"),
                                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                                };
                                _ = await Dialog1.ShowAsync().ConfigureAwait(true);
                                break;
                            }
                    }
                }
                else
                {
                    LoadingText.Text = Globalization.GetString("Progress_Tip_Exporting");
                    LoadingControl.IsLoading = true;
                    MainPage.ThisPage.IsAnyTaskRunning = true;

                    StorageFolder SecureFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("SecureFolder", CreationCollisionOption.OpenIfExists);
                    string FileEncryptionAesKey = KeyGenerator.GetMD5FromKey(CredentialProtector.GetPasswordFromProtector("SecureAreaPrimaryPassword"), 16);

                    foreach (var Item in await SecureFolder.GetFilesAsync())
                    {
                        try
                        {
                            _ = await Item.DecryptAsync(Dialog.ExportFolder, FileEncryptionAesKey).ConfigureAwait(true);

                            await Item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        }
                        catch (Exception ex)
                        {
                            await Item.MoveAsync(Dialog.ExportFolder, $"{Item.Name}-{Globalization.GetString("DecryptFail_Backup_Text")}", NameCollisionOption.GenerateUniqueName);

                            if (ex is PasswordErrorException)
                            {
                                QueueContentDialog Dialog1 = new QueueContentDialog
                                {
                                    Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                                    Content = Globalization.GetString("QueueDialog_DecryptPasswordError_Content"),
                                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                                };

                                _ = await Dialog1.ShowAsync().ConfigureAwait(true);
                            }
                            else if (ex is FileDamagedException)
                            {
                                QueueContentDialog Dialog1 = new QueueContentDialog
                                {
                                    Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                                    Content = Globalization.GetString("QueueDialog_FileDamageError_Content"),
                                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                                };

                                _ = await Dialog1.ShowAsync().ConfigureAwait(true);
                            }
                        }
                    }

                    SQLite.Current.Dispose();
                    MySQL.Current.Dispose();
                    try
                    {
                        ApplicationData.Current.LocalSettings.Values.Clear();
                        await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Local);
                        await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Temporary);
                        await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Roaming);
                    }
                    catch (Exception)
                    {
                        await ApplicationData.Current.LocalFolder.DeleteAllSubFilesAndFolders().ConfigureAwait(true);
                        await ApplicationData.Current.TemporaryFolder.DeleteAllSubFilesAndFolders().ConfigureAwait(true);
                        await ApplicationData.Current.RoamingFolder.DeleteAllSubFilesAndFolders().ConfigureAwait(true);
                    }

                    await Task.Delay(1000).ConfigureAwait(true);

                    LoadingControl.IsLoading = false;
                    MainPage.ThisPage.IsAnyTaskRunning = false;

                    await Task.Delay(1000).ConfigureAwait(true);

                    Window.Current.Activate();
                    switch (await CoreApplication.RequestRestartAsync(string.Empty))
                    {
                        case AppRestartFailureReason.InvalidUser:
                        case AppRestartFailureReason.NotInForeground:
                        case AppRestartFailureReason.Other:
                            {
                                QueueContentDialog Dialog1 = new QueueContentDialog
                                {
                                    Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                                    Content = Globalization.GetString("QueueDialog_RestartFail_Content"),
                                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                                };
                                _ = await Dialog1.ShowAsync().ConfigureAwait(true);
                                break;
                            }
                    }
                }
            }
        }

        private void UIMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["UIDisplayMode"] = UIMode.SelectedIndex;

            switch (UIMode.SelectedIndex)
            {
                case 0:
                    {
                        CustomUIArea.Visibility = Visibility.Collapsed;
                        SolidColorArea.Visibility = Visibility.Collapsed;

                        AcrylicMode.IsChecked = null;
                        PictureMode.IsChecked = null;
                        SolidColor_White.IsChecked = null;
                        SolidColor_Black.IsChecked = null;
                        CustomFontColor.IsEnabled = true;

                        BackgroundController.Current.SwitchTo(BackgroundBrushType.Acrylic);
                        BackgroundController.Current.TintOpacity = 0.6;
                        BackgroundController.Current.TintLuminosityOpacity = -1;
                        BackgroundController.Current.AcrylicColor = Colors.LightSlateGray;
                        break;
                    }
                case 1:
                    {
                        CustomUIArea.Visibility = Visibility.Collapsed;
                        SolidColorArea.Visibility = Visibility.Visible;

                        AcrylicMode.IsChecked = null;
                        PictureMode.IsChecked = null;
                        CustomFontColor.IsEnabled = false;

                        if (ApplicationData.Current.LocalSettings.Values["SolidColorType"] is string ColorType)
                        {
                            if (ColorType == Colors.White.ToString())
                            {
                                SolidColor_White.IsChecked = true;
                            }
                            else
                            {
                                SolidColor_Black.IsChecked = true;
                            }
                        }
                        else
                        {
                            SolidColor_White.IsChecked = true;
                        }

                        break;
                    }
                default:
                    {
                        CustomUIArea.Visibility = Visibility.Visible;
                        SolidColorArea.Visibility = Visibility.Collapsed;
                        SolidColor_White.IsChecked = null;
                        SolidColor_Black.IsChecked = null;
                        CustomFontColor.IsEnabled = true;

                        if (ApplicationData.Current.LocalSettings.Values["CustomUISubMode"] is string Mode)
                        {
                            if ((BackgroundBrushType)Enum.Parse(typeof(BackgroundBrushType), Mode) == BackgroundBrushType.Acrylic)
                            {
                                AcrylicMode.IsChecked = true;
                            }
                            else
                            {
                                PictureMode.IsChecked = true;
                            }
                        }
                        else
                        {
                            ApplicationData.Current.LocalSettings.Values["CustomUISubMode"] = Enum.GetName(typeof(BackgroundBrushType), BackgroundBrushType.Acrylic);
                            AcrylicMode.IsChecked = true;

                            if (ApplicationData.Current.LocalSettings.Values["BackgroundTintLuminosity"] is string Luminosity)
                            {
                                float Value = Convert.ToSingle(Luminosity);
                                TintLuminositySlider.Value = Value;
                                BackgroundController.Current.TintLuminosityOpacity = Value;
                            }
                            else
                            {
                                TintLuminositySlider.Value = 0.8;
                                BackgroundController.Current.TintLuminosityOpacity = 0.8;
                            }

                            if (ApplicationData.Current.LocalSettings.Values["BackgroundTintOpacity"] is string Opacity)
                            {
                                float Value = Convert.ToSingle(Opacity);
                                TintOpacitySlider.Value = Value;
                                BackgroundController.Current.TintOpacity = Value;
                            }
                            else
                            {
                                TintOpacitySlider.Value = 0.6;
                                BackgroundController.Current.TintOpacity = 0.6;
                            }

                            if (ApplicationData.Current.LocalSettings.Values["AcrylicThemeColor"] is string AcrylicColor)
                            {
                                BackgroundController.Current.AcrylicColor = BackgroundController.GetColorFromHexString(AcrylicColor);
                            }
                        }

                        break;
                    }
            }
        }

        private void AcrylicColor_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerTeachTip.IsOpen = true;
        }

        private void TintOpacityQuestion_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            OpacityTip.IsOpen = true;
        }

        private void TintLuminosityQuestion_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            LuminosityTip.IsOpen = true;
        }

        private void ColorPickerTeachTip_Closed(Microsoft.UI.Xaml.Controls.TeachingTip sender, Microsoft.UI.Xaml.Controls.TeachingTipClosedEventArgs args)
        {
            ApplicationData.Current.LocalSettings.Values["AcrylicThemeColor"] = AcrylicColorPicker.Color.ToString();
        }

        private async void Purchase_Click(object sender, RoutedEventArgs e)
        {
            StoreContext Store = StoreContext.GetDefault();
            StoreProductResult ProductResult = await Store.GetStoreProductForCurrentAppAsync();

            if (ProductResult.ExtendedError == null)
            {
                if (ProductResult.Product != null)
                {
                    switch ((await ProductResult.Product.RequestPurchaseAsync()).Status)
                    {
                        case StorePurchaseStatus.Succeeded:
                            {
                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                {
                                    Title = Globalization.GetString("Common_Dialog_TipTitle"),
                                    Content = Globalization.GetString("QueueDialog_Store_PurchaseSuccess_Content"),
                                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                                };
                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                break;
                            }
                        case StorePurchaseStatus.AlreadyPurchased:
                            {
                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                {
                                    Title = Globalization.GetString("Common_Dialog_TipTitle"),
                                    Content = Globalization.GetString("QueueDialog_Store_AlreadyPurchase_Content"),
                                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                                };
                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                break;
                            }
                        case StorePurchaseStatus.NotPurchased:
                            {
                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                {
                                    Title = Globalization.GetString("Common_Dialog_TipTitle"),
                                    Content = Globalization.GetString("QueueDialog_Store_NotPurchase_Content"),
                                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                                };
                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                break;
                            }
                        default:
                            {
                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                {
                                    Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                                    Content = Globalization.GetString("QueueDialog_Store_NetworkError_Content"),
                                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                                };
                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                break;
                            }
                    }
                }
            }
            else
            {
                QueueContentDialog QueueContenDialog = new QueueContentDialog
                {
                    Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                    Content = Globalization.GetString("QueueDialog_Store_NetworkError_Content"),
                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                };
                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
            }
        }

        private async void UpdateLogLink_Click(object sender, RoutedEventArgs e)
        {
            WhatIsNew Dialog = new WhatIsNew();
            _ = await Dialog.ShowAsync().ConfigureAwait(true);
        }

        private async void SystemInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (Package.Current.Id.Architecture == ProcessorArchitecture.X64 || Package.Current.Id.Architecture == ProcessorArchitecture.X86)
            {
                SystemInfoDialog dialog = new SystemInfoDialog();
                _ = await dialog.ShowAsync().ConfigureAwait(true);
            }
            else
            {
                QueueContentDialog dialog = new QueueContentDialog
                {
                    Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                    Content = Globalization.GetString("QueueDialog_NotSupportARM_Content"),
                    CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                };
                _ = await dialog.ShowAsync().ConfigureAwait(true);
            }

        }

        private async void AddFeedBack_Click(object sender, RoutedEventArgs e)
        {
            FeedBackDialog Dialog = new FeedBackDialog();
            if ((await Dialog.ShowAsync().ConfigureAwait(true)) == ContentDialogResult.Primary)
            {
                if (FeedBackCollection.Count != 0)
                {
                    if (FeedBackCollection.FirstOrDefault((It) => It.UserName == UserName && It.Suggestion == Dialog.FeedBack && It.Title == Dialog.TitleName) == null)
                    {
                        FeedBackItem Item = new FeedBackItem(UserName, Dialog.TitleName, Dialog.FeedBack, "0", "0", UserID, Guid.NewGuid().ToString("D"));
                        if (await MySQL.Current.SetFeedBackAsync(Item).ConfigureAwait(true))
                        {
                            FeedBackCollection.Add(Item);
                            await Task.Delay(1000).ConfigureAwait(true);
                            FeedBackList.ScrollIntoViewSmoothly(FeedBackCollection.Last());
                        }
                        else
                        {
                            QueueContentDialog dialog = new QueueContentDialog
                            {
                                Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                                Content = Globalization.GetString("QueueDialog_FeedBackNetworkError_Content"),
                                CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                            };
                            _ = await dialog.ShowAsync().ConfigureAwait(true);
                        }
                    }
                    else
                    {
                        QueueContentDialog TipsDialog = new QueueContentDialog
                        {
                            Title = Globalization.GetString("Common_Dialog_TipTitle"),
                            Content = Globalization.GetString("QueueDialog_FeedBackRepeatError_Content"),
                            CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                        };
                        _ = await TipsDialog.ShowAsync().ConfigureAwait(true);
                    }
                }
                else
                {
                    FeedBackItem Item = new FeedBackItem(UserName, Dialog.TitleName, Dialog.FeedBack, "0", "0", UserID, Guid.NewGuid().ToString("D"));
                    if (!await MySQL.Current.SetFeedBackAsync(Item).ConfigureAwait(true))
                    {
                        QueueContentDialog dialog = new QueueContentDialog
                        {
                            Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                            Content = Globalization.GetString("QueueDialog_FeedBackNetworkError_Content"),
                            CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                        };
                        _ = await dialog.ShowAsync().ConfigureAwait(true);
                    }
                    else
                    {
                        FeedBackCollection.Add(Item);
                        await Task.Delay(1000).ConfigureAwait(true);
                        FeedBackList.ScrollIntoViewSmoothly(FeedBackCollection.Last());
                    }
                }
            }
        }

        private void FeedBackList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is FeedBackItem Item)
            {
                FeedBackList.SelectedItem = Item;
                FeedBackList.ContextFlyout = UserID == "zrfcfgs@outlook.com" ? FeedBackDevelopFlyout : (Item.UserID == UserID ? FeedBackFlyout : null);
            }
        }

        private async void FeedBackEdit_Click(object sender, RoutedEventArgs e)
        {
            if (FeedBackList.SelectedItem is FeedBackItem SelectItem)
            {
                FeedBackDialog Dialog = new FeedBackDialog(SelectItem.Title, SelectItem.Suggestion);
                if ((await Dialog.ShowAsync().ConfigureAwait(true)) == ContentDialogResult.Primary)
                {
                    if (!await MySQL.Current.UpdateFeedBackAsync(Dialog.TitleName, Dialog.FeedBack, SelectItem.GUID).ConfigureAwait(true))
                    {
                        QueueContentDialog dialog = new QueueContentDialog
                        {
                            Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                            Content = Globalization.GetString("QueueDialog_FeedBackNetworkError_Content"),
                            CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                        };
                        _ = await dialog.ShowAsync().ConfigureAwait(true);
                    }
                    else
                    {
                        SelectItem.UpdateTitleAndSuggestion(Dialog.TitleName, Dialog.FeedBack);
                    }
                }
            }
        }

        private async void FeedBackDelete_Click(object sender, RoutedEventArgs e)
        {
            if (FeedBackList.SelectedItem is FeedBackItem SelectItem)
            {
                if (!await MySQL.Current.DeleteFeedBackAsync(SelectItem).ConfigureAwait(true))
                {
                    QueueContentDialog dialog = new QueueContentDialog
                    {
                        Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                        Content = Globalization.GetString("QueueDialog_FeedBackNetworkError_Content"),
                        CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                    };
                    _ = await dialog.ShowAsync().ConfigureAwait(true);
                }
                else
                {
                    FeedBackCollection.Remove(SelectItem);
                }
            }
        }

        private void FeedBackQuestion_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FeedBackTip.IsOpen = true;
        }

        private void OpenLeftArea_Toggled(object sender, RoutedEventArgs e)
        {
            TabViewContainer.ThisPage.LeftSideLength = OpenLeftArea.IsOn ? new GridLength(300) : new GridLength(0);
        }

        private void FolderOpenMethod_Toggled(object sender, RoutedEventArgs e)
        {
            if (FolderOpenMethod.IsOn)
            {
                ApplicationData.Current.LocalSettings.Values["IsDoubleClickEnable"] = true;
                IsDoubleClickEnable = true;
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values["IsDoubleClickEnable"] = false;
                IsDoubleClickEnable = false;
            }
        }

        private void AcrylicMode_Checked(object sender, RoutedEventArgs e)
        {
            CustomAcrylicArea.Visibility = Visibility.Visible;
            CustomPictureArea.Visibility = Visibility.Collapsed;

            BackgroundController.Current.SwitchTo(BackgroundBrushType.Acrylic);
            ApplicationData.Current.LocalSettings.Values["CustomUISubMode"] = Enum.GetName(typeof(BackgroundBrushType), BackgroundBrushType.Acrylic);

            if (ApplicationData.Current.LocalSettings.Values["BackgroundTintLuminosity"] is string Luminosity)
            {
                float Value = Convert.ToSingle(Luminosity);
                TintLuminositySlider.Value = Value;
                BackgroundController.Current.TintLuminosityOpacity = Value;
            }
            else
            {
                TintLuminositySlider.Value = 0.8;
                BackgroundController.Current.TintLuminosityOpacity = 0.8;
            }

            if (ApplicationData.Current.LocalSettings.Values["BackgroundTintOpacity"] is string Opacity)
            {
                float Value = Convert.ToSingle(Opacity);
                TintOpacitySlider.Value = Value;
                BackgroundController.Current.TintOpacity = Value;
            }
            else
            {
                TintOpacitySlider.Value = 0.6;
                BackgroundController.Current.TintOpacity = 0.6;
            }

            if (ApplicationData.Current.LocalSettings.Values["AcrylicThemeColor"] is string AcrylicColor)
            {
                BackgroundController.Current.AcrylicColor = BackgroundController.GetColorFromHexString(AcrylicColor);
            }
        }

        private async void PictureMode_Checked(object sender, RoutedEventArgs e)
        {
            CustomAcrylicArea.Visibility = Visibility.Collapsed;
            CustomPictureArea.Visibility = Visibility.Visible;

            if (PictureList.Count == 0)
            {
                foreach (Uri ImageUri in await SQLite.Current.GetBackgroundPictureAsync().ConfigureAwait(true))
                {
                    BitmapImage Image = new BitmapImage
                    {
                        DecodePixelHeight = 90,
                        DecodePixelWidth = 160
                    };
                    PictureList.Add(new BackgroundPicture(Image, ImageUri));
                    Image.UriSource = ImageUri;
                }
            }

            ApplicationData.Current.LocalSettings.Values["CustomUISubMode"] = Enum.GetName(typeof(BackgroundBrushType), BackgroundBrushType.Picture);

            if (ApplicationData.Current.LocalSettings.Values["PictureBackgroundUri"] is string Uri)
            {
                BackgroundPicture PictureItem = PictureList.FirstOrDefault((Picture) => Picture.PictureUri.ToString() == Uri);

                PictureGirdView.SelectedItem = PictureItem;
                PictureGirdView.ScrollIntoViewSmoothly(PictureItem);
                BackgroundController.Current.SwitchTo(BackgroundBrushType.Picture, PictureItem.PictureUri);
            }
            else
            {
                PictureGirdView.SelectedIndex = 0;
                BackgroundController.Current.SwitchTo(BackgroundBrushType.Picture, PictureList.FirstOrDefault().PictureUri);
            }
        }

        private void PictureGirdView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PictureGirdView.SelectedItem is BackgroundPicture Picture)
            {
                BackgroundController.Current.SwitchTo(BackgroundBrushType.Picture, Picture.PictureUri);
            }
        }

        private async void AddImageToPictureButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker Picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };
            Picker.FileTypeFilter.Add(".png");
            Picker.FileTypeFilter.Add(".jpg");
            Picker.FileTypeFilter.Add(".jpeg");
            Picker.FileTypeFilter.Add(".bmp");

            if (await Picker.PickSingleFileAsync() is StorageFile File)
            {
                StorageFolder ImageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("CustomImageFolder");

                StorageFile CopyedFile = await File.CopyAsync(ImageFolder, $"BackgroundPicture_{Guid.NewGuid():N}{File.FileType}", NameCollisionOption.GenerateUniqueName);

                BitmapImage Bitmap = new BitmapImage
                {
                    DecodePixelWidth = 160,
                    DecodePixelHeight = 90
                };
                BackgroundPicture Picture = new BackgroundPicture(Bitmap, new Uri($"ms-appdata:///local/CustomImageFolder/{CopyedFile.Name}"));
                PictureList.Add(Picture);
                Bitmap.UriSource = Picture.PictureUri;

                PictureGirdView.ScrollIntoViewSmoothly(Picture);
                PictureGirdView.SelectedItem = Picture;

                await SQLite.Current.SetBackgroundPictureAsync(Picture.PictureUri).ConfigureAwait(false);
            }
        }

        private async void DeletePictureButton_Click(object sender, RoutedEventArgs e)
        {
            if (PictureGirdView.SelectedItem is BackgroundPicture Picture)
            {
                await SQLite.Current.DeleteBackgroundPictureAsync(Picture.PictureUri).ConfigureAwait(true);
                PictureList.Remove(Picture);
                PictureGirdView.SelectedIndex = PictureList.Count - 1;
            }
        }

        private void PictureGirdView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is BackgroundPicture Picture)
            {
                PictureGirdView.ContextFlyout = PictureFlyout;

                DeletePictureButton.IsEnabled = !Picture.PictureUri.ToString().StartsWith("ms-appx://");

                PictureGirdView.SelectedItem = Picture;
            }
            else
            {
                PictureGirdView.ContextFlyout = null;
            }
        }

        private void CustomFontColor_Toggled(object sender, RoutedEventArgs e)
        {
            if (CustomFontColor.IsOn)
            {
                AppThemeController.Current.ChangeThemeTo(ElementTheme.Light);
            }
            else
            {
                AppThemeController.Current.ChangeThemeTo(ElementTheme.Dark);
            }
        }

        private async void AutoBoot_Toggled(object sender, RoutedEventArgs e)
        {
            StartupTask BootTask = await StartupTask.GetAsync("RXExplorer");

            if (AutoBoot.IsOn)
            {
                switch (await BootTask.RequestEnableAsync())
                {
                    case StartupTaskState.Disabled:
                    case StartupTaskState.DisabledByPolicy:
                    case StartupTaskState.DisabledByUser:
                        {
                            AutoBoot.Toggled -= AutoBoot_Toggled;
                            AutoBoot.IsOn = false;
                            AutoBoot.Toggled += AutoBoot_Toggled;

                            QueueContentDialog Dialog = new QueueContentDialog
                            {
                                Title = Globalization.GetString("Common_Dialog_TipTitle"),
                                Content = Globalization.GetString("QueueDialog_BootAtStart_Content"),
                                PrimaryButtonText = Globalization.GetString("Common_Dialog_NowButton"),
                                CloseButtonText = Globalization.GetString("Common_Dialog_LaterButton")
                            };

                            if ((await Dialog.ShowAsync().ConfigureAwait(true)) == ContentDialogResult.Primary)
                            {
                                await Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures-app"));
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            else
            {
                BootTask.Disable();
            }
        }

        private void EnablePreLaunch_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["EnablePreLaunch"] = EnablePreLaunch.IsOn;

            CoreApplication.EnablePrelaunch(EnablePreLaunch.IsOn);
        }

        private void PreLaunchQuestion_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PreLaunchTip.IsOpen = true;
        }

        private void SolidColor_White_Checked(object sender, RoutedEventArgs e)
        {
            BackgroundController.Current.SwitchTo(BackgroundBrushType.SolidColor, Color: Colors.White);
            if (CustomFontColor.IsOn)
            {
                AppThemeController.Current.ChangeThemeTo(ElementTheme.Light);
            }
            else
            {
                CustomFontColor.IsOn = true;
            }
        }

        private void SolidColor_Black_Checked(object sender, RoutedEventArgs e)
        {
            BackgroundController.Current.SwitchTo(BackgroundBrushType.SolidColor, Color: Colors.Black);
            if (CustomFontColor.IsOn)
            {
                CustomFontColor.IsOn = false;
            }
            else
            {
                AppThemeController.Current.ChangeThemeTo(ElementTheme.Dark);
            }
        }

        private async void FeedBackNotice_Click(object sender, RoutedEventArgs e)
        {
            if (FeedBackList.SelectedItem is FeedBackItem SelectItem)
            {
                FeedBackDialog Dialog = new FeedBackDialog($"@{SelectItem.UserName}");
                if ((await Dialog.ShowAsync().ConfigureAwait(true)) == ContentDialogResult.Primary)
                {
                    if (FeedBackCollection.FirstOrDefault((It) => It.UserName == UserName && It.Suggestion == Dialog.FeedBack && It.Title == Dialog.TitleName) == null)
                    {
                        FeedBackItem Item = new FeedBackItem(UserName, Dialog.TitleName, Dialog.FeedBack, "0", "0", UserID, Guid.NewGuid().ToString("D"));
                        if (await MySQL.Current.SetFeedBackAsync(Item).ConfigureAwait(true))
                        {
                            FeedBackCollection.Add(Item);
                            await Task.Delay(1000).ConfigureAwait(true);
                            FeedBackList.ScrollIntoViewSmoothly(FeedBackCollection.Last());

                            if (Regex.IsMatch(SelectItem.UserID, "^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$"))
                            {
                                if (Globalization.CurrentLanguage == LanguageEnum.Chinese)
                                {
                                    string Message = $"您的反馈原文：\r------------------------------------\r{SelectItem.Title}{Environment.NewLine}{SelectItem.Suggestion}\r------------------------------------\r\r开发者回复内容：\r------------------------------------\r{Item.Title}{Environment.NewLine}{Item.Suggestion}\r------------------------------------{Environment.NewLine}";
                                    _ = await Launcher.LaunchUriAsync(new Uri($"mailto:{SelectItem.UserID}?subject=开发者已回复您的反馈&body={Uri.EscapeDataString(Message)}"), new LauncherOptions { TreatAsUntrusted = false, DisplayApplicationPicker = false });
                                }
                                else
                                {
                                    string Message = $"Your original feedback：\r------------------------------------\r{SelectItem.Title}{Environment.NewLine}{SelectItem.Suggestion}\r------------------------------------\r\rDeveloper reply：\r------------------------------------\r{Item.Title}{Environment.NewLine}{Item.Suggestion}\r------------------------------------{Environment.NewLine}";
                                    _ = await Launcher.LaunchUriAsync(new Uri($"mailto:{SelectItem.UserID}?subject=The developer has responded to your feedback in RX Explorer&body={Uri.EscapeDataString(Message)}"), new LauncherOptions { TreatAsUntrusted = false, DisplayApplicationPicker = false });
                                }
                            }
                        }
                        else
                        {
                            QueueContentDialog dialog = new QueueContentDialog
                            {
                                Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                                Content = Globalization.GetString("QueueDialog_FeedBackNetworkError_Content"),
                                CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                            };
                            _ = await dialog.ShowAsync().ConfigureAwait(true);
                        }
                    }
                    else
                    {
                        QueueContentDialog TipsDialog = new QueueContentDialog
                        {
                            Title = Globalization.GetString("Common_Dialog_TipTitle"),
                            Content = Globalization.GetString("QueueDialog_FeedBackRepeatError_Content"),
                            CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                        };
                        _ = await TipsDialog.ShowAsync().ConfigureAwait(true);
                    }
                }
            }
        }

        private void TreeViewDetach_Toggled(object sender, RoutedEventArgs e)
        {
            TabViewContainer.ThisPage.TreeViewLength = TreeViewDetach.IsOn ? new GridLength(2, GridUnitType.Star) : new GridLength(0);
        }

        private void QuicklookQuestion_Tapped(object sender, TappedRoutedEventArgs e)
        {
            QuicklookTip.IsOpen = true;
        }

        private void EnableQuicklook_Toggled(object sender, RoutedEventArgs e)
        {
            if (EnableQuicklook.IsOn)
            {
                IsQuicklookEnable = true;
                ApplicationData.Current.LocalSettings.Values["EnableQuicklook"] = true;
            }
            else
            {
                IsQuicklookEnable = false;
                ApplicationData.Current.LocalSettings.Values["EnableQuicklook"] = false;
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LanguageRestartButton.Visibility = Visibility.Visible;

            switch (LanguageComboBox.SelectedIndex)
            {
                case 0:
                    {
                        Globalization.SwitchTo(LanguageEnum.Chinese);
                        break;
                    }
                case 1:
                    {
                        Globalization.SwitchTo(LanguageEnum.English);
                        break;
                    }
                case 2:
                    {
                        Globalization.SwitchTo(LanguageEnum.French);
                        break;
                    }
            }
        }

        private async void LanguageRestartButton_Click(object sender, RoutedEventArgs e)
        {
            SQLite.Current.Dispose();
            MySQL.Current.Dispose();

            Window.Current.Activate();

            switch (await CoreApplication.RequestRestartAsync(string.Empty))
            {
                case AppRestartFailureReason.InvalidUser:
                case AppRestartFailureReason.NotInForeground:
                case AppRestartFailureReason.Other:
                    {
                        QueueContentDialog Dialog1 = new QueueContentDialog
                        {
                            Title = Globalization.GetString("Common_Dialog_ErrorTitle"),
                            Content = Globalization.GetString("QueueDialog_RestartFail_Content"),
                            CloseButtonText = Globalization.GetString("Common_Dialog_CloseButton")
                        };
                        _ = await Dialog1.ShowAsync().ConfigureAwait(true);
                        break;
                    }
            }
        }
    }
}
