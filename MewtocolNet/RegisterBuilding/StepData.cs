﻿using MewtocolNet.PublicEnums;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Reflection;

namespace MewtocolNet.RegisterBuilding {

    internal class StepData {

        //for referencing the output at builder level
        internal Action<IRegister> registerOut;

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

    }

}
