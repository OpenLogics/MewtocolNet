using MewtocolNet.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet {

    public static class MewtocolInterfaceExtensions {

        /// <summary>
        /// Attaches a continous reader that reads back the Registers and Contacts
        /// </summary>
        public static Task AttachContinousReader (this Task<MewtocolInterface> interfaceTask, int _refreshTimeMS = 200) {

            interfaceTask.Wait(-1);

            var interf = interfaceTask.Result;

            if (interf.ContinousReaderRunning)
                return Task.CompletedTask;

            interf.cTokenAutoUpdater = new CancellationTokenSource();

            Console.WriteLine("Attaching cont reader");

            Task.Factory.StartNew(async () => {

                var plcinf = await interf.GetPLCInfoAsync();
                if (plcinf == null) {
                    Console.WriteLine("PLC is not reachable");
                    throw new Exception("PLC is not reachable");
                }
                if (!plcinf.OperationMode.RunMode) {
                    Console.WriteLine("PLC is not running");
                    throw new Exception("PLC is not running");
                }

                interf.ContinousReaderRunning = true;

                while (true) {

                    //dont update when currently writing a var
                    if (interf.isWriting) {
                        continue;
                    }

                    await Task.Delay(_refreshTimeMS);
                    foreach (var reg in interf.Registers) {

                        if (reg is NRegister<short> shortReg) {
                            var lastVal = shortReg.Value;
                            var readout = (await interf.ReadNumRegister(shortReg)).Register.Value;
                            if (lastVal != readout) {
                                shortReg.LastValue = readout;
                                interf.InvokeRegisterChanged(shortReg);
                                shortReg.TriggerNotifyChange();
                            }
                        }
                        if (reg is NRegister<ushort> ushortReg) {
                            var lastVal = ushortReg.Value;
                            var readout = (await interf.ReadNumRegister(ushortReg)).Register.Value;
                            if (lastVal != readout) {
                                ushortReg.LastValue = readout;
                                interf.InvokeRegisterChanged(ushortReg);
                                ushortReg.TriggerNotifyChange();
                            }
                        }
                        if (reg is NRegister<int> intReg) {
                            var lastVal = intReg.Value;
                            var readout = (await interf.ReadNumRegister(intReg)).Register.Value;
                            if (lastVal != readout) {
                                intReg.LastValue = readout;
                                interf.InvokeRegisterChanged(intReg);
                                intReg.TriggerNotifyChange();
                            }
                        }
                        if (reg is NRegister<uint> uintReg) {
                            var lastVal = uintReg.Value;
                            var readout = (await interf.ReadNumRegister(uintReg)).Register.Value;
                            if (lastVal != readout) {
                                uintReg.LastValue = readout;
                                interf.InvokeRegisterChanged(uintReg);
                                uintReg.TriggerNotifyChange();
                            }
                        }
                        if (reg is NRegister<float> floatReg) {
                            var lastVal = floatReg.Value;
                            var readout = (await interf.ReadNumRegister(floatReg)).Register.Value;
                            if (lastVal != readout) {
                                floatReg.LastValue = readout;
                                interf.InvokeRegisterChanged(floatReg);
                                floatReg.TriggerNotifyChange();
                            }
                        } else if (reg is SRegister stringReg) {
                            var lastVal = stringReg.Value;
                            var readout = (await interf.ReadStringRegister(stringReg)).Register.Value;
                            if (lastVal != readout) {
                                interf.InvokeRegisterChanged(stringReg);
                                stringReg.TriggerNotifyChange();
                            }

                        }

                    }


                }

            }, interf.cTokenAutoUpdater.Token);

            return Task.CompletedTask;

        }

    }

}
