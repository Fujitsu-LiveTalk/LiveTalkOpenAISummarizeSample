/*
 * Copyright 2023 FUJITSU LIMITED
 * クラス名　：
 * 概要      ：イベントパラメタ共通定義
*/
using System;

namespace LiveTalkOpenAISummarizeSample.Common
{
    internal delegate void MessagedEventHandler(object sender, MessageEventArgs e);
    internal class MessageEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }
}
