using System.Collections;
using System.IO;
using Boo.Lang;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class FlashAssetPostprocessor : AssetPostprocessor {
    public static string pngExportFromFlashPath = FlashUtils.assetsFolderPath +"pngs/";
    public static string jsonExportFromFlashPath = FlashUtils.assetsFolderPath +"jsons/";
    private static int _maxTextureSize = 2048;
    private static int _compressionQuality = 100;
    private static bool _crunchedCompression = false;
    void OnPreprocessTexture () {
        string _dirName = System.IO.Path.GetDirectoryName (assetPath);
        if (OtherUtils.isAContainsB (_dirName, pngExportFromFlashPath)) {
            string[] _strSplit = System.Text.RegularExpressions.Regex.Split (_dirName, pngExportFromFlashPath);
            string _altasFolderName = _strSplit[1];
            string _altasName = _altasFolderName + ".spriteatlas";
            string _altasPath = _dirName + "/" + _altasName;
            if (!File.Exists (_altasPath)) { //Check atlas.
                createSpriteAltas (pngExportFromFlashPath + _altasFolderName, _altasName);
            }
            TextureImporter textureImporter = (TextureImporter) assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
        }
    }

    private void createSpriteAltas (string altasFolderPath_, string altasName_) {
        //New altas file.
        SpriteAtlas _atlas = new SpriteAtlas ();
        SpriteAtlasPackingSettings _packSetting = new SpriteAtlasPackingSettings () {
            blockOffset = 1,
            enableRotation = true,
            enableTightPacking = true,
            padding = 2,
        };
        _atlas.SetPackingSettings (_packSetting);
        SpriteAtlasTextureSettings _textureSetting = new SpriteAtlasTextureSettings () {
            readable = false,
            generateMipMaps = false,
            sRGB = true,
            filterMode = FilterMode.Bilinear,
        };
        _atlas.SetTextureSettings (_textureSetting);
        TextureImporterPlatformSettings _platformSetting = new TextureImporterPlatformSettings () {
            maxTextureSize = _maxTextureSize,
            format = TextureImporterFormat.Automatic,
            crunchedCompression = _crunchedCompression,
            textureCompression = TextureImporterCompression.Compressed,
            compressionQuality = _compressionQuality,
        };
        _atlas.SetPlatformSettings (_platformSetting);
        //Create altas file.
        AssetDatabase.CreateAsset (_atlas, altasFolderPath_ + "/" + altasName_);
        //Get folder pack with it.
        Object _folderObj = AssetDatabase.LoadAssetAtPath (altasFolderPath_, typeof (Object));
        _atlas.Add (new [] { _folderObj });
        AssetDatabase.SaveAssets ();
    }
    static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        string[] _strSplit;
        string _fileName;
        List<string> _jsonList = new List<string> ();
        for (int _idx = 0;_idx < importedAssets.Length;_idx++) {
            string _importedAsset = importedAssets[_idx];
            if (
                OtherUtils.isAContainsB (_importedAsset, jsonExportFromFlashPath) &&
                OtherUtils.isAContainsB (_importedAsset, ".json")
            ) {
                _strSplit = System.Text.RegularExpressions.Regex.Split (_importedAsset, jsonExportFromFlashPath);
                _fileName = _strSplit[1];
                _jsonList.Add (_fileName);
            }
        }
//        for (int i = 0; i < deletedAssets.Length; i++) {
//            Debug.Log ("Deleted Asset: " + deletedAssets[i]);
//        }
//        for (int i = 0; i < movedAssets.Length; i++) {
//            Debug.Log ("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
//        }
    }
}