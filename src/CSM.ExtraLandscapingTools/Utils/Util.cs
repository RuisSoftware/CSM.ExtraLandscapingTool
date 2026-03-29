using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.Utils
{
    public static class Util
    {
        public static Texture2D LoadTextureFromAssembly(Assembly assembly, string path, string textureName, bool readOnly = true)
        {
            try
            {
                using (var textureStream = assembly.GetManifestResourceStream(path))
                {
                    return LoadTextureFromStream(readOnly, textureStream, textureName);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public static UITextureAtlas CreateAtlasFromResources(List<string> baseIconNames)
        {
            var names = GetIconNames(baseIconNames);
            var sprites = Load<Texture2D>(names);
            return CreateAtlas(sprites);
        }

        public static UITextureAtlas CreateAtlasFromEmbeddedResources(Assembly assembly, string basePath, List<string> baseIconNames)
        {
            var names = GetIconNames(baseIconNames);
            var sprites = new Texture2D[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                sprites[i] = LoadTextureFromAssembly(assembly, $"{basePath}.{names[i]}.png", names[i], false);
            }
            return CreateAtlas(sprites);
        }

        private static string[] GetIconNames(List<string> baseIconNames)
        {
            var names = new string[baseIconNames.Count * 5];
            var i = 0;
            foreach (var baseIconName in baseIconNames)
            {
                names[i * 5] = baseIconName;
                names[i * 5 + 1] = baseIconName + "Focused";
                names[i * 5 + 2] = baseIconName + "Hovered";
                names[i * 5 + 3] = baseIconName + "Pressed";
                names[i * 5 + 4] = baseIconName + "Disabled";
                i++;
            }
            return names;
        }

        public static UITextureAtlas CreateAtlas(Texture2D[] sprites)
        {
            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.material = new Material(GetUIAtlasShader());

            Texture2D texture = new Texture2D(0, 0);
            Rect[] rects = texture.PackTextures(sprites, 0);

            for (int i = 0; i < rects.Length; ++i)
            {
                Texture2D sprite = sprites[i];
                Rect rect = rects[i];

                UITextureAtlas.SpriteInfo spriteInfo = new UITextureAtlas.SpriteInfo();
                spriteInfo.name = sprite.name;
                spriteInfo.texture = sprite;
                spriteInfo.region = rect;
                spriteInfo.border = new RectOffset();

                atlas.AddSprite(spriteInfo);
            }
            atlas.material.mainTexture = texture;
            return atlas;
        }

        private static Shader GetUIAtlasShader()
        {
            return UIView.GetAView().defaultAtlas.material.shader;
        }

        private static Texture2D LoadTextureFromStream(bool readOnly, Stream textureStream, string textureName = null)
        {
            var buf = new byte[textureStream.Length];
            textureStream.Read(buf, 0, buf.Length);
            textureStream.Close();
            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            tex.LoadImage(buf);
            tex.Apply(false, readOnly);
            tex.name = textureName ?? Guid.NewGuid().ToString();
            return tex;
        }

        public static void AddLocale(string idBase, string key, string title, string description)
        {
            var localeField = typeof(LocaleManager).GetField("m_Locale", BindingFlags.NonPublic | BindingFlags.Instance);
            var locale = (Locale)localeField.GetValue(SingletonLite<LocaleManager>.instance);
            var localeKey = new Locale.Key() { m_Identifier = $"{idBase}_TITLE", m_Key = key };
            if (!locale.Exists(localeKey))
            {
                locale.AddLocalizedString(localeKey, title);
            }
            localeKey = new Locale.Key() { m_Identifier = $"{idBase}_DESC", m_Key = key };
            if (!locale.Exists(localeKey))
            {
                locale.AddLocalizedString(localeKey, description);
            }
            localeKey = new Locale.Key() { m_Identifier = $"{idBase}", m_Key = key };
            if (!locale.Exists(localeKey))
            {
                locale.AddLocalizedString(localeKey, description);
            }
        }

        public static Type FindType(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types.Where(type => type.Name == className))
                    {
                        Log.Info($"FindType: found {className} in assembly {assembly.GetName().Name}");
                        return type;
                    }
                }
                catch
                {
                    // ignored
                }
            }
            Log.Warn($"FindType: could not find {className}");
            return null;
        }

        public static T Load<T>(string name) where T : UnityEngine.Object
        {
            T[] ts = Load<T>(new string[] { name });
            if (ts.Length >= 1)
            {
                return ts[0];
            }
            else
            {
                return null;
            }
        }

        public static T[] Load<T>(string[] names) where T : UnityEngine.Object
        {
            List<T> ts = new List<T>();
            foreach (T t in Resources.FindObjectsOfTypeAll<T>())
            {
                if (Array.Exists(names, n => n == t.name))
                {
                    ts.Add(t);
                }
            }
            return ts.ToArray();
        }

        public static T GetPrivate<T>(object o, string fieldName)
        {
            var field = o.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field.GetValue(o);
        }

        public static void SetPrivate(object o, string fieldName, object value)
        {
            var field = o.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(o, value);
        }

        public static bool IsModActive(string modName)
        {
            var plugins = PluginManager.instance.GetPluginsInfo();
            return (from plugin in plugins.Where(p => p.isEnabled)
                    select plugin.GetInstances<IUserMod>() into instances
                    where instances.Any()
                    select instances[0].Name into name
                    where name == modName
                    select name).Any();
        }

        public static bool IsModAssemblyActive(string assemblyName)
        {
            return (from plugin in PluginManager.instance.GetPluginsInfo()
                    from assembly in plugin.GetAssemblies()
                    where assembly.GetName().Name.Equals(assemblyName) && plugin.isEnabled
                    select plugin).Any();
        }

        public static void CopySprite(string originalName, string newName, UITextureAtlas destAtlas)
        {
            try
            {
                var spriteInfo = UIView.GetAView().defaultAtlas[originalName];
                if (spriteInfo == null) return;
                destAtlas.AddSprite(new UITextureAtlas.SpriteInfo
                {
                    border = spriteInfo.border,
                    name = newName,
                    region = spriteInfo.region,
                    texture = spriteInfo.texture
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
