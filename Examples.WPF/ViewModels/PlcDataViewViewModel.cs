using MewtocolNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.WPF.ViewModels;

public class PlcDataViewViewModel : ViewModelBase {

    public IPlc Plc => App.ViewModel.Plc!;

}