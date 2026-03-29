using System;
using ColossalFramework;
using ColossalFramework.IO;
using CSM.ExtraLandscapingTools.Utils;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.Surface
{
    public class SurfaceManager : Singleton<SurfaceManager>
    {
        public static readonly int CELL_SIZE = 4;
        public static readonly int GRID_SIZE = 4320;
        public static readonly int GRID_PER_AREA = 480;
        private static readonly int STEP = 16;

        private static readonly SurfaceItem EMPTY_ITEM = new SurfaceItem
        {
            overrideExisting = false,
            surface = TerrainModify.Surface.None
        };

        private SurfaceItem[] m_surfaces;
        public bool isEightyOneEnabled;

        public void Setup()
        {
            Reset();
            try
            {
                isEightyOneEnabled = Util.IsModActive("81 Tiles (Fixed for C:S 1.2+)")
                                     || Util.IsModAssemblyActive("EightyOne2");
                Log.Info($"81 Tiles enabled={isEightyOneEnabled}");
            }
            catch (Exception e)
            {
                isEightyOneEnabled = false;
                Debug.LogException(e);
                Log.Error("Failed to figure out whether 81 Tiles was enabled");
            }
        }

        public void Reset()
        {
            m_surfaces = null;
        }

        public static void UpdateWholeMap()
        {
            SimulationManager.instance.AddAction(() =>
            {
                int offset = SurfaceManager.instance.isEightyOneEnabled ? 0 : 120 * 2;
                for (var i = offset; i < TerrainManager.RAW_RESOLUTION - offset; i += STEP)
                {
                    for (var j = offset; j < TerrainManager.RAW_RESOLUTION - offset; j += STEP)
                    {
                        TerrainModify.BeginUpdateArea();
                        TerrainModify.UpdateArea(i, j, i + STEP, j + STEP, false, true, false);
                        TerrainModify.EndUpdateArea();
                    }
                }
            });
        }

        public SurfaceItem GetSurfaceItem(int z, int x)
        {
            x = Mathf.Min(x, GRID_SIZE - 1);
            z = Mathf.Min(z, GRID_SIZE - 1);

            if (isEightyOneEnabled)
            {
                return Surfaces[z * GRID_SIZE + x];
            }
            else
            {
                if (x < GRID_PER_AREA * 2 || x >= GRID_PER_AREA * 7 || z < GRID_PER_AREA * 2 || z >= GRID_PER_AREA * 7)
                {
                    return EMPTY_ITEM;
                }
            }
            return Surfaces[(z - 2 * GRID_PER_AREA) * (GRID_SIZE - GRID_PER_AREA * 4) + x - 2 * GRID_PER_AREA];
        }

        public void SetSurfaceItem(int z, int x, TerrainModify.Surface surface, bool overrideExisting)
        {
            x = Mathf.Min(x, GRID_SIZE - 1);
            z = Mathf.Min(z, GRID_SIZE - 1);

            var item = new SurfaceItem
            {
                surface = surface,
                overrideExisting = overrideExisting
            };
            if (isEightyOneEnabled)
            {
                Surfaces[z * GRID_SIZE + x] = item;
                return;
            }
            if (x < GRID_PER_AREA * 2 || x >= GRID_PER_AREA * 7 || z < GRID_PER_AREA * 2 || z >= GRID_PER_AREA * 7)
            {
                return;
            }
            Surfaces[(z - 2 * GRID_PER_AREA) * (GRID_SIZE - GRID_PER_AREA * 4) + x - 2 * GRID_PER_AREA] = item;
        }

        private SurfaceItem[] Surfaces
        {
            get
            {
                var eightyOneSize = GRID_SIZE * GRID_SIZE;
                var defaultSize = (GRID_SIZE - GRID_PER_AREA * 4) * (GRID_SIZE - GRID_PER_AREA * 4);
                if (m_surfaces == null)
                {
                    m_surfaces = isEightyOneEnabled ? new SurfaceItem[eightyOneSize] : new SurfaceItem[defaultSize];
                }
                if (isEightyOneEnabled && m_surfaces.Length == defaultSize)
                {
                    var newSurfaces = new SurfaceItem[eightyOneSize];
                    MigrateItems(newSurfaces, true);
                    m_surfaces = newSurfaces;
                }
                else if (!isEightyOneEnabled && m_surfaces.Length == eightyOneSize)
                {
                    var newSurfaces = new SurfaceItem[defaultSize];
                    MigrateItems(newSurfaces, false);
                    m_surfaces = newSurfaces;
                }
                return m_surfaces;
            }
        }

        private void MigrateItems(SurfaceItem[] newSurfaces, bool toEightyOne)
        {
            var defaultRowLength = 5 * GRID_PER_AREA;
            var eightyOneRowLength = 9 * GRID_PER_AREA;
            var offset = 2 * eightyOneRowLength * GRID_PER_AREA + 2 * GRID_PER_AREA;
            for (var i = 0; i < defaultRowLength; i++)
            {
                var sourceStart = toEightyOne ? i * defaultRowLength : offset + i * eightyOneRowLength;
                var destStart = toEightyOne ? offset + i * eightyOneRowLength : i * defaultRowLength;
                Array.Copy(m_surfaces, sourceStart, newSurfaces, destStart, defaultRowLength);
            }
        }

        #region Encoding

        internal static byte GetSurfaceCode(TerrainModify.Surface surface)
        {
            switch (surface)
            {
                case TerrainModify.Surface.PavementA: return 1;
                case TerrainModify.Surface.PavementB: return 2;
                case TerrainModify.Surface.Ruined: return 3;
                case TerrainModify.Surface.Gravel: return 4;
                case TerrainModify.Surface.Field: return 5;
                case TerrainModify.Surface.Clip: return 6;
                default: return 0;
            }
        }

        internal static TerrainModify.Surface GetSurface(byte surfaceCode)
        {
            switch (surfaceCode)
            {
                case 1: return TerrainModify.Surface.PavementA;
                case 2: return TerrainModify.Surface.PavementB;
                case 3: return TerrainModify.Surface.Ruined;
                case 4: return TerrainModify.Surface.Gravel;
                case 5: return TerrainModify.Surface.Field;
                case 6: return TerrainModify.Surface.Clip;
                default: return TerrainModify.Surface.None;
            }
        }

        private static byte EncodeItems(SurfaceItem item1, SurfaceItem item2)
        {
            var over1 = (byte)(item1.overrideExisting ? 1 : 0);
            var number1 = (byte)(over1 | (GetSurfaceCode(item1.surface) << 1));
            var over2 = (byte)(item2.overrideExisting ? 1 : 0);
            var number2 = (byte)(over2 | (GetSurfaceCode(item2.surface) << 1));
            return (byte)(number1 | (number2 << 4));
        }

        private static SurfaceItem DecodeItem(byte data, int index)
        {
            var value = (data >> (index * 4)) & 0xF;
            var overrideExisting = (value & 0x1) == 1;
            var surface = GetSurface((byte)((value >> 1) & 0x7));
            return new SurfaceItem
            {
                overrideExisting = overrideExisting,
                surface = surface
            };
        }

        #endregion

        public struct SurfaceItem
        {
            public TerrainModify.Surface surface;
            public bool overrideExisting;
        }

        public class Data : IDataContainer
        {
            public void Serialize(DataSerializer s)
            {
                var items = instance.Surfaces;
                s.WriteInt32(items.Length);
                var @byte = EncodedArray.Byte.BeginWrite(s);
                for (var index = 0; index < items.Length; index += 2)
                    @byte.Write(EncodeItems(items[index], items[index + 1]));
                @byte.EndWrite();
            }

            public void Deserialize(DataSerializer s)
            {
                var arraySize = s.ReadInt32();
                instance.m_surfaces = new SurfaceItem[arraySize];
                var @byte = EncodedArray.Byte.BeginRead(s);
                for (var index = 0; index < arraySize / 2; ++index)
                {
                    var item = @byte.Read();
                    instance.m_surfaces[index * 2] = DecodeItem(item, 0);
                    instance.m_surfaces[index * 2 + 1] = DecodeItem(item, 1);
                }
                @byte.EndRead();
            }

            public void AfterDeserialize(DataSerializer s)
            {
            }
        }
    }
}
