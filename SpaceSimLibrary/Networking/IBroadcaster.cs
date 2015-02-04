using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaceSimLibrary.Networking
{
    public interface IBroadcaster
    {
        void Broadcast();
        void ProcessBroadcastQueue();
    }
}
