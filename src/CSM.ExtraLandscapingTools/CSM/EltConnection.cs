using System;
using CSM.API;
using CSM.ExtraLandscapingTools.Mod;
using CSM.ExtraLandscapingTools.Utils;
using Log = CSM.ExtraLandscapingTools.Utils.Log;

namespace CSM.ExtraLandscapingTools.CSM
{
    public class EltConnection : Connection
    {
        public EltConnection()
        {
            Name = ModMetadata.ModName;
            Enabled = true;
            ModClass = typeof(MyUserMod);
            CommandAssemblies.Add(typeof(EltConnection).Assembly);
        }

        public override void RegisterHandlers()
        {
            Log.Info("CSM connection registered.");
        }

        public override void UnregisterHandlers()
        {
            Log.Info("CSM connection unregistered.");
        }
    }
}
