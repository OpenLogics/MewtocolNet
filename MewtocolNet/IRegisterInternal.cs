using MewtocolNet.Registers;
using System;
using System.Threading.Tasks;

namespace MewtocolNet {
    internal interface IRegisterInternal {

        void WithCollectionType(Type colType);

        void SetValueFromPLC(object value);

        Task<object> ReadAsync(MewtocolInterface interf);

        Task<bool> WriteAsync(MewtocolInterface interf, object data);

    }

}
