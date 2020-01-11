using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace flash.display {
	[RequireComponent (typeof (SortingGroup))]

	public class MovieClip : DisplayObject {

		public class CodeControlFrameAction : IDisposable {
			public FlashUtils.FrameNameType frameActionType;
			public int targetFrame;

			public CodeControlFrameAction (FlashUtils.FrameNameType frameActionType_) {
				frameActionType = frameActionType_;
			}

			public void Dispose () { }
		}
		//MovieClip and SpriteRenderer caches
		protected Dictionary<FlashUtils.ChildInfo, DisplayObject> _disOnStage = new Dictionary<FlashUtils.ChildInfo, DisplayObject> ();

		private Dictionary<FlashUtils.ChildInfo, DisplayObject> _disNotOnStage = new Dictionary<FlashUtils.ChildInfo, DisplayObject> ();

		//Normally NodeBase's display path is fixed.Won't change in runtime.So we can cache it when find;
		private Dictionary<string, MovieClip> _movieClipOnPathCache = new Dictionary<string, MovieClip> ();

		private Dictionary<int, List<FlashUtils.FrameLabel>> _frameIntToFrameActionCache;

		//MovieClip's MovieClip infos
		public FlashUtils.MCTimeLineInfo mcTimeLineInfo;

		[ReadOnly]
		public String movieClipClassName;

		//Frame control or special action use
		public int currentFrame;
		public bool autoPlay;
		private int _lastFrame;

		public Transform notOnStageContainer; //Only on MainMovieClip.

		private CodeControlFrameAction _codeControlFrameAction; //Other MovieClip do frame control.

		[HideInInspector]
		public FlashUtils.FrameLabel frameLabelAtNextFrameBegin; //excute FrameLabel when next update.

		private bool _isMainMovieClip; //as a MainMovieClip

		private bool _childrenNeverRemoveFromStage;
		private bool _allChildrenSprite;

		//[Flash2UnityCustomEditor]
		private bool _spriteFromAltas = true;
		//Set to false,Then you can copy movieClip at run time.Paste in editor in edit mode.
		public bool spriteFromAltas {
			get { return _spriteFromAltas; }
			set {
				if (_spriteFromAltas != value) {
					_spriteFromAltas = value;
					resetSprite (_spriteFromAltas);
				}
			}
		}

		public SortingGroup sortingGroup;

		public override void resetSprite (bool isFromAltas_) {
			var _enume = _disOnStage.GetEnumerator ();
			while (_enume.MoveNext ()) {
				_enume.Current.Value.resetSprite (isFromAltas_);
			}
			_enume.Dispose ();
			_enume = _disNotOnStage.GetEnumerator ();
			while (_enume.MoveNext ()) {
				_enume.Current.Value.resetSprite (isFromAltas_);
			}
			_enume.Dispose ();
		}

		public override void Awake () {
			base.Awake ();
			sortingGroup = GetComponent<SortingGroup> ();
			type = FlashUtils.ClassType.MovieClip;
			FlashManager.getInstance ().movieClipCount++;
		}

		public void removeFromParent () {
			if (_isMainMovieClip) {
				Destroy (gameObject);
			} else {
				Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " + new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
					"remove only excute on mainMovieClip."
				);
			}
		}

		public override void OnDestroy () {
			FlashManager _flashManager = FlashManager.getInstance ();
			if (_flashManager != null) _flashManager.mainMovieClipList.Remove (this);
			if (_flashManager != null) _flashManager.movieClipCount--;
			_disOnStage.Clear ();
			_disNotOnStage.Clear ();
			_movieClipOnPathCache.Clear ();
			_codeControlFrameAction = null;
			frameLabelAtNextFrameBegin = null;
			_mainMovieClip = null;
			mcTimeLineInfo = null;
			_frameIntToFrameActionCache = null;
			base.OnDestroy ();
		}

		//Create as a MainNode or not.
		public virtual void resetByTimeLineInfo (FlashUtils.MCTimeLineInfo mcTimeLineInfo_, MovieClip mainMovieClip_) {
			mcTimeLineInfo = mcTimeLineInfo_;
			movieClipClassName = mcTimeLineInfo.className;
			_childrenNeverRemoveFromStage = mcTimeLineInfo.childrenNeverRemoveFromStage;
			_allChildrenSprite = mcTimeLineInfo.allChildrenSprite;
			_frameIntToFrameActionCache = FlashUtils.getFrameIntToFrameActionCache (mcTimeLineInfo);
			_codeControlFrameAction = new CodeControlFrameAction (FlashUtils.FrameNameType.none);

			if (mainMovieClip_ == null) {
				_isMainMovieClip = true;
				gameObject.name = mcTimeLineInfo.className;
				spriteFromAltas = true;
				setMainMovieClip (this);
				FlashManager.getInstance ().mainMovieClipList.Add (this);
			} else {
				_isMainMovieClip = false;
				setMainMovieClip (mainMovieClip_);
			}

			if (_isMainMovieClip) {
				resetParsWhenOnOrOffStage (true);
			}

			if (_childrenNeverRemoveFromStage) { //Add all child to stage.
				putAllChildOnStage (); //Because there is no remove.
			}

			frameUpdate (); //Must do once frameUpdate.To move to frame 1 right now.
		}

		public override void initByChildInfo (FlashUtils.ChildInfo childInfo_, MovieClip parentMovieClip_) {
			base.initByChildInfo (childInfo_, parentMovieClip_);
			gameObject.GetComponent<SortingGroup> ().sortingOrder = childInfo.childIndex;
		}

		public override void frameUpdate () {
			updateCurrentFrame ();
			updateChildrenOnStage ();
			runActions ();
			enterFrame ();
			frameUpdateEnd (); //Notice there is call when all done.
		}

		public void updateCurrentFrame () {
			if (FlashUtils.FrameNameType.none != _codeControlFrameAction.frameActionType) {
				//Control by code.
				if (FlashUtils.FrameNameType.gotoAndPlay == _codeControlFrameAction.frameActionType) {
					gotoAndPlayAction (_codeControlFrameAction.targetFrame);
				} else if (FlashUtils.FrameNameType.gotoAndStop == _codeControlFrameAction.frameActionType) {
					gotoAndStopAction (_codeControlFrameAction.targetFrame);
				} else if (FlashUtils.FrameNameType.prevFrame == _codeControlFrameAction.frameActionType) {
					prevFrameAction ();
				} else if (FlashUtils.FrameNameType.nextFrame == _codeControlFrameAction.frameActionType) {
					nextFrameAction ();
				} else if (FlashUtils.FrameNameType.remove == _codeControlFrameAction.frameActionType) {
					removeFromParent ();
				}
				_codeControlFrameAction.frameActionType = FlashUtils.FrameNameType.none;
				frameLabelAtNextFrameBegin = null; //Higher priority,clear lower one.Avoid do it in next frame.
			} else if (frameLabelAtNextFrameBegin != null) {
				//Control by other movieclip frame.Then self cann't do auto play things.
				doFrameActionByFrameLabel (frameLabelAtNextFrameBegin); //excute actions
				frameLabelAtNextFrameBegin = null; //clear it
			} else {
				if (autoPlay) {
					if (currentFrame < mcTimeLineInfo.totalFrames) {
						currentFrame = currentFrame + 1;
					} else {
						currentFrame = 1;
					}
				}
			}

			//When init,must make it to frame 1.
			if (currentFrame == 0) {
				currentFrame = 1;
			}
		}

		public void updateChildrenOnStage () {
			// if (_childrenNeverRemoveFromStage) {
			// 	return;
			// }
			FlashUtils.ChildInfo[] _childrenInfos = mcTimeLineInfo.childrenInfos;
			var _length = _childrenInfos.Length;
			//Declear things for loop.
			FlashUtils.ChildInfo _childInfo;
			DisplayObject _displayObject;
			MovieClip _movieClip;
			Sprite _sprite;
			for (int _idx = 0; _idx < _length; _idx++) {
				_childInfo = _childrenInfos[_idx];
				if (_childInfo.beginFrame <= currentFrame && _childInfo.endFrame >= currentFrame) {
					//In frame range.
					if (!_disOnStage.ContainsKey (_childInfo)) {
						//Not on stage.
						if (_disNotOnStage.TryGetValue (_childInfo, out _displayObject)) {
							//Get from cache.
							_disNotOnStage.Remove (_childInfo);
						} else {
							//No cache
							if (_childInfo.superClassName == 2 /*(int) FlashUtils.ClassType.MovieClip */ ) {
								_movieClip = FlashUtils.getMovieClipByChildInfoAndAddTo (_childInfo, _mainMovieClip, this);
								_displayObject = _movieClip;
							} else if (_childInfo.superClassName == 3 /*(int) FlashUtils.ClassType.Sprite */ ) {
								//SpriteRenderer
								_sprite = FlashUtils.getPicNodeAndAddTo (_mainMovieClip, selfTrans);
								_sprite.initByChildInfo (_childInfo, this);
								_displayObject = _sprite;
							}
						}
						//Cache it
						_displayObject.resetParsWhenOnOrOffStage (true); //Reset parameters.Use it as new one.
						_disOnStage[_childInfo] = _displayObject; //cache it as on stage
						_displayObject.selfTrans.parent = selfTrans;
						_displayObject.selfTrans.SetSiblingIndex (_childInfo.childIndex);
					}
				} else {
					//Not on stage.
					if (_disOnStage.TryGetValue (_childInfo, out _displayObject)) {
						_disOnStage.Remove (_childInfo); //remove from stage
						_displayObject.resetParsWhenOnOrOffStage (false); //Reset parameters.Use it as new one.
						_disNotOnStage[_childInfo] = _displayObject; //cache it as hide from stage.
						_mainMovieClip.cacheNotOnStageDisplayObject (_displayObject);
					}
				}
			}
		}

		public void runActions () {
			if (currentFrame == _lastFrame) {
				return;
			}
			doFrameActionByFrameInt (currentFrame);
		}

		public void enterFrame () {
			if (currentFrame == _lastFrame && !_allChildrenSprite) {
				//Update when children have MovieClip
				updateDisplayList (false);
				return;
			}

			updateDisplayList (true);
			_lastFrame = currentFrame;
		}

		public void updateDisplayList (bool frameChanged_) {
			DisplayObject _displayObject;
			var _enume = _disOnStage.GetEnumerator ();
			while (_enume.MoveNext ()) {
				_displayObject = _enume.Current.Value;
				if (frameChanged_) {
				_displayObject.syncPropertys (currentFrame);
				}
				_displayObject.currentAlpha = currentAlpha * _displayObject.getCurrentFrameAlpha (currentFrame);
				_displayObject.frameUpdate (); //recursive MovieClip
			}
			_enume.Dispose ();
		}

		//Frame control call by code.
		public void nextFrame () {
			_codeControlFrameAction.frameActionType = FlashUtils.FrameNameType.nextFrame;
		}

		public void prevFrame () {
			_codeControlFrameAction.frameActionType = FlashUtils.FrameNameType.prevFrame;
		}

		public void gotoAndStop (string frameName_) {
			int _targetFrameInt = FlashUtils.getFrameIntByFrameName (mcTimeLineInfo, frameName_);
			gotoAndStop (_targetFrameInt);
		}

		public void gotoAndStop (int frameInt_) {
			_codeControlFrameAction.frameActionType = FlashUtils.FrameNameType.gotoAndStop;
			_codeControlFrameAction.targetFrame = frameInt_;
		}

		public void gotoAndPlay (string frameName_) {
			int _targetFrameInt = FlashUtils.getFrameIntByFrameName (mcTimeLineInfo, frameName_);
			gotoAndPlay (_targetFrameInt);
		}

		public void gotoAndPlay (int frameInt_) {
			_codeControlFrameAction.frameActionType = FlashUtils.FrameNameType.gotoAndPlay;
			_codeControlFrameAction.targetFrame = frameInt_;
		}

		//Frame control editor by flash ide
		private void nextFrameAction () {
			if (currentFrame < mcTimeLineInfo.totalFrames) {
				gotoAndStopAction (currentFrame + 1);
			}
		}

		private void prevFrameAction () {
			if (currentFrame > 1) {
				gotoAndStopAction (currentFrame - 1);
			}
		}

		private void gotoAndStopAction (int frameInt_) {
			autoPlay = false;
			gotoFrame (frameInt_);
		}

		private void gotoAndPlayAction (int frameInt_) {
			autoPlay = true;
			gotoFrame (frameInt_);
		}

		private void gotoAndStopAction (string frameName_) {
			int _targetFrameInt = FlashUtils.getFrameIntByFrameName (mcTimeLineInfo, frameName_);
			autoPlay = false;
			gotoFrame (_targetFrameInt);
		}

		private void gotoAndPlayAction (string frameName_) {
			int _targetFrameInt = FlashUtils.getFrameIntByFrameName (mcTimeLineInfo, frameName_);
			autoPlay = true;
			gotoFrame (_targetFrameInt);
		}

		//Code and Flash IDE both can call.
		public void play () {
			autoPlay = true;
		}

		public void stop () {
			autoPlay = false;
		}

		//Real frame change
		private void gotoFrame (int frameInt_) {
			//frame limition
			if (frameInt_ < 1 || frameInt_ > mcTimeLineInfo.totalFrames) {
				Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " +
					new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
					mcTimeLineInfo.className + " : " + frameInt_ + " not in [1," +
					mcTimeLineInfo.totalFrames.ToString () + "]!"
				);
				return;
			}

			//Check whether frame changed.
			if (currentFrame != frameInt_) {
				currentFrame = frameInt_;
				doFrameActionByFrameInt (frameInt_);
			}
		}

		//Call when add or remove from stage.
		public override void resetParsWhenOnOrOffStage (bool backingToStage_) {
			base.resetParsWhenOnOrOffStage (backingToStage_);
			//Later in enterFrame()'s updateDisplayList() will call _disNode's frameUpdate().'currentFrame' will be 1 as that time.
			currentFrame = 0; //Set to 0,means no one on its stage.
			updateChildrenOnStage (); //Recursive all children's resetParsWhenOnOrOffStage().
			_lastFrame = -1;
			autoPlay = true;
		}

		//Frame label -> actions
		public void doFrameActionByFrameInt (int frameInt_) {
			List<FlashUtils.FrameLabel> _frameActions;
			if (_frameIntToFrameActionCache.TryGetValue (frameInt_, out _frameActions)) {
				//Show this frame,then do actions on this frame.
				//Frame control actions which target is self,Will do in next frame.
				FlashUtils.FrameLabel _frameLabel;
				int _selfFrameControl = 0;
				MovieClip targetMovieClip;
				string _path;
				for (int _idx = 0; _idx < _frameActions.Count; _idx++) {
					_frameLabel = _frameActions[_idx];
					_path = _frameLabel.targetPath;
					if (_frameLabel.name == FlashUtils.FrameNameType.remove) {
						if (_frameLabel.targetPath == "t") {
							removeFromParent (); //Self remove is call right now.
						} else {
							targetMovieClip = getMovieClipByPath (_path);
							targetMovieClip.frameLabelAtNextFrameBegin = _frameLabel;
						}
					} else if (
						_frameLabel.name == FlashUtils.FrameNameType.stop ||
						_frameLabel.name == FlashUtils.FrameNameType.play
					) {
						if (_frameLabel.targetPath == "t") {
							_selfFrameControl++;
							targetMovieClip = this;
						} else {
							targetMovieClip = getMovieClipByPath (_path);
						}
						if (_frameLabel.name == FlashUtils.FrameNameType.stop) targetMovieClip.stop ();
						if (_frameLabel.name == FlashUtils.FrameNameType.play) targetMovieClip.play ();
					} else if (
						_frameLabel.name == FlashUtils.FrameNameType.nextFrame ||
						_frameLabel.name == FlashUtils.FrameNameType.prevFrame ||
						_frameLabel.name == FlashUtils.FrameNameType.gotoAndStop ||
						_frameLabel.name == FlashUtils.FrameNameType.gotoAndPlay
					) {
						if (_frameLabel.targetPath == "t") {
							_selfFrameControl++;
							if (frameLabelAtNextFrameBegin == null) {
								//Other MovieClip frame control has higher priority
								frameLabelAtNextFrameBegin = _frameLabel;
							}
						} else {
							targetMovieClip = getMovieClipByPath (_path);
							targetMovieClip.frameLabelAtNextFrameBegin = _frameLabel;
						}
					}
				}

				if (_selfFrameControl > 1) {
					Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " +
						new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
						"There are more then two frame control actions in one frame." + mcTimeLineInfo.className + " : " +
						frameInt_.ToString () + " !"
					);
				}
			}
		}

		//MovieClip control frame.
		public void doFrameActionByFrameLabel (FlashUtils.FrameLabel frameLabel_) {
			if (frameLabel_.name == FlashUtils.FrameNameType.nextFrame) {
				nextFrameAction ();
			} else if (frameLabel_.name == FlashUtils.FrameNameType.prevFrame) {
				prevFrameAction ();
			} else if (frameLabel_.name == FlashUtils.FrameNameType.gotoAndStop) {
				gotoAndStopAction (FlashUtils.getFrameIntByString (mcTimeLineInfo, frameLabel_.parameter));
			} else if (frameLabel_.name == FlashUtils.FrameNameType.gotoAndPlay) {
				gotoAndPlayAction (FlashUtils.getFrameIntByString (mcTimeLineInfo, frameLabel_.parameter));
			} else if (frameLabel_.name == FlashUtils.FrameNameType.remove) {
				removeFromParent ();
			}
		}

		//Back null means the path can not get any transfrom...
		public MovieClip getMovieClipByPath (
			string path_
		) {
			MovieClip _currentNode;
			if (_movieClipOnPathCache.TryGetValue (path_, out _currentNode)) {
				return _currentNode;
			}

			ArrayList _uiPathArr;
			if (path_.Contains (".")) {
				_uiPathArr = new ArrayList (path_.Split ('.'));
			} else {
				_uiPathArr = new ArrayList ();
				_uiPathArr.Add (path_);
			}

			_currentNode = this;

			string _currentKey;
			while (_uiPathArr.Count > 0) {
				_currentKey = (string) _uiPathArr[0];
				_uiPathArr.RemoveAt (0);
				if (_currentKey == "p" || _currentKey == "parent") {
					if (_currentNode.parentMovieClip == null) {
						Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " +
							new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
							gameObject.name + " has no parent."
						);
						return null;
					}

					_currentNode = _currentNode.parentMovieClip;
				} else if (_currentKey == "t" || _currentKey == "this") {
					Debug.LogError ("ERROR " + System.Reflection.MethodBase.GetCurrentMethod ().ReflectedType.FullName + " -> " +
						new System.Diagnostics.StackTrace ().GetFrame (0).GetMethod ().Name + " : " +
						"t or this is not allow in  'getMovieClipByPath'. " + path_
					);
				} else {
					_currentNode = _currentNode.getChildByName (_currentKey) as MovieClip;
				}
			}

			_movieClipOnPathCache[path_] = _currentNode; //Cache it
			return _currentNode;
		}

		public DisplayObject getChildByName (string name_) {
			FlashUtils.ChildInfo _childInfo;
			var _enume = _disOnStage.GetEnumerator ();
			while (_enume.MoveNext ()) {
				_childInfo = _enume.Current.Key;
				if (_childInfo.insName == name_) {
				_enume.Dispose ();
				return _enume.Current.Value; //Back when instance name match.
				}
			}

			_enume.Dispose ();
			return null;
		}
		public void putAllChildOnStage () {
			FlashUtils.ChildInfo[] _childrenInfos = mcTimeLineInfo.childrenInfos;
			var _length = _childrenInfos.Length;
			FlashUtils.ChildInfo _childInfo;
			DisplayObject _displayObject = null;
			MovieClip _movieClip;
			Sprite _sprite;
			for (int _idx = 0; _idx < _length; _idx++) {
			_childInfo = _childrenInfos[_idx];
			if (_childInfo.superClassName == 2 /*(int) FlashUtils.ClassType.MovieClip */ ) { //No cache
			_movieClip = FlashUtils.getMovieClipByChildInfoAndAddTo (_childInfo, _mainMovieClip, this);
			_displayObject = _movieClip;
			} else if (_childInfo.superClassName == 3 /*(int) FlashUtils.ClassType.Sprite */ ) {
			_sprite = FlashUtils.getPicNodeAndAddTo (_mainMovieClip, selfTrans); //SpriteRenderer
			_sprite.initByChildInfo (_childInfo, this);
			_displayObject = _sprite;
				}
				_displayObject.resetParsWhenOnOrOffStage (true); //Reset parameters.Use it as new one.
				_disOnStage[_childInfo] = _displayObject; //cache it as on stage
				_displayObject.selfTrans.parent = selfTrans;
				_displayObject.selfTrans.SetSiblingIndex (_childInfo.childIndex);
			}
		}
		public void cacheNotOnStageDisplayObject (DisplayObject displayObject_) {
			if (notOnStageContainer == null) {
				notOnStageContainer = getNotOnStageContainer ();
			}
			displayObject_.selfTrans.parent = notOnStageContainer;
		}

		public Transform getNotOnStageContainer () {
			Transform _notOnStageContainer = new GameObject ().transform;
			_notOnStageContainer.parent = selfTrans;
			_notOnStageContainer.name = "nodeNotOnStage";
			_notOnStageContainer.gameObject.SetActive (false);
			_notOnStageContainer.SetSiblingIndex (0);
			return _notOnStageContainer;
		}
		public string currentLocalFrameName () {
			return FlashUtils.getFrameIntLocalInFrameName (mcTimeLineInfo, currentFrame);
		}
	}
}