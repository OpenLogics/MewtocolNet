namespace MewtocolNet.Registers {
    public class BRegisterResult {

        public CommandResult Result { get; set; }
        public BRegister Register { get; set; }

        public override string ToString() {
            string errmsg = Result.Success ? "" : $", Error [{Result.ErrorDescription}]";
            return $"Result [{Result.Success}], Register [{Register.ToString()}]{errmsg}";
        }

    }
}
