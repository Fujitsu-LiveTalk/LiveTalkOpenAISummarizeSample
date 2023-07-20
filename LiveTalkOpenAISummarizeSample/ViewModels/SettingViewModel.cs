/*
 * Copyright 2023 FUJITSU LIMITED
 * クラス名　：SettingViewModel
 * 概要      ：SettingViewModel
*/

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Disposables;
using System.Windows;

namespace LiveTalkOpenAISummarizeSample.ViewModels
{

    public class SettingViewModel
    {
        private Models.SummarizeModel Model = new Models.SummarizeModel();
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();

        #region "Property"
        [Required]       // 必須チェック
        public ReactiveProperty<string> DeploymentName { get; }
        [Required]       // 必須チェック
        public ReactiveProperty<string> APIResourceName { get; }
        [Required]       // 必須チェック
        public ReactiveProperty<string> APIKey { get; }
        #endregion

        /// <summary>
        /// True:設定可能
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsCanStart { get; }

        public SettingViewModel()
        {
            // プロパティ設定
            this.DeploymentName = this.Model.ToReactivePropertyAsSynchronized((x) => x.DeploymentName)
                .SetValidateAttribute(() => this.DeploymentName)
                .AddTo(this.Disposable);
            this.APIResourceName = this.Model.ToReactivePropertyAsSynchronized((x) => x.APIResourceName)
                .SetValidateAttribute(() => this.APIResourceName)
                .AddTo(this.Disposable);
            this.APIKey = this.Model.ToReactivePropertyAsSynchronized((x) => x.APIKey)
                .SetValidateAttribute(() => this.APIKey)
                .AddTo(this.Disposable);

            // 3つのステータスがすべてFalseの時だけスタートボタンがクリックできる
            this.IsCanStart = new[]
            {
                this.DeploymentName.ObserveHasErrors,
                this.APIResourceName.ObserveHasErrors,
                this.APIKey.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
             .ToReadOnlyReactiveProperty()
             .AddTo(this.Disposable);

            // コマンド設定
            this.ExitCommand.Subscribe((x) =>
            {
                OnClosed();
            }).AddTo(this.Disposable);

            // エラーハンドリング
            this.Model.Threw += (s, e) =>
            {
                MessageBox.Show(e.GetException().Message, "LiveTalk SummarizeText Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
            };
        }

        /// <summary>
        /// 画面クローズ
        /// </summary>
        public ReactiveCommand ExitCommand { get; } = new ReactiveCommand();
        public event EventHandler Closed;
        protected virtual void OnClosed()
        {
            this.Closed?.Invoke(this, new EventArgs());
        }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}
