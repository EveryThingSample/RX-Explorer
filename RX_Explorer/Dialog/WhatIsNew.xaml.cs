﻿using RX_Explorer.Class;
using System;
using Windows.Storage;

namespace RX_Explorer.Dialog
{
    public sealed partial class WhatIsNew : QueueContentDialog
    {
        public WhatIsNew()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            switch (Globalization.CurrentLanguage)
            {
                case LanguageEnum.Chinese:
                    {
                        StorageFile UpdateFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/UpdateLog-Chinese.txt")).AsTask().Result;
                        MarkDown.Text = FileIO.ReadTextAsync(UpdateFile).AsTask().Result;
                        break;
                    }

                case LanguageEnum.English:
                    {
                        StorageFile UpdateFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/UpdateLog-English.txt")).AsTask().Result;
                        MarkDown.Text = FileIO.ReadTextAsync(UpdateFile).AsTask().Result;
                        break;
                    }
                case LanguageEnum.French:
                    {
                        StorageFile UpdateFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/UpdateLog-French.txt")).AsTask().Result;
                        MarkDown.Text = FileIO.ReadTextAsync(UpdateFile).AsTask().Result;
                        break;
                    }
            }
        }
    }
}
