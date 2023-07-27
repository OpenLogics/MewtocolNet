using MewtocolNet.DataLists;

namespace MewtocolNet
{

    public struct MewtocolFrameResponse {

        public bool Success { get; private set; }

        public string Response { get; private set; }

        public int ErrorCode { get; private set; }

        public string Error { get; private set; }

        public static MewtocolFrameResponse Timeout => new MewtocolFrameResponse(403, "Request timed out");

        public static MewtocolFrameResponse NotIntialized => new MewtocolFrameResponse(405, "PLC was not initialized");

        public MewtocolFrameResponse(string response) {

            Success = true;
            ErrorCode = 0;
            Response = response;
            Error = null;

        }

        public MewtocolFrameResponse(int errorCode) {

            Success = false;
            Response = null;
            ErrorCode = errorCode;
            Error = CodeDescriptions.Error[errorCode];

        }

        public MewtocolFrameResponse(int errorCode, string exceptionMsg) {

            Success = false;
            Response = null;
            ErrorCode = errorCode;
            Error = exceptionMsg;

        }

        /// <inheritdoc/>
        public static bool operator ==(MewtocolFrameResponse c1, MewtocolFrameResponse c2) {
            return c1.Equals(c2);
        }

        /// <inheritdoc/>
        public static bool operator !=(MewtocolFrameResponse c1, MewtocolFrameResponse c2) {
            return !c1.Equals(c2);
        }

    }

}
