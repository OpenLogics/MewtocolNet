using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet {

    public struct MewtocolFrameResponse {

        public bool Success { get; private set; }

        public string Response { get; private set; }

        public int ErrorCode { get; private set; }

        public string Error { get; private set; }

        public MewtocolFrameResponse (string response) {

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

    }

}
