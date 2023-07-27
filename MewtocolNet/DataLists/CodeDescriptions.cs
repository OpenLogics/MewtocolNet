using System.Collections.Generic;

namespace MewtocolNet.DataLists {

    internal class CodeDescriptions {

        internal static Dictionary<int, string> Error = new Dictionary<int, string> {

            {21, "NACK error"},
            {22, "WACK error"},
            {23, "Station number overlap"},
            {24, "Transmission error"},
            {25, "Hardware error"},
            {26, "Station number setting error"},
            {27, "Frame over error"},
            {28, "No response error"},
            {29, "Buffer close error"},
            {30, "Timeout error"},
            {32, "Transmission impossible"},
            {33, "Communication stop"},
            {36, "No local station"},
            {38, "Other com error"},
            {40, "BCC error"},
            {41, "Format error"},
            {42, "Not supported error"},
            {43, "Procedure error"},
            {50, "Link setting error"},
            {51, "Simultanious operation error"},
            {52, "Sending disable error"},
            {53, "Busy error"},
            {60, "Paramter error"},
            {61, "Data error"},
            {62, "Registration error"},
            {63, "Mode error"},
            {66, "Adress error"},
            {67, "No data error"},
            {72, "Timeout"},
            {73, "Timeout"},

        };

    }

}