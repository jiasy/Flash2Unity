using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;
using LitJson;

public class OtherUtils {
    //cache
    private static Dictionary<string, Sprite> _spriteCacheDict = new Dictionary<string, Sprite> ();
    private static Dictionary<string, Sprite> _spriteInAltasCacheDict = new Dictionary<string, Sprite> ();

    public static bool isAContainsB (string a_, string b_, StringComparison comp_ = StringComparison.OrdinalIgnoreCase) {
        return a_.IndexOf (b_, comp_) >= 0;
    }

    public static bool isInt (string str_) {
        return Regex.IsMatch (str_, @"^\d+$");
    }
    public static int toInt (string str_) {
        return int.Parse (str_);
    }
    public static Component getComponentAndAddTo (Type t_, MonoBehaviour component_) {
        return getComponentAndAddTo (t_, component_.gameObject);
    }
    public static Component getComponentAndAddTo (Type t_, GameObject parent_) {
        return getComponentAndAddTo (t_, parent_.transform);
    }
    public static Component getComponentAndAddTo (Type t_, Transform parentTrans_) {
        Component _component = getComponent (t_);
        _component.transform.parent = parentTrans_;
        return _component;
    }
    public static Component getComponent (Type t_, object[] parameters_ = null) {
        return (new GameObject ()).AddComponent (t_);
    }

    public static void cacheSpriteInFolder (string folderPathInResources_) {
        Object[] _sprites = Resources.LoadAll (folderPathInResources_, typeof (UnityEngine.Sprite));
        for (int _idx = 0;_idx < _sprites.Length;_idx++) {
            var _sp = _sprites[_idx];
            _spriteCacheDict[_sp.name] = _sp as UnityEngine.Sprite;
        }
    }

    public static void cacheSpriteFromSpriteAltasInFolder (string folderPathInResources_) {
        Object[] _spriteAltas = Resources.LoadAll (folderPathInResources_, typeof (UnityEngine.U2D.SpriteAtlas));
        Sprite[] _spriteArray;
        UnityEngine.U2D.SpriteAtlas _spAltas;
        for (int _idx = 0;_idx < _spriteAltas.Length;_idx++) {
            _spAltas = _spriteAltas[_idx] as UnityEngine.U2D.SpriteAtlas;
            _spriteArray = new Sprite[_spAltas.spriteCount];
            _spAltas.GetSprites (_spriteArray);
            for (int _idxInside = 0;_idxInside < _spriteArray.Length;_idxInside++) {
                var _sp = _spriteArray[_idxInside];//Cache sprites in spriteAltas.
                _spriteInAltasCacheDict[_sp.name] = _sp;
            }
        }
    }

    public static Sprite getSpriteByName (string spriteName_, bool isFromAltas_) {
        Sprite _sprite =null;
        if (isFromAltas_) {
            if (!_spriteInAltasCacheDict.TryGetValue (spriteName_ + "(Clone)", out _sprite)) {
                Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                    spriteName_ + " : there is no sprite with this name.In sprite altas."
                );
            }
        }
        if(_sprite == null){
            if (!_spriteCacheDict.TryGetValue (spriteName_, out _sprite)) {
                Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                    spriteName_ + " : there is no sprite with this name."
                );
            }
        }
        return _sprite;
    }

    //get float value from LitJson.
    public static float getFloat (JsonData valueData_) {
        if (valueData_.IsLong) {
            return (float) Convert.ToDouble ((valueData_ as IJsonWrapper).GetLong ());
        } else if (valueData_.IsInt) {
            return (float) Convert.ToDouble ((valueData_ as IJsonWrapper).GetInt ());
        } else {
            return (float) (valueData_ as IJsonWrapper).GetDouble ();
        }
    }

    //cache property's value.
    public static void setTo (float[, ] propertyInFrames_, int propertyIdx_, float[] valueList_) {
        for (var _idx = 0; _idx < valueList_.Length; _idx++) {
            propertyInFrames_[propertyIdx_, _idx] = valueList_[_idx];
        }
    }

    //Expand value list
    public static float[] propertyToList (JsonData propertyInfo_, int frameLast_, float mult_ = 1f) {
        float[] _propertyInFrames = new float[frameLast_]; //Array's length is last frames.
        int _currentIndx = 0; //Current real index.
        for (int _idx = 0; _idx < propertyInfo_.Count; _idx += 2) {
            float _value = getFloat (propertyInfo_[_idx]) * mult_;
            int _frameLast = (propertyInfo_[_idx + 1] as IJsonWrapper).GetInt ();
            for (int _lastCount = 0; _lastCount < _frameLast; _lastCount++) { //Expand property values.
                if (_currentIndx < frameLast_) {
                    _propertyInFrames[_currentIndx] = _value;
                }
                _currentIndx++;
            }
        }
        return _propertyInFrames;
    }
}