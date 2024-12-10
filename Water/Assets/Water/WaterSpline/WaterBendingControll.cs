using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SplineMesh;

public class WaterBendingControll : MonoBehaviour
{
    [SerializeField] float _PointCount;
    [SerializeField] float _Radius;
    [SerializeField] float _HeightDelta;
    [SerializeField] Vector3 _Scale;

    [SerializeField] Spline _Spline;
    [SerializeField] ExampleContortAlong _ContortAlong;

    [SerializeField] float _SpeedDelta;
    [SerializeField] float _AnimSpeed;
    [SerializeField] ParticleSystem _PuddleParticle;
    [SerializeField] ParticleSystem _SplashParticle;
    [SerializeField] float _SplashActivationOffset;

    [SerializeField] float _PuddleScaleSpeed;

    private Vector3 _target;

    public void WaterBend(Vector3 target)
    {
        _target = target;
        StopAllCoroutines();
        StartCoroutine(Coroutine_WaterBend());
    }

    IEnumerator Coroutine_WaterBend()
    {
        _Spline.gameObject.SetActive(false);
        _SplashParticle.gameObject.SetActive(false);
        _PuddleParticle.gameObject.SetActive(false);

        ConfigureSpline();

        _ContortAlong.Init();
        float meshLength = _ContortAlong.MeshBender.Source.Length;
        meshLength = meshLength == 0 ? 1 : meshLength;
        float totalLength = meshLength + _Spline.Length;

        Vector3 startScale = _Scale;
        startScale.x = 0;
        Vector3 targetScale = _Scale;

        float speedCurveLerp = 0;
        float length = 0;

        _PuddleParticle.gameObject.SetActive(true);
        _PuddleParticle.transform.localPosition = _Spline.nodes[0].Position;

        Vector3 startPuddleScale = Vector3.zero;
        Vector3 endPuddleScale = _PuddleParticle.transform.localScale;
        float lerp = 0;
        while (lerp < 1)
        {
            _PuddleParticle.transform.localScale = Vector3.Lerp(startPuddleScale, endPuddleScale, lerp);
            lerp += Time.deltaTime * _PuddleScaleSpeed;
            yield return null;
        }

        _Spline.gameObject.SetActive(true);
        _PuddleParticle.Play();
        while (length < totalLength)
        {
            if (length < meshLength)
            {
                _ContortAlong.ScaleMesh(Vector3.Lerp(startScale, targetScale, length / meshLength));
            }
            else
            {
                if (_PuddleParticle.isPlaying)
                {
                    _PuddleParticle.Stop();
                }

                _ContortAlong.Contort((length - meshLength) / _Spline.Length);
                if (length + meshLength > totalLength + _SplashActivationOffset)
                {
                    if (!_SplashParticle.isPlaying)
                    {
                        _SplashParticle.gameObject.SetActive(true);
                        _SplashParticle.transform.position = _target;
                        _SplashParticle.Play();
                    }
                }
            }

            length += Time.deltaTime * _AnimSpeed * speedCurveLerp;
            speedCurveLerp += _SpeedDelta * Time.deltaTime;
            yield return null;
        }

        _Spline.gameObject.SetActive(false);
        _SplashParticle.Stop();
        Destroy(gameObject, 2f);
    }

    private void ConfigureSpline()
    {
        var nodes = new System.Collections.Generic.List<SplineNode>(_Spline.nodes);

        if (nodes.Count == 0)
        {
            Vector3 parentPosLocal = transform.InverseTransformPoint(transform.parent.position);
            Vector3 parentForwardLocal =
                transform.InverseTransformPoint(transform.parent.position + transform.parent.forward);
            SplineNode startNode = new SplineNode(parentPosLocal, parentForwardLocal);
            _Spline.AddNode(startNode);
            nodes.Add(startNode);
        }
        else
        {
            SplineNode firstNode = nodes[0];
            Vector3 parentPosLocal = transform.InverseTransformPoint(transform.parent.position);
            Vector3 parentForwardLocal =
                transform.InverseTransformPoint(transform.parent.position + transform.parent.forward);
            firstNode.Position = parentPosLocal;
            firstNode.Direction = parentForwardLocal;
            _Spline.nodes[0] = firstNode;
        }

        Vector3 targetDirection = (_target - transform.position);
        Vector3 flatDirection = new Vector3(targetDirection.x, 0, targetDirection.z).normalized;
        transform.forward = flatDirection;

        SplineNode finalNode;
        if (nodes.Count > 1)
        {
            finalNode = nodes[^1];
        }
        else
        {
            finalNode = new SplineNode(Vector3.zero, Vector3.forward);
            _Spline.AddNode(finalNode);
            nodes = new System.Collections.Generic.List<SplineNode>(_Spline.nodes);
        }

        finalNode.Position = transform.InverseTransformPoint(_target);

        Vector3 nodeWorldPos = transform.TransformPoint(finalNode.Position);
        Vector3 directionToTarget = (_target - nodeWorldPos).normalized;
        Vector3 directionWorld = nodeWorldPos + directionToTarget;
        finalNode.Direction = transform.InverseTransformPoint(directionWorld);

        _Spline.nodes[nodes.Count - 1] = finalNode;
    }
}