using ColossalFramework;
using HarmonyLib;
using CSM.ExtraLandscapingTools.Surface;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches TerrainManager.GetSurfaceCell to overlay SurfaceManager data onto terrain rendering.
    /// This is what makes painted surfaces actually appear in the game world.
    /// Ported directly from SurfacePainter's TerrainManagerDetour.
    /// </summary>
    [HarmonyPatch(typeof(TerrainManager), "GetSurfaceCell")]
    public static class TerrainManagerPatch
    {
        public static void Postfix(ref TerrainManager.SurfaceCell __result, int x, int z)
        {
            if (!Singleton<SurfaceManager>.exists)
                return;

            __result = UpdateCell(__result, x, z);
        }

        private static TerrainManager.SurfaceCell UpdateCell(TerrainManager.SurfaceCell surfaceCell, int x, int z)
        {
            var surfaceItem = SurfaceManager.instance.GetSurfaceItem(z, x);
            if (!surfaceItem.overrideExisting &&
                (surfaceCell.m_field >= byte.MaxValue / 2 || surfaceCell.m_gravel >= byte.MaxValue / 2 ||
                 surfaceCell.m_pavementA >= byte.MaxValue / 2 || surfaceCell.m_pavementB >= byte.MaxValue / 2 ||
                 surfaceCell.m_ruined >= byte.MaxValue / 2 || surfaceCell.m_clipped >= byte.MaxValue / 2))
            {
                return surfaceCell;
            }
            if (surfaceItem.surface == TerrainModify.Surface.Gravel)
            {
                surfaceCell.m_gravel = byte.MaxValue;
                surfaceCell.m_field = 0;
                surfaceCell.m_pavementB = 0;
                surfaceCell.m_ruined = 0;
                surfaceCell.m_pavementA = 0;
            }
            if (surfaceItem.surface == TerrainModify.Surface.PavementA)
            {
                surfaceCell.m_pavementA = byte.MaxValue;
                surfaceCell.m_field = 0;
                surfaceCell.m_pavementB = 0;
                surfaceCell.m_ruined = 0;
                surfaceCell.m_gravel = 0;
            }
            if (surfaceItem.surface == TerrainModify.Surface.PavementB)
            {
                surfaceCell.m_pavementB = byte.MaxValue;
                surfaceCell.m_field = 0;
                surfaceCell.m_gravel = 0;
                surfaceCell.m_ruined = 0;
                surfaceCell.m_pavementA = 0;
            }
            if (surfaceItem.surface == TerrainModify.Surface.Field)
            {
                surfaceCell.m_field = byte.MaxValue;
                surfaceCell.m_gravel = 0;
                surfaceCell.m_pavementB = 0;
                surfaceCell.m_ruined = 0;
                surfaceCell.m_pavementA = 0;
            }
            if (surfaceItem.surface == TerrainModify.Surface.Ruined)
            {
                surfaceCell.m_ruined = byte.MaxValue;
                surfaceCell.m_field = 0;
                surfaceCell.m_pavementB = 0;
                surfaceCell.m_gravel = 0;
                surfaceCell.m_pavementA = 0;
            }
            if (surfaceItem.surface == TerrainModify.Surface.Clip)
            {
                surfaceCell.m_ruined = 0;
                surfaceCell.m_field = 0;
                surfaceCell.m_pavementB = 0;
                surfaceCell.m_gravel = 0;
                surfaceCell.m_pavementA = 0;
            }
            return surfaceCell;
        }
    }
}
