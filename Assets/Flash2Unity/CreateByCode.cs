using System.Collections;
using System.Collections.Generic;
using flash.display;
using UnityEngine;

public class CreateByCode : MonoBehaviour{
    
    void Start(){
        //Create instances of MovieClip by code.
        FlashUtils.getMovieClipByClassNameAndAddTo ("Game_ShootGameDis", transform).selfTrans.position = new Vector3 (0f, -2f, 0f);

        FlashUtils.getMovieClipByClassNameAndAddTo ("Animation_PerformanceTest", transform).selfTrans.position = new Vector3 (0f, -3.22f, 0f);
        MovieClip _upMovieClip = FlashUtils.getMovieClipByClassNameAndAddTo ("Animation_PerformanceTest", transform);
        _upMovieClip.selfTrans.position = new Vector3 (0f, 3.22f, 0f);
        _upMovieClip.selfTrans.rotation = Quaternion.Euler (new Vector3 (0f, 0f, 180f));
        _upMovieClip.gotoAndStop(_upMovieClip.mcTimeLineInfo.totalFrames);//All frog add to stage at same frame.Then stop as last frame.

        FlashUtils.getMovieClipByClassNameAndAddTo ("Trail_DisTrailBackAndForthGroupCircle", transform).selfTrans.position = new Vector3 (4f, 2.5f, 0f);
        FlashUtils.getMovieClipByClassNameAndAddTo ("Trail_DisTrailLineRotate", transform).selfTrans.position = new Vector3 (-4f, 2.5f, 0f);
    }
}
