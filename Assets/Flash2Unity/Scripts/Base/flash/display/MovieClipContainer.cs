using UnityEngine;

namespace flash.display {
    public class MovieClipContainer : MonoBehaviour {
        private MovieClip _mainMovieClip;
        [Tooltip("MovieClip class name you want create.")]
        public string className;

        private void Start () {
            if (string.IsNullOrEmpty (className)) {
                transform.parent = null;
                Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
                    "className IsNullOrWhiteSpace!"
                );
            } else {
                if (FlashManager.getInstance () == null || !FlashManager.getInstance ().initEnd) {
                    FlashManager.beforeCreateList.Add (this);
                } else {
                    createMovieClip ();
                }
            }
        }
        public void createMovieClip () { //Create MovieClip When FlashManager._ins created.
            _mainMovieClip = FlashUtils.getMovieClipByClassNameAndAddTo (className, gameObject);
            _mainMovieClip.selfTrans.localPosition = new Vector3 (0f, 0f, 0f);
        }
    }
}