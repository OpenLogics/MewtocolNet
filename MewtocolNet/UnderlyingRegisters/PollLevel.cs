using System.Collections.Generic;

namespace MewtocolNet.UnderlyingRegisters {

    internal class PollLevel {

        internal int lastReadTimeMs = 0; 

        internal PollLevel (int wrSize, int dtSize) {

            externalRelayInAreas = new List<WRArea>(wrSize * 16);
            externalRelayOutAreas = new List<WRArea>(wrSize * 16);
            internalRelayAreas = new List<WRArea>(wrSize * 16);
            dataAreas = new List<DTArea>(dtSize);

        }

        internal int level;

        // WR areas are n of words, each word has 2 bytes representing the "special address component"

        //X WR
        internal List<WRArea> externalRelayInAreas;

        //Y WR
        internal List<WRArea> externalRelayOutAreas;

        //R WR
        internal List<WRArea> internalRelayAreas;

        //DT
        internal List<DTArea> dataAreas;

    }

}
