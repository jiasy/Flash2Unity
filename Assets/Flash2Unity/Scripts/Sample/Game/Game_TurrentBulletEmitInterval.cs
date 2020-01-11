using System;
using flash.display;
using UnityEngine;
public class Game_TurrentBulletEmitInterval: MovieClip {
    public Transform redDotTransform = null;

    public override void resetByTimeLineInfo (FlashUtils.MCTimeLineInfo mcTimeLineInfo_, MovieClip mainMOvieClip_) {
        base.resetByTimeLineInfo (mcTimeLineInfo_, mainMOvieClip_);

        redDotTransform = getChildByName("redDot").selfTrans;
        
        if (redDotTransform == null) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "'redDot' not put on stage..."
            );
        }

        if (Math.Abs (redDotTransform.localPosition.x) < 0.0001f || Math.Abs (redDotTransform.localPosition.y) < 0.0001f) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "redDot must has a distance from original point"
            );
        }

        //redDotTransform.gameObject.GetComponent<SpriteRenderer> ().sprite = null;
    }

    public override void frameUpdate () {
        base.frameUpdate ();
        if (redDotTransform == null ) {
            return;
        }
        var _gameIns = Game_ShootGameDis.getInstance ();
        if (_gameIns.target == null ) {
            return;
        }
        if (currentFrame == 1) {
            MovieClip _bullet = FlashUtils.getMovieClipByClassNameAndAddTo ("Game_Bullet", _gameIns.bulletContainerTrans);
            (_bullet as Game_Bullet).target = _gameIns.target;
            _bullet.selfTrans.position = transform.position;
            _bullet.selfTrans.forward = (redDotTransform.position - transform.position); //Forward from <redDotTransform in world> - <self transform in world>.
        }
    }
}