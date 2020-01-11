using UnityEngine;

namespace flash.display {
    public class DisplayObject : MonoBehaviour {
        [ReadOnly]
        protected MovieClip _mainMovieClip; //cache parent MovieClip.
        [HideInInspector]
        public Transform selfTrans;
        [ReadOnly]
        public bool xyChanged;
        [ReadOnly]
        public bool sxyChanged;
        [ReadOnly]
        public bool roChanged;
        [ReadOnly]
        public bool rgbChanged;
        [ReadOnly]
        public bool aChanged;
        public FlashUtils.ChildInfo childInfo; //This is null in MainNode.
        [ReadOnly]
        public MovieClip parentMovieClip; //This is null in MainNode.
        [HideInInspector]
        public FlashUtils.ClassType type;
        protected bool _backingToStage;
        [HideInInspector]
        public float[, ] propertyInFrames;
        [ReadOnly]
        public float currentAlpha = 1f;

        protected int _beginFrame;

        public virtual void OnDestroy () {
            selfTrans = null;
            parentMovieClip = null;
            childInfo = null;
            propertyInFrames = null;
            FlashManager _flashManager = FlashManager.getInstance ();
            if (_flashManager != null) _flashManager.displayObjectCount--;
        }

        public virtual void Awake () {
            selfTrans = transform;
            FlashManager.getInstance ().displayObjectCount++;
        }

        public virtual void initByChildInfo (FlashUtils.ChildInfo childInfo_, MovieClip parentMovieClip_) {
            childInfo = childInfo_;
            propertyInFrames = childInfo_.propertyInFrames;
            gameObject.name = childInfo_.insName;
            _beginFrame = childInfo_.beginFrame;
            xyChanged = childInfo_.xyChanged;
            sxyChanged = childInfo_.sxyChanged;
            roChanged = childInfo_.roChanged;
            rgbChanged = childInfo_.rgbChanged;
            aChanged = childInfo_.aChanged;
            parentMovieClip = parentMovieClip_;
        }

        public void setMainMovieClip (MovieClip mainMovieClip_) {
            _mainMovieClip = mainMovieClip_;
        }
        public void syncPropertys (int parentCurrentFrame_) {
            int _frameIdx = parentCurrentFrame_ - _beginFrame;
            if (_backingToStage || xyChanged) {
                selfTrans.localPosition = new Vector3 (
                    propertyInFrames[0 /*(int) PropertyType.x*/ , _frameIdx],
                    propertyInFrames[1 /*(int) PropertyType.y*/ , _frameIdx]
                );
            }

            if (_backingToStage || sxyChanged) {
                selfTrans.localScale = new Vector3 (
                    propertyInFrames[2 /*(int) PropertyType.scaleX*/ , _frameIdx],
                    propertyInFrames[3 /*(int) PropertyType.scaleY*/ , _frameIdx]
                );
            }

            if (_backingToStage || roChanged) {
                selfTrans.localRotation = Quaternion.Euler (new Vector3 (0f, 0f,
                    propertyInFrames[4 /*(int) PropertyType.rotation*/ , _frameIdx]
                ));
            }
        }

        public virtual void frameUpdate () {
            throw new System.NotImplementedException ();
        }

        public void frameUpdateEnd () {
            _backingToStage = false;
            currentAlpha = 1f;
        }

        public float getCurrentFrameAlpha (int parentCurrentFrame_) {
            if (aChanged) {
                int _frameIdx = parentCurrentFrame_ - _beginFrame;
                return propertyInFrames[5 /*(int) PropertyType.alpha*/ , _frameIdx];
            } else {
                return 1f;
            }
        }

        public virtual void updateAlpha (int parentCurrentFrame_, float a_) {
            throw new System.NotImplementedException ();
        }

        public virtual void resetSprite (bool isFromAltas_) {
            throw new System.NotImplementedException ();
        }

        public virtual void resetParsWhenOnOrOffStage (bool backingToStage_) {
            _backingToStage = backingToStage_;
        }

        public void changeActive (bool state_) {
            if (gameObject.activeSelf != state_) {
                gameObject.SetActive (state_);
            }
        }
    }
}