using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Examples.WPF.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged {

    public event PropertyChangedEventHandler? PropertyChanged;

    public void PropChange(string _name) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_name));
    }

    protected void OnPropChange([CallerMemberName] string propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
