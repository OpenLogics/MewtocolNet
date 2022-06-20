namespace MewtocolNet.Registers {
    /// <summary>
    /// Result for a read/write operation
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class NRegisterResult<T> {
        public CommandResult Result { get; set; }
        public NRegister<T> Register { get; set; }

        public override string ToString() {
            string errmsg = Result.Success ? "" : $", Error [{Result.ErrorDescription}]";
            return $"Result [{Result.Success}], Register [{Register.ToString()}]{errmsg}";
        }
    }




}
