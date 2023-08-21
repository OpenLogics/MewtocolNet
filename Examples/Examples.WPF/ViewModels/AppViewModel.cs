using Examples.WPF.RegisterCollections;
using MewtocolNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.WPF.ViewModels {

    public class AppViewModel : ViewModelBase {

        private IPlc? plc;
        private TestRegisterCollection testRegCollection = null!;

        public bool PlcIsNull => plc == null;

        public bool PlcIsNotNull => plc != null;

        public IPlc? Plc {
            get => plc; 
            set {
                plc = value;
                OnPropChange();
                OnPropChange(nameof(PlcIsNull));
                OnPropChange(nameof(PlcIsNotNull));
            }
        }

        public TestRegisterCollection TestRegCollection { 
            get => testRegCollection; 
            set {
                testRegCollection = value;
                OnPropChange();
            }
        }

    }

}
