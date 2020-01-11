using System.Collections.Generic;
using flash.display;
using UnityEngine;

public class Performance : MonoBehaviour
{
    /*
    When running in Unity editor.Don't select this gameObject.
        When current MovieClip on stage number is large.It will be slow when MovieClip show on stage while you selecting this.
     */
    [SerializeField] public int animationNumber = 500;
    [SerializeField] public string movieClipClassName = "Animation_Frog";
    [SerializeField] public bool createOneByOne;
    private int _currentCreatCount = 0;
    [SerializeField] public bool changeHeadSkin;
    [SerializeField] public float alpha = 1f;

    private float _beginX = -4f;
    private float _width = 8f;

    private List<MovieClip> _mcList = new List<MovieClip>();
    private Transform _hideContainer;

    void Start()
    {
        _hideContainer = new GameObject().transform;
        _hideContainer.parent = transform;
        _hideContainer.gameObject.SetActive(false);
        _hideContainer.gameObject.name = "hideContainer";
        
        if (changeHeadSkin && !string.Equals(movieClipClassName, "Animation_Frog"))
        {
            Debug.LogError("ERROR " + System.Reflection.MethodBase.GetCurrentMethod().ReflectedType.FullName + " -> " +
                           new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name + " : " +
                           "changeHeadSkin only for 'Animation_Frog' in sample."
            );
        }

        float _interval = _width / animationNumber;
        for (int _idx = 0; _idx < animationNumber; _idx++)
        {
            MovieClip _mainMovieClip = FlashUtils.getMovieClipByClassNameAndAddTo(movieClipClassName, transform);
            _mainMovieClip.selfTrans.localPosition = new Vector3(_idx * _interval + _beginX, 0f, 0f);
            _mainMovieClip.currentAlpha = alpha;
            _mainMovieClip.sortingGroup.sortingOrder = _idx;
            _mcList.Add(_mainMovieClip);
        }

        if (createOneByOne)
        {
            FlashManager.getInstance().frameUpdate += frameUpdate;
            for (int _idx = 0; _idx < _mcList.Count; _idx++)
            {
                _mcList[_idx].stop();
                _mcList[_idx].selfTrans.parent = _hideContainer;
            }
        }
    }

    private void OnDestroy()
    {
        if (createOneByOne && FlashManager.getInstance() != null) FlashManager.getInstance().frameUpdate -= frameUpdate;
    }


    public void frameUpdate(object sender_)
    {
        if (!createOneByOne)
        {
            return;
        }

        if (_currentCreatCount < animationNumber)
        {
            MovieClip _mainMovieClip = _mcList[_currentCreatCount];
            if (changeHeadSkin)
            {
                _mainMovieClip.getMovieClipByPath("head").gotoAndStop(_currentCreatCount % 3 + 1);
            }

            _mainMovieClip.play();
            _mainMovieClip.selfTrans.parent = transform;
            _currentCreatCount++;
        }
    }
}