using MewtocolNet.Registers;
using System;
using System.Threading.Tasks;

namespace MewtocolNet {
    internal interface IRegisterInternal {

        event Action<object> ValueChanged;

        //props

        MewtocolInterface AttachedInterface { get; }

        RegisterType RegisterType { get; }

        string Name { get; }

        object Value { get; }

        uint MemoryAddress { get; }

        // setters

        void WithCollectionType(Type colType);

        void SetValueFromPLC(object value);

        void ClearValue();

        // Accessors

        Type GetCollectionType();

        string GetRegisterString();

        string GetCombinedName();

        string GetContainerName();

        string GetMewName();

        byte? GetSpecialAddress();

        string GetStartingMemoryArea();

        string GetValueString();

        string BuildMewtocolQuery();

        uint GetRegisterAddressLen();

        string GetRegisterWordRangeString();

        //others

        void TriggerNotifyChange();

        Task<object> ReadAsync();

        Task<bool> WriteAsync(object data);

        string ToString();

        string ToString(bool detailed);

    }

}
