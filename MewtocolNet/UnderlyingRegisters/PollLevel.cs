using System.Collections.Generic;

namespace MewtocolNet.UnderlyingRegisters {

    internal class PollLevel {

        internal int lastReadTimeMs = 0;

        internal int level;

        // WR areas are n of words, each word has 2 bytes representing the "special address component"

        //X WR
        internal List<AreaBase> externalRelayInAreas;

        //Y WR
        internal List<AreaBase> externalRelayOutAreas;

        //R WR
        internal List<AreaBase> internalRelayAreas;

        //DT
        internal List<AreaBase> dataAreas;

        internal PollLevel(int wrSize, int dtSize) {

            externalRelayInAreas = new List<AreaBase>(wrSize * 16);
            externalRelayOutAreas = new List<AreaBase>(wrSize * 16);
            internalRelayAreas = new List<AreaBase>(wrSize * 16);
            dataAreas = new List<AreaBase>(dtSize);

        }

        internal IEnumerable<AreaBase> GetAllAreas () {

            List<AreaBase> combined = new List<AreaBase>();

            combined.AddRange(internalRelayAreas);
            combined.AddRange(externalRelayInAreas);    
            combined.AddRange(externalRelayOutAreas);
            combined.AddRange(dataAreas);

            return combined;

        }

    }

}
