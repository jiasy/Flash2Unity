using UnityEngine;
using flash.display;
public class Sample_DisBtn : MovieClip {
    private Vector3 screenPosition; //Convert object's world position to screen's;
    private Vector3 mousePositionOnScreen; //Get position clicked.
    private Vector3 mousePositionInWorld; //Convert clicked position to world position.
    private Transform _btnBgTransform = null;
    private Transform _poseDotTransform = null;
    private Bounds _btnBgBounds;
    private Bounds _poseDotBounds;

    public override void resetByTimeLineInfo (FlashUtils.MCTimeLineInfo mcTimeLineInfo_, MovieClip mainMOvieClip_) {
        base.resetByTimeLineInfo (mcTimeLineInfo_, mainMOvieClip_);
        for (int _idx = 0; _idx < mcTimeLineInfo.childrenInfos.Length; _idx++) {
            FlashUtils.ChildInfo _ci = mcTimeLineInfo.childrenInfos[_idx];
            if (_ci.insName == "btnBg") {
                _btnBgTransform = _disOnStage[_ci].selfTrans;
                _btnBgBounds = _btnBgTransform.gameObject.GetComponent<SpriteRenderer> ().sprite.bounds;
            }
            if (_ci.insName == "poseDot") {
                _poseDotTransform = _disOnStage[_ci].selfTrans;
                _poseDotBounds = _poseDotTransform.gameObject.GetComponent<SpriteRenderer> ().sprite.bounds;
            }
        }

        if (
            _btnBgTransform == null ||
            _poseDotTransform == null
        ) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "'btnBg' or 'poseDot' not put on stage..."
            );
        }
    }

    public void Update () {
        if (_btnBgTransform == null) {
            return;
        }
        screenPosition = Camera.main.WorldToScreenPoint (transform.position);
        if (Input.GetMouseButtonDown (0)) {
            mousePositionOnScreen = Input.mousePosition;
            mousePositionOnScreen.z = screenPosition.z;
            mousePositionInWorld = Camera.main.ScreenToWorldPoint (mousePositionOnScreen);
            if (
                mousePositionInWorld.x < _btnBgTransform.position.x + _btnBgTransform.localScale.x * _btnBgBounds.max.x &&
                mousePositionInWorld.y < _btnBgTransform.position.y + _btnBgTransform.localScale.y * _btnBgBounds.max.y &&
                mousePositionInWorld.x > _btnBgTransform.position.x + _btnBgTransform.localScale.x * _btnBgBounds.min.x &&
                mousePositionInWorld.y > _btnBgTransform.position.y + _btnBgTransform.localScale.y * _btnBgBounds.min.y
            ) {
                _poseDotTransform.position = mousePositionInWorld;
                _poseDotTransform.localScale = new Vector3 (1, 1, 1);
                _poseDotTransform.localRotation = Quaternion.Euler (new Vector3 (0f, 0f, 0f));
                //Control parent to play.
                transform.parent.gameObject.GetComponent<MovieClip>().play();
            } else {
                Vector3 _disV3 = (mousePositionInWorld - _btnBgTransform.position);
                _poseDotTransform.position = new Vector3 (
                    _btnBgTransform.position.x + _disV3.x * 0.5f,
                    _btnBgTransform.position.y + _disV3.y * 0.5f,
                    _btnBgTransform.position.z
                );
                _poseDotTransform.localRotation = Quaternion.Euler (new Vector3 (0f, 0f,
                    Mathf.Atan2 (_disV3.y, _disV3.x) * 57.295779513082321f
                ));
                float _length = new Vector2 (_disV3.x, _disV3.y).magnitude;
                _poseDotTransform.localScale = new Vector3 (
                    _length / (_poseDotBounds.max.x - _poseDotBounds.min.x),
                    1f,
                    1f
                );
            }
        }
    }
}