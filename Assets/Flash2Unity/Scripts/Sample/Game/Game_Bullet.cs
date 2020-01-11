using flash.display;
using UnityEngine;
public class Game_Bullet : MovieClip {
    private float _acceleratedVeocity = 0.4f;
    private float _maximumVelocity = 25.0f;
    private float _maximumLifeTime = 0.5f;
    public MovieClip target = null;
    private float _currentVelocity = 15f;
    private float _lifeTime = 0.0f;
    private bool _isDestoryed = false;

    private void Start () {
        Shader _shader = Shader.Find ("Sprites/Default");
        Material _material = new Material (_shader);
        TrailRenderer _trailRenderer = gameObject.AddComponent<TrailRenderer> ();
        _trailRenderer.material = _material;
        _trailRenderer.textureMode = LineTextureMode.RepeatPerSegment;
        _trailRenderer.startColor = Color.red;
        _trailRenderer.endColor = Color.yellow;
        _trailRenderer.startWidth = 0.02f;
        _trailRenderer.endWidth = 0.01f;
        _trailRenderer.time = 0.1f;
    }

    private void Explode () {
        _isDestoryed = true;
        FlashUtils.getMovieClipByClassNameAndAddTo ("Game_BulletHitPar", transform.parent).selfTrans.position = transform.position;
        removeFromParent ();
    }

    public override void frameUpdate () {
        base.frameUpdate ();
        if (target == null) {
            return;
        }
        float _deltaTime = Time.deltaTime;
        _lifeTime += _deltaTime;
        if (_lifeTime > _maximumLifeTime) {
            removeFromParent ();
            return;
        }
        if (_isDestoryed) {
            return;
        }
        if (_currentVelocity < _maximumVelocity) {
            _currentVelocity += _deltaTime * _acceleratedVeocity;
        }

        selfTrans.position += selfTrans.forward * _currentVelocity * _deltaTime;
        if ((selfTrans.position - target.selfTrans.position).sqrMagnitude < .9f) {
            Explode ();
            Game_ShootGameDis.getInstance ().isHit = true;
        }
    }
}