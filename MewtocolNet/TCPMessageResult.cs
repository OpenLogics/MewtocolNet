namespace MewtocolNet {
    internal enum TCPMessageResult {

        Waiting,
        Success,
        NotConnected,
        FailedWithException,
        FailedLineFeed,

    }

    internal enum CommandState {

        Intial,
        LineFeed,
        RequestedNextFrame,
        Complete

    }

}