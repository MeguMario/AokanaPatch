﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace AokanaPatch
{
    [BepInPlugin("org.bepinex.plugins.aokanapatch", "Aokana Patch Plugin", "1.0.0.0")]
    public class AokanaPatch : BaseUnityPlugin
    {
        // Awake is called once when both the game and the plug-in are loaded
        void Awake()
        {
            UnityEngine.Debug.Log("Aokana patcher is active!");
            Harmony harmony = new Harmony("org.bepinex.plugins.aokanapatch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(AssetIO), "FileReadPGroup")]
    class AssetIO_Patch
    {
        static byte[] getEncryptedFile(string path)
        {
            if (EngineMain.engine.pkmain == null)
            {
                return null;
            }
            return EngineMain.engine.pkmain.Get(path.ToLower());
        }
        static bool Prefix(ref byte[] __result, string path)
        {

            if (path[0] == '/')
            {
                path = path.Substring(1);
            }
            string fullPath = Path.Combine(Application.dataPath, path);
            if (File.Exists(fullPath))
            {
                //UnityEngine.Debug.Log(fullPath);
                FileStream fStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                byte[] blob = new byte[fStream.Length];
                fStream.Read(blob, 0, blob.Length);
                __result = blob;
                return false;
                
            }
            byte[] blob2 = getEncryptedFile(path);
            if (blob2 == null) { return false; }

            if (blob2.Length == 0)
            {
                //If file is not found in *.dat files or in unencrypted files, don't let original method run
                return false;
            } else
            {
                //If file is found in *.dat files but not in unencrypted files, let the original method run (no replacement was found in unencrypted files)
                return true;
            }
            
        }
    }

    class Font_patch_generic
    {
        public static AssetBundle assets;
        public static string path = Path.Combine(Application.dataPath, "font");
        public static TMP_FontAsset getFont(TextMeshProUGUI advtext)
        {
            TMP_FontAsset font = assets.LoadAsset<TMP_FontAsset>("font.asset");
            return font;
        }
    }

    //Load the font on startup
    [HarmonyPatch(typeof(UIAdv), "Start")]
    class Font_patch_startup
    {
        static void Postfix(ref TextMeshProUGUI ___advtext)
        {
            if (File.Exists(Font_patch_generic.path))
            {
                Font_patch_generic.assets = AssetBundle.LoadFromFile(Font_patch_generic.path);
            }
        }
    }
    //Make sure font is always loaded instead of anything else
    [HarmonyPatch(typeof(UIAdv), "ChangeFont")]
    class Font_patch_change
    {
        static void Postfix(ref TextMeshProUGUI ___advtext) 
        {
            if (File.Exists(Font_patch_generic.path))
            {
                TMP_FontAsset font = Font_patch_generic.getFont(___advtext);
                ___advtext.font = font;
            }
        }
    }
}