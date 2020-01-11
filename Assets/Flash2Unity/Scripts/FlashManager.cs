using System;
using System.Collections;
using System.Collections.Generic;
using flash.display;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReadOnlyAttribute : PropertyAttribute {

}

#if UNITY_EDITOR
[CustomPropertyDrawer (typeof (ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer {
    public override float GetPropertyHeight (SerializedProperty property, GUIContent label) {
        return EditorGUI.GetPropertyHeight (property, label, true);
    }

    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
        GUI.enabled = false;
        EditorGUI.PropertyField (position, property, label, true);
        GUI.enabled = true;
    }
}
#endif

public class FlashManager : MonoBehaviour {
    [HideInInspector]
    public static List<MovieClipContainer> beforeCreateList = new List<MovieClipContainer> ();

    //Unity Editor 
    [ReadOnly]
    public int movieClipCount = 0;
    [ReadOnly]
    public int spriteCount = 0;
    [ReadOnly]
    public int displayObjectCount = 0;

    private static FlashManager _instance = null;
    [HideInInspector]
    public bool initEnd = false;

    [ReadOnly]
    //MainMovieClip current on stage.
    public List<MovieClip> mainMovieClipList = new List<MovieClip> ();

    private Camera _camera;

    //[Flash2UnityCustomEditor]
    private int _frameRate = 30;
    //Frame per second.
    public int frameRate {
        get { return _frameRate; }
        set {
            if (_frameRate != value) {
                _frameRate = value;
                _updateIntervalTime = 1f / (float) _frameRate;
            }
        }
    }
    private float _updateIntervalTime;
    private float _currentTime = 0f;
    
    public delegate void FrameUpdateEventHander(object sender_);
    public event FrameUpdateEventHander frameUpdate;
    

    public static FlashManager getInstance () {
        return _instance;
    }
    public FlashManager () {
        if (_instance != null) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "instance is already created."
            );
        }
        _instance = this;
        _updateIntervalTime = 1f / frameRate;
    }

    private void OnDestroy () {
        for (int _idx = 0; _idx < mainMovieClipList.Count; _idx++) {
            MovieClip _v2 = mainMovieClipList[_idx];
            _v2.removeFromParent ();
        }
        mainMovieClipList.Clear();
        _instance = null;
    }
    private void afterInit () {
        initEnd = true;
        //Create MovieClipContariner's movieClip which call 'Start' earlier then this.
        if (beforeCreateList.Count > 0) {
            for (int _idx = 0; _idx < beforeCreateList.Count; _idx++) {
                MovieClipContainer _mcc = beforeCreateList[_idx];
                _mcc.createMovieClip ();
            }
            beforeCreateList.Clear ();
            beforeCreateList = null;
        }
    }

    public void Awake () {
        DontDestroyOnLoad(gameObject);
        
        // FlashUtils.spriteRendererShader = Shader.Find ("Custom/Overdraw");
        // GameObject _cameraGameObject = GameObject.Find ("Main Camera");
        // _camera = _cameraGameObject.GetComponent<Camera> ();
        // _camera.SetReplacementShader(FlashUtils.spriteRendererShader, "");

        /* 
         Folder path is must under 'Assets/Resources/'.
         Create a folder named 'flash' in  'Assets/Resources/'.
         Select it as export folder in flash IDE.
            Assets/Resources/<flash>/pngs/ -> png files
            Assets/Resources/<flash>/jsons/ -> json files
        */

        //1.Load json config which export from Flash.
        //  Use them to init MovieClip's timeline infos.
        FlashUtils.cacheMovieClipInFolder ("flash/jsons");

        //2.cache png files in altas which export from Flash.
        //  when Flash export png files.
        //  'Base/editor/FlashAssetProstprocessor.cs' will auto convert them in to spriteAltas.
        OtherUtils.cacheSpriteInFolder ("flash/pngs");
        OtherUtils.cacheSpriteFromSpriteAltasInFolder ("flash/pngs");

        //3.Create a gameObject contains caches create in runtime.
        Transform _trans = new GameObject ().transform;
        _trans.parent = transform;
        _trans.gameObject.name = "cacheContainer";
        FlashUtils.setCacheContainerTransform (_trans); //set to FlashUtils.

        //4.Do things after init.
        afterInit ();

    }
    private void Update () {
        _currentTime += Time.deltaTime;
        if (_currentTime <= _updateIntervalTime) {
            return;
        }
        _currentTime = _currentTime - _updateIntervalTime;
        if (frameUpdate != null) frameUpdate(this);
        int _length = mainMovieClipList.Count;
        for (int _idx = 0; _idx < _length; _idx++) {
            MovieClip _mainMovieClip = mainMovieClipList[_idx];
            _mainMovieClip.frameUpdate ();
        }
        
    }
}