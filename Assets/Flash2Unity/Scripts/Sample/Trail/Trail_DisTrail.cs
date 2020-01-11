using flash.display;
using UnityEngine;
public class Trail_DisTrail : MovieClip {
    public Transform redDotTransform = null;
    public TrailRenderer trailRenderer;

    public override void resetByTimeLineInfo (FlashUtils.MCTimeLineInfo mcTimeLineInfo_, MovieClip mainMOvieClip_) {
        base.resetByTimeLineInfo (mcTimeLineInfo_, mainMOvieClip_);
        for (int _idx = 0; _idx < mcTimeLineInfo.childrenInfos.Length; _idx++) {
            FlashUtils.ChildInfo _ci = mcTimeLineInfo.childrenInfos[_idx];
            if (_ci.insName == "redDot") {
                redDotTransform = _disOnStage[_ci].selfTrans;
            }
        }

        if (redDotTransform == null) {
            Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                "'redDot' not put on stage..."
            );
        }
        redDotTransform.gameObject.GetComponent<SpriteRenderer> ().sprite = null;

        Shader _shader = Shader.Find ("Sprites/Default");
        Material _material = new Material (_shader);
        trailRenderer = redDotTransform.gameObject.AddComponent<TrailRenderer> ();
        trailRenderer.material = _material;
        trailRenderer.textureMode = LineTextureMode.RepeatPerSegment;
        trailRenderer.startColor = Color.red;
        trailRenderer.endColor = Color.yellow;
        trailRenderer.startWidth = 0.05f;
        trailRenderer.endWidth = 0.01f;
        trailRenderer.time = 1f;

    }

    public override void frameUpdate () {
        base.frameUpdate ();
        if (redDotTransform == null) {
            return;
        }

        if (currentFrame == mcTimeLineInfo.totalFrames - 1) {
            trailRenderer.Clear ();
        }
    }
}