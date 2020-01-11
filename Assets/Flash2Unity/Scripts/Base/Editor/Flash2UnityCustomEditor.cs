using flash.display;
using UnityEditor;

// Open comment,can control frameRate at runtime
[CustomEditor(typeof(FlashManager))]
public class FlashManagerCustomEditor : Editor {

    public override void OnInspectorGUI(){
        FlashManager _flashManager = target as FlashManager;
        int _frameRate = EditorGUILayout.IntField("frameRate", _flashManager.frameRate);
        if (_flashManager.frameRate != _frameRate){
            _flashManager.frameRate = _frameRate;
        }
        base.DrawDefaultInspector();
    }
}

[CustomEditor(typeof(MovieClip))]
public class MovieClipCustomEditor : Editor {
    public override void OnInspectorGUI(){
        MovieClip _movieClip = target as MovieClip;
        bool _spriteFromAltas = EditorGUILayout.Toggle("spriteFromAltas", _movieClip.spriteFromAltas);
        if (_movieClip.spriteFromAltas != _spriteFromAltas){
            _movieClip.spriteFromAltas = _spriteFromAltas;
        }
        base.DrawDefaultInspector();
    }
}