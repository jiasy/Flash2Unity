using flash.display;
using UnityEngine;
using UnityEngine.Rendering;

public class Game_ShootGameDis: MovieClip {
    public Transform turrentTrans;
    public MovieClip target;
    public MovieClip turrentMovieClip;
    public Transform bulletContainerTrans;
    private static Game_ShootGameDis _ins = null;

    public bool isHit = false;

    public int targetStateChangeInterval = 0;

    static public Game_ShootGameDis getInstance () {
        if (_ins == null) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "instance create in resectByMCStruct..."
            );
        }
        return _ins;
    }
    public override void Awake () {
        base.Awake ();
        _ins = this;
    }
    public void createBulletContainer(){
        bulletContainerTrans = new GameObject ().transform;
        bulletContainerTrans.gameObject.name = "BulletContainer";
        bulletContainerTrans.parent = selfTrans;
        bulletContainerTrans.gameObject.AddComponent<SortingGroup>().sortingOrder = mcTimeLineInfo.childrenInfos.Length+1;
    }
    public override void resetByTimeLineInfo (FlashUtils.MCTimeLineInfo mcTimeLineInfo_, MovieClip mainMOvieClip_) {
        base.resetByTimeLineInfo (mcTimeLineInfo_, mainMOvieClip_);
        createBulletContainer();

        turrentMovieClip = getChildByName("turrent") as MovieClip;
        turrentTrans = turrentMovieClip.selfTrans;

        target = getChildByName("target") as MovieClip;

        if (turrentTrans == null || target == null) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "'turrent' or 'target' not put on stage..."
            );
        }

    }

    public void Update () {
        if (turrentTrans == null) {
            return;
        }

        if (Input.GetMouseButtonDown (0)) {
            //alternate turrent state.
            string _currentType = turrentMovieClip.currentLocalFrameName();
            if (_currentType == "type1") {
                turrentMovieClip.gotoAndPlay ("type2");
            } else if (_currentType == "type2") {
                turrentMovieClip.gotoAndPlay ("type1");
            }
        }

        if (targetStateChangeInterval < 5) {
            targetStateChangeInterval++;
            return;
        } else {
            targetStateChangeInterval = 0;
        }

        if (isHit) {
            isHit = false;
            if (target.currentFrame < target.mcTimeLineInfo.totalFrames) {
                target.gotoAndStop (target.currentFrame + 1);
            } else {
                target.gotoAndStop (1);
            }
        }
    }
}