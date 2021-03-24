using System;

public class MsgEventArgs : EventArgs {
        public string Message { get; private set; }

        public MsgEventArgs(string message) {
            Message = message;
        }
    }