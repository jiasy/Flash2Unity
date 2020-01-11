using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace flash.display {
    [RequireComponent (typeof (SpriteRenderer))]
    public class Sprite : DisplayObject {
        public SpriteRenderer spriteRenderer;
        [ReadOnly]
        public float currentR=1f;
        [ReadOnly]
        public float currentG=1f;
        [ReadOnly]
        public float currentB=1f;

        public override void Awake () {
            base.Awake ();
            type = FlashUtils.ClassType.Sprite;
            FlashManager.getInstance ().spriteCount++;
        }

        public override void OnDestroy () {
            spriteRenderer.sprite = null;
            FlashManager _flashManager = FlashManager.getInstance ();
            if(_flashManager!=null)_flashManager.spriteCount--;
            base.OnDestroy ();
        }

        public override void initByChildInfo (FlashUtils.ChildInfo childInfo_, MovieClip parentMovieClip_) {
            base.initByChildInfo (childInfo_, parentMovieClip_);
            resetSprite(true);
            spriteRenderer.sortingOrder = childInfo.childIndex; //Sort order in current container.
        }

        public override void resetSprite(bool isFromAltas_){
            spriteRenderer.sprite = OtherUtils.getSpriteByName (childInfo.className,isFromAltas_); //Show that pic
        }

        public override void frameUpdate () {
            updateRGB (parentMovieClip.currentFrame);
            updateAlpha (parentMovieClip.currentFrame, parentMovieClip.currentAlpha);
            frameUpdateEnd ();
        }

        public void updateRGB (int parentCurrentFrame_) {
            int _frameIdx = parentCurrentFrame_ - _beginFrame;
            if (_backingToStage || rgbChanged) {
                //first change RGB,Then change alpha.So 1f is OK here.
                currentR = propertyInFrames[6 /*(int) PropertyType.r*/ , _frameIdx];
                currentG = propertyInFrames[7 /*(int) PropertyType.g*/ , _frameIdx];
                currentB = propertyInFrames[8 /*(int) PropertyType.b*/ , _frameIdx];
            }
        }
        public override void updateAlpha (int parentCurrentFrame_, float parentsAlpha_) {
            int _frameIdx = parentCurrentFrame_ - _beginFrame;
            if (_backingToStage || aChanged ||rgbChanged|| parentsAlpha_<0.99f) {
                spriteRenderer.color = new Color (currentR, currentG, currentB,
                    propertyInFrames[5 /*(int) PropertyType.a*/ , _frameIdx] * parentsAlpha_
                );
            }
        }
    }
}