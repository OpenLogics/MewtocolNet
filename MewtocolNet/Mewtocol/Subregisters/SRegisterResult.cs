namespace MewtocolNet.Responses {
    public class SRegisterResult {
        public CommandResult Result { get; set; }
        public SRegister Register { get; set; }

        public override string ToString() {
            string errmsg = Result.Success ? "" : $", Error [{Result.ErrorDescription}]";
            return $"Result [{Result.Success}], Register [{Register.ToString()}]{errmsg}";
        }
    }




}
