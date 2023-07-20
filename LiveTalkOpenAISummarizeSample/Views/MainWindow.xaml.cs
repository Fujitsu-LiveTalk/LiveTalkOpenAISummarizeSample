/*
 * Copyright 2023 FUJITSU LIMITED
 * クラス名　：MainWindow
 * 概要      ：メイン画面
*/
using System.Windows;

namespace LiveTalkOpenAISummarizeSample.Views
{
    public partial class MainWindow : Window
    {
        public ViewModels.MainViewModel ViewModel { get; } = App.MainVM;

        public MainWindow()
        {
            InitializeComponent();

            // メインウィンドウクローズイベント処理
            this.ViewModel.Closed += (s, args) =>
            {
                this.Close();
            };

            // 画面遷移イベント処理
            this.ViewModel.Messaged += (s, args) =>
            {
                switch (args.Message)
                {
                    case "SettingWindow":
                        this.ShowDialog(new SettingWindow() { Owner = (MainWindow)MainWindow.GetWindow(this) });
                        break;
                }
            };

            // ページの読み込み完了
            this.Loaded += (s, args) =>
            {
                // 必須設定チェック→設定画面表示
                this.ViewModel.InitialSetting();
            };
        }

        // メイン画面を使用不可にして疑似的なDialogとする（サブスクリーン含め移動などは可能）
        private void ShowDialog(Window target)
        {
            target.Closed += (_, __) =>
            {
                this.IsEnabled = true;
            };
            this.IsEnabled = false;
            target.Show();
        }
    }
}
