/*
 * Copyright 2023 FUJITSU LIMITED
 * クラス名　：SettingWindow
 * 概要      ：メイン画面
*/
using System.Windows;

namespace LiveTalkOpenAISummarizeSample.Views
{
    public partial class SettingWindow : Window
    {
        public ViewModels.SettingViewModel ViewModel { get; } = new ViewModels.SettingViewModel();
        public SettingWindow()
        {
            InitializeComponent();

            // 設定画面クローズイベント
            this.ViewModel.Closed += (s, args) =>
            {
                this.Close();
            };
        }
    }
}
