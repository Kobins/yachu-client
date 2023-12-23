using System;
using UnityEngine;

namespace Yachu.Client
{

    public class AlertMessagePanel : MonoBehaviour
    {
        public CanvasGroup _canvasGroup;
        public AnimationCurve _alphaCurve = AnimationCurve.EaseInOut(5f, 1f, 6f, 0f);

        private float _duration;
        private float _time;

        private void OnEnable()
        {
            ResetTime();
        }

        private void ResetTime()
        {
            _duration = _alphaCurve[_alphaCurve.length - 1].time;
            _time = 0f;
        }

        private void Update()
        {
            if (_time > _duration)
            {
                gameObject.SetActive(false);
                return;
            }
            _canvasGroup.alpha = _alphaCurve.Evaluate(_time);
            _time += Time.deltaTime;
        }
    }
}