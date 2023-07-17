using MewtocolNet.PublicEnums;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Reflection;

namespace MewtocolNet.RegisterBuilding {

    internal class BaseStepData {

        internal Action<object> registerOut;

        internal RegisterBuildSource buildSource = RegisterBuildSource.Anonymous;

        internal bool wasAddressStringRangeBased;
        internal string originalParseStr;
        internal string name;
        internal RegisterType regType;
        internal uint memAddress;
        internal byte specialAddress;
        internal Type dotnetVarType;

        //optional
        internal uint? byteSizeHint;
        internal uint? perElementByteSizeHint;
        internal int[] arrayIndicies;

        internal int pollLevel = 1;

        //only for building from attributes
        internal RegisterCollection regCollection;
        internal PropertyInfo boundProperty;

        internal string typeDef;

        internal void InvokeBuilt(Register reg) {

            registerOut.Invoke(reg);

            //var selftype = this.GetType();

            //if ((selftype.IsGenericType && selftype.GetGenericTypeDefinition() == typeof(StepData<>))) {

            //    var field = selftype.GetField("registerOut", BindingFlags.NonPublic | BindingFlags.Instance);

            //    var generic = typeof(IRegister<>).MakeGenericType()

            //    var action = Action.CreateDelegate(typeof(IRegister<T>));

            //    field.SetValue(this,);

            //} 

        }

    }

    internal class StepData<T> : BaseStepData {

        //for referencing the output at builder level
        //internal Action<IRegister<T>> registerOut;

    }

    internal class StepData : BaseStepData {

        //for referencing the output at builder level
        //internal Action<IRegister> registerOut;

    }

}
