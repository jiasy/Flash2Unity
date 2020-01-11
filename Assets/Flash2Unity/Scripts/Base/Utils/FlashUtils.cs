using System;
using System.Collections.Generic;
using flash.display;
using LitJson;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public class FlashUtils {
    /*
    Flash/Animate IDE export folder 'Assets/Flash2Unity/Resources/flash/'
    'Base/Editor/FlashAssetPostprocessor' will automaticly create 'spriteatlas' file to pack png resources.
        When create png file:
            Assets/Flash2Unity/Resources/flash/pngs/altasName/altasName_pngName.png
        Will auto create:
            Assets/Flash2Unity/Resources/flash/pngs/altasName/altasName.spriteatlas
        To pack png in folder:
            Assets/Flash2Unity/Resources/flash/pngs/altasName/
    */
    public static string assetsFolderPath = "Assets/Flash2Unity/Resources/flash/";
    //Shader for all the spriteRenderers.
    public static Shader spriteRendererShader = Shader.Find ("Sprites/Default");
    
    //Timeline of MovieClip infos cache.
    public static Dictionary<string, MCTimeLineInfo> disDict = new Dictionary<string, MCTimeLineInfo> ();

    //Dis className-> frameName -> <beginFrame,endFrame>
    private static Dictionary<string, Dictionary<string, BeginEndFrame>> _frameNameRangeDict = new Dictionary<string, Dictionary<string, BeginEndFrame>> ();
    //Dis className-> frameInt -> frameName
    private static Dictionary<string, Dictionary<int, string>> _frameIntToNameDict = new Dictionary<string, Dictionary<int, string>> ();
    private static Dictionary<string, Dictionary<string, int>> _frameNameToIntDict = new Dictionary<string, Dictionary<string, int>> ();
    // className -> frameInt -> FrameLabel
    private static Dictionary<string, Dictionary<int, List<FrameLabel>>> _frameActionDict = new Dictionary<string, Dictionary<int, List<FrameLabel>>> ();
    private static flash.display.Sprite _spriteCache;
    private static MovieClip _movieClipCache;
    private static Transform _cacheContainerTransform;
    public enum PropertyType {
        x = 0,
        y = 1,
        scaleX = 2,
        scaleY = 3,
        rotation = 4,
        a = 5,
        r = 6,
        g = 7,
        b = 8
        //        ,
        //        visible=9,
        //        blur_blurX=10,
        //        blur_blurY=11,
        //        glow_r=12,
        //        glow_g=13,
        //        glow_b=14,
        //        glow_blurX=15,
        //        glow_blurY=16,
        //        glow_strength=17
    }

    public enum MovieClipType {
        className = 0,
        superClassName = 1,
        onStage = 2,
        childrenInfos = 3,
        totalFrames = 4,
        frameLabelInfos = 5
    }

    public enum ChildType {
        insName = 0,
        blendMode = 1,
        className = 2,
        superClassName = 3,
        childIndex = 4,
        beginFrame = 5,
        endFrame = 6,
        framePropertys = 7
        // ,
        // maskChildrenNumber = 8
    }

    public enum FrameType {
        frame = 0,
        targetPath = 1,
        name = 2,
        parameter = 3
    }

    //frame actions.
    public enum FrameNameType {
        none = -1,
        stop = 0,
        play = 1,
        nextFrame = 2,
        prevFrame = 3,
        gotoAndStop = 4,
        gotoAndPlay = 5,
        baseOverlay = 6,
        baseReplace = 7,
        popOverlay = 8,
        popReplace = 9,
        playBgSound = 10,
        playEffectSound = 11,
        callFunc = 12,
        eventHappen = 13,
        frameName = 14,
        remove = 15,
    }

    //Base Class from Flash
    public enum ClassType {
        SpriteInFlashIDE = 0,
        MovieClipInFlashIDE = 1,
        MovieClip = 2,
        Sprite = 3
    }
    public class MCTimeLineInfo {
        public string className;
        public int superClassName;
        public bool onStage;
        public int totalFrames;
        public ChildInfo[] childrenInfos;
        public FrameLabel[] frameLabels;
        public MovieClip MovieClipCache = null;
        //True when there is no child removed from timeline.
        public bool childrenNeverRemoveFromStage;
        //True when there are all spirte children.
        public bool allChildrenSprite;
    }

    public class ChildInfo {
        public string insName;
        public string blendMode;
        public string className;
        public int superClassName;
        public int childIndex;
        public int parentNumChildren;
        public int beginFrame;
        public int endFrame;
        public float[, ] propertyInFrames;
        public bool xyChanged;
        public bool sxyChanged;
        public bool roChanged;
        public bool aChanged;
        public bool rgbChanged;
        //public int maskChildrenNumber;
    }

    public class FrameLabel {
        public int frame;
        public string targetPath;
        public FrameNameType name;
        public string parameter;
    }
    public class BeginEndFrame {
        public int beginFrame;
        public int endFrame;
    }

    //Text file in 'Assets/Resources/flash/jsons'.
    public static void cacheMovieClipInFolder (string folderPathInResources_) {
        Object[] _textFiles = Resources.LoadAll (folderPathInResources_, typeof (TextAsset));

        for (int _idx = 0;_idx < _textFiles.Length;_idx++) {
            JsonData _jsonRoot = JsonMapper.ToObject ((_textFiles[_idx] as TextAsset).ToString ());
            cacheMovieClipInfo (_jsonRoot);
        } 
    }

    //Create object for 'GameObject.Instantiate()'
    public static void createCache () {
        if (_spriteCache == null) {
            _spriteCache = new GameObject ().AddComponent<flash.display.Sprite> ();
            _spriteCache.gameObject.name = "cache_FlashUtils_Sprite";
            _spriteCache.spriteRenderer = _spriteCache.GetComponent<SpriteRenderer> ();
            _spriteCache.spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _spriteCache.spriteRenderer.receiveShadows = false;
            _spriteCache.spriteRenderer.material = new Material (spriteRendererShader);
        }
        if (_movieClipCache == null) {
            _movieClipCache = new GameObject ().AddComponent<MovieClip> ();
            _movieClipCache.gameObject.name = "cache_FlashUtils_MovieClip";
        }
    }

    //MovieClip and Sprite 
    public static void setCacheContainerTransform (Transform trans_) {
        createCache ();
        _cacheContainerTransform = trans_;
        _cacheContainerTransform.gameObject.SetActive (false);
        _spriteCache.transform.parent = trans_;
        _movieClipCache.transform.parent = trans_;
    }

    //Get MovieClip by className.
    public static MovieClip getMovieClipByClassNameAndAddTo (string className_, GameObject gameObject_) {
        return getMovieClipByClassNameAndAddTo (className_, gameObject_.transform);
    }
    public static MovieClip getMovieClipByClassNameAndAddTo (string className_, Transform transform_) {
        MCTimeLineInfo _mcTimeLineInfo;
        if (!disDict.TryGetValue (className_, out _mcTimeLineInfo)) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "There is no config : " + className_ + " !"
            );
            return null;
        }

        MovieClip _mainMovieClip = getMovieClipByTimeLine (_mcTimeLineInfo);
        _mainMovieClip.resetByTimeLineInfo (_mcTimeLineInfo, null);
        _mainMovieClip.selfTrans.parent = transform_;
        _mainMovieClip.selfTrans.localPosition = new Vector3(0f, 0f, 0f);
        _mainMovieClip.selfTrans.localScale = new Vector3(1f,1f,1f);
        return _mainMovieClip;
    }

    public static flash.display.Sprite getPicNodeAndAddTo (MovieClip mainMovieClip_, Transform parent_) {
        flash.display.Sprite sprite = GameObject.Instantiate (_spriteCache);
        sprite.setMainMovieClip (mainMovieClip_);
        Transform _spTrans = sprite.transform;
        _spTrans.parent = parent_;
        _spTrans.localPosition = new Vector3(0f, 0f, 0f);
        _spTrans.localScale = new Vector3(1f,1f,1f);
        return sprite;
    }

    public static MovieClip getMovieClipByTimeLine (MCTimeLineInfo _mcTimeLineInfo) {
        MovieClip _movieClip;
        if (_mcTimeLineInfo.MovieClipCache == null) { //Every timeline info 
            Type type = Type.GetType (_mcTimeLineInfo.className);
            if (type != null) { //cache a DIY class instance.
                _movieClip = OtherUtils.getComponent (type) as MovieClip;
                _movieClip.gameObject.name = "cache_" + _mcTimeLineInfo.className;
                _movieClip.selfTrans.parent = _cacheContainerTransform;
                _mcTimeLineInfo.MovieClipCache =_movieClip;
            } else { //use normal cache MovieClip.
                _mcTimeLineInfo.MovieClipCache = _movieClipCache;
            }
        }
        _movieClip = GameObject.Instantiate (_mcTimeLineInfo.MovieClipCache);
        return _movieClip;
    }

    public static MovieClip getMovieClipByChildInfoAndAddTo (ChildInfo childInfo_, MovieClip mainMovieClip_, MovieClip parentMovieClip_) {
        MCTimeLineInfo _mcTimeLineInfo;
        if (!disDict.TryGetValue (childInfo_.className, out _mcTimeLineInfo)) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "there is no config " + childInfo_.className + " !"
            );
            return null;
        }
        MovieClip _movieClip = getMovieClipByTimeLine (_mcTimeLineInfo);
        _movieClip.resetByTimeLineInfo (_mcTimeLineInfo, mainMovieClip_);
        _movieClip.initByChildInfo (childInfo_, parentMovieClip_);
        _movieClip.transform.parent = parentMovieClip_.selfTrans;
        return _movieClip;
    }
    public static void cacheMovieClipInfo (JsonData value_) {
        string _className = (value_[(int) MovieClipType.className] as IJsonWrapper).GetString ();
        if (disDict.ContainsKey (_className)) {
            return;
        }
        MCTimeLineInfo _mcTimeLineInfo;
        JsonData _childInfos;
        ChildInfo _ci;
        JsonData _childInfo;
        JsonData _framePropertys;
        JsonData _frameLabelInfos;
        JsonData _frameLabelInfo;
        FrameLabel _fl;
        int lastFrames;

        _mcTimeLineInfo = new MCTimeLineInfo ();
        _mcTimeLineInfo.className = _className;
        _mcTimeLineInfo.superClassName = (value_[(int) MovieClipType.superClassName] as IJsonWrapper).GetInt ();
        _mcTimeLineInfo.onStage = (value_[(int) MovieClipType.onStage] as IJsonWrapper).GetBoolean ();
        _mcTimeLineInfo.totalFrames = (value_[(int) MovieClipType.totalFrames] as IJsonWrapper).GetInt ();
        _mcTimeLineInfo.childrenNeverRemoveFromStage = true;
        _mcTimeLineInfo.allChildrenSprite = true;

        _childInfos = value_[(int) MovieClipType.childrenInfos];
        int _numChildren = _childInfos.Count;
        _mcTimeLineInfo.childrenInfos = new ChildInfo[_numChildren];
        for (int _disIndex = 0; _disIndex < _numChildren; _disIndex++) {
            _ci = new ChildInfo ();
            _childInfo = _childInfos[_disIndex];
            _ci.insName = (_childInfo[(int) ChildType.insName] as IJsonWrapper).GetString ();
            _ci.blendMode = (_childInfo[(int) ChildType.blendMode] as IJsonWrapper).GetString ();
            _ci.className = (_childInfo[(int) ChildType.className] as IJsonWrapper).GetString ();
            _ci.superClassName = (_childInfo[(int) ChildType.superClassName] as IJsonWrapper).GetInt ();
            if (_ci.superClassName != (int) ClassType.Sprite) {
                _mcTimeLineInfo.allChildrenSprite = false;
            }
            _ci.childIndex = (_childInfo[(int) ChildType.childIndex] as IJsonWrapper).GetInt ();
            _ci.parentNumChildren = _numChildren;
            _ci.beginFrame = (_childInfo[(int) ChildType.beginFrame] as IJsonWrapper).GetInt ();
            _ci.endFrame = (_childInfo[(int) ChildType.endFrame] as IJsonWrapper).GetInt ();
            //_ci.maskChildrenNumber = (_childInfo[(int) ChildType.maskChildrenNumber] as IJsonWrapper).GetInt ();
            if (_ci.beginFrame != 1 || _ci.endFrame != _mcTimeLineInfo.totalFrames) {
                _mcTimeLineInfo.childrenNeverRemoveFromStage = false; //Any children remove from stage.set it to false.
            }
            lastFrames = _ci.endFrame - _ci.beginFrame + 1;

            //cache frame propertys
            _framePropertys = _childInfo[(int) ChildType.framePropertys];
            if (lastFrames == 1) {
                _ci.propertyInFrames = new float[9, 1];
                _ci.propertyInFrames[(int) PropertyType.x, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.x]) * 0.01f;
                _ci.propertyInFrames[(int) PropertyType.y, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.y]) * 0.01f;
                _ci.propertyInFrames[(int) PropertyType.scaleX, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.scaleX]);
                _ci.propertyInFrames[(int) PropertyType.scaleY, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.scaleY]);
                _ci.propertyInFrames[(int) PropertyType.rotation, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.rotation]);
                _ci.propertyInFrames[(int) PropertyType.r, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.r]);
                _ci.propertyInFrames[(int) PropertyType.g, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.g]);
                _ci.propertyInFrames[(int) PropertyType.b, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.b]);
                _ci.propertyInFrames[(int) PropertyType.a, 0] = OtherUtils.getFloat (_framePropertys[(int) PropertyType.a]);
            } else {
                _ci.propertyInFrames = new float[9, lastFrames];
                bool _xChanged = _framePropertys[(int) PropertyType.x].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.x, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.x], lastFrames, 0.01f));
                bool _yChanged = _framePropertys[(int) PropertyType.y].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.y, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.y], lastFrames, 0.01f));
                bool _sxChanged = _framePropertys[(int) PropertyType.scaleX].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.scaleX, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.scaleX], lastFrames));
                bool _syChanged = _framePropertys[(int) PropertyType.scaleY].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.scaleY, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.scaleY], lastFrames));
                _ci.roChanged = _framePropertys[(int) PropertyType.rotation].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.rotation, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.rotation], lastFrames));
                _ci.aChanged = _framePropertys[(int) PropertyType.a].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.a, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.a], lastFrames));
                bool _rChanged = _framePropertys[(int) PropertyType.r].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.r, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.r], lastFrames));
                bool _gChanged = _framePropertys[(int) PropertyType.g].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.g, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.g], lastFrames));
                bool _bChanged = _framePropertys[(int) PropertyType.b].Count != 2;
                OtherUtils.setTo (_ci.propertyInFrames, (int) PropertyType.b, OtherUtils.propertyToList (_framePropertys[(int) PropertyType.b], lastFrames));
                if (_xChanged || _yChanged) { _ci.xyChanged = true; }
                if (_sxChanged || _syChanged) { _ci.sxyChanged = true; }
                if (_rChanged || _gChanged || _bChanged) { _ci.rgbChanged = true; }
            }
            //cache child info by their childindex
            _mcTimeLineInfo.childrenInfos[_ci.childIndex] = _ci;
        }
        //Frame label -> actions
        _frameLabelInfos = value_[(int) MovieClipType.frameLabelInfos];
        int _frameLabelInfosCount = _frameLabelInfos.Count;
        _mcTimeLineInfo.frameLabels = new FrameLabel[_frameLabelInfosCount];
        for (int _frameLabelIndex = 0; _frameLabelIndex < _frameLabelInfosCount; _frameLabelIndex++) {
            _frameLabelInfo = _frameLabelInfos[_frameLabelIndex];
            _fl = new FrameLabel ();
            _fl.frame = (_frameLabelInfo[(int) FrameType.frame] as IJsonWrapper).GetInt ();
            _fl.targetPath = (_frameLabelInfo[(int) FrameType.targetPath] as IJsonWrapper).GetString ();
            _fl.name = (FrameNameType) (_frameLabelInfo[(int) FrameType.name] as IJsonWrapper).GetInt ();
            _fl.parameter = (_frameLabelInfo[(int) FrameType.parameter] as IJsonWrapper).GetString ();
            _mcTimeLineInfo.frameLabels[_frameLabelIndex] = _fl;
        }
        cacheFrameInfo (_mcTimeLineInfo);
        disDict[_mcTimeLineInfo.className] = _mcTimeLineInfo;

    }

    public static void cacheFrameInfo (MCTimeLineInfo _mcTimeLineInfoStruct) {
        var _className = _mcTimeLineInfoStruct.className;
        if (_frameNameRangeDict.ContainsKey (_className)) { //Check cache
            return;
        }
        _frameNameRangeDict[_className] = new Dictionary<string, BeginEndFrame> ();
        _frameIntToNameDict[_className] = new Dictionary<int, string> ();
        _frameNameToIntDict[_className] = new Dictionary<string, int> ();
        var _totalFrames = _mcTimeLineInfoStruct.totalFrames;
        string _lastFrameName = null;
        string _frameName;
        BeginEndFrame _currentBeginEnd;
        //Cache frame label infos : < frameName : [begin,end] >
        for (int _frameInt = 1; _frameInt <= _totalFrames; _frameInt++) {
            _frameName = getFrameNameByFrameInt (_mcTimeLineInfoStruct, _frameInt);
            if (_frameName != null) {
                _currentBeginEnd = new BeginEndFrame ();
                _currentBeginEnd.beginFrame = _frameInt;
                _frameNameRangeDict[_className][_frameName] = _currentBeginEnd;
                _frameIntToNameDict[_className][_frameInt] = _frameName;
                _frameNameToIntDict[_className][_frameName] = _frameInt;
                if (_lastFrameName != null) {
                    _currentBeginEnd = _frameNameRangeDict[_className][_lastFrameName];
                    _currentBeginEnd.endFrame = _frameInt - 1;
                }
                _lastFrameName = _frameName;
            }
        }
        if (_lastFrameName != null) {
            _currentBeginEnd = _frameNameRangeDict[_className][_lastFrameName];
            _currentBeginEnd.endFrame = _totalFrames;
        }

        //Cache actions
        _frameActionDict[_className] = new Dictionary<int, List<FrameLabel>> ();
        FrameLabel _frameLabel;
        for (int _frameInt = 1; _frameInt <= _totalFrames; _frameInt++) {
            for (int _idx = 0; _idx < _mcTimeLineInfoStruct.frameLabels.Length; _idx++) {
                _frameLabel = _mcTimeLineInfoStruct.frameLabels[_idx];
                if (_frameLabel.frame == _frameInt) {
                    if (
                        _frameLabel.name == FrameNameType.remove ||
                        _frameLabel.name == FrameNameType.stop ||
                        _frameLabel.name == FrameNameType.play ||
                        _frameLabel.name == FrameNameType.nextFrame ||
                        _frameLabel.name == FrameNameType.prevFrame ||
                        _frameLabel.name == FrameNameType.gotoAndPlay ||
                        _frameLabel.name == FrameNameType.gotoAndStop
                    ) { //is frame control actions
                        if (!_frameActionDict[_className].ContainsKey (_frameInt)) {
                            _frameActionDict[_className][_frameInt] = new List<FrameLabel> ();
                        }
                        _frameActionDict[_className][_frameInt].Add (_frameLabel);
                    }
                }
            }
        }
    }

    public static Dictionary<int, List<FrameLabel>> getFrameIntToFrameActionCache (MCTimeLineInfo _mcTimeLineInfoStruct) {
        return _frameActionDict[_mcTimeLineInfoStruct.className];
    }

    public static List<FrameLabel> getFrameActionsByFrameInt (MCTimeLineInfo _mcTimeLineInfoStruct, int frameInt_) {
        List<FrameLabel> _backFrameLabel;
        if (_frameActionDict[_mcTimeLineInfoStruct.className].TryGetValue (frameInt_, out _backFrameLabel)) {
            return _backFrameLabel;
        }
        return null;
    }

    public static int getFrameIntByString (MCTimeLineInfo _mcTimeLineInfoStruct, string str_) {
        if (OtherUtils.isInt (str_)) {
            return OtherUtils.toInt (str_);
        } else {
            return getFrameIntByFrameName (_mcTimeLineInfoStruct, str_);
        }
    }

    public static string getFrameIntLocalInFrameName (MCTimeLineInfo _mcTimeLineInfoStruct, int frameInt_) {
        Dictionary<string, BeginEndFrame> _beginEndDict = _frameNameRangeDict[_mcTimeLineInfoStruct.className];
        var _enume = _beginEndDict.GetEnumerator ();
        while (_enume.MoveNext ()) {
            if (_enume.Current.Value.beginFrame <= frameInt_ && _enume.Current.Value.endFrame >= frameInt_) {
            _enume.Dispose ();
            return _enume.Current.Key;
            }
        }
        _enume.Dispose ();
        return null;
    }

    public static string getFrameNameByFrameInt (MCTimeLineInfo _mcTimeLineInfoStruct, int frameInt_) {
        int _length = _mcTimeLineInfoStruct.frameLabels.Length;
        FrameLabel _frameLabel;
        for (int _idx = 0; _idx < _length; _idx++) {
        _frameLabel = _mcTimeLineInfoStruct.frameLabels[_idx];
        if (_frameLabel.frame == frameInt_) {
        if (_frameLabel.name == FrameNameType.frameName) {
        return _frameLabel.parameter;
                }
            }
        }
        return null;
    }
    public static string getFrameNameByFrameInt (string className_, int frameInt_) {
        if (!_frameIntToNameDict.ContainsKey (className_)) {
            return null;
        }
        string _frameName;
        if (_frameIntToNameDict[className_].TryGetValue (frameInt_, out _frameName)) {
            return _frameName;
        }
        return null;
    }

    public static int getFrameIntByFrameName (MCTimeLineInfo _mcTimeLineInfoStruct, string frameName_) {
        BeginEndFrame _beginEndFrame;
        if (_frameNameRangeDict[_mcTimeLineInfoStruct.className].TryGetValue (frameName_, out _beginEndFrame)) {
            return _beginEndFrame.beginFrame; //back the first frame
        } else { // -1 ,means there is on frame name
            return -1;
        }
    }

    public static int getFrameIntByFrameName (string className_, string frameName_) {
        if (!_frameNameToIntDict.ContainsKey (className_)) {
            return -1;
        }
        int _frameInt;
        if (_frameNameToIntDict[className_].TryGetValue (frameName_, out _frameInt)) {
            return _frameInt;
        }
        return -1;
    }
}