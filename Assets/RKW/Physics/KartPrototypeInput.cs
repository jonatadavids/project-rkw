using UnityEngine;
using UnityEngine.InputSystem;

namespace RKW.Physics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(KartDynamics))]
    public sealed class KartPrototypeInput : MonoBehaviour
    {
        private KartDynamics _dynamics;
        private Rect _steerLeftTouchArea;
        private Rect _steerRightTouchArea;
        private Rect _throttleTouchArea;
        private Rect _brakeTouchArea;
        private float _steering;
        private float _throttle;
        private float _brake;

        private void Awake()
        {
            _dynamics = GetComponent<KartDynamics>();
        }

        private void Update()
        {
            ReadKeyboard();
            ReadTouches();
            _dynamics.SetInput(_steering, _throttle, _brake);
        }

        private void ReadKeyboard()
        {
            _steering = 0f;
            _throttle = 0f;
            _brake = 0f;

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                _steering -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                _steering += 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                _throttle = 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.spaceKey.isPressed)
            {
                _brake = 1f;
            }
        }

        private void ReadTouches()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            CalculateTouchAreas();
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    continue;
                }

                var position = touch.position.ReadValue();
                if (_steerLeftTouchArea.Contains(position))
                {
                    _steering = -1f;
                }
                else if (_steerRightTouchArea.Contains(position))
                {
                    _steering = 1f;
                }
                else if (_throttleTouchArea.Contains(position))
                {
                    _throttle = 1f;
                }
                else if (_brakeTouchArea.Contains(position))
                {
                    _brake = 1f;
                }
            }
        }

        private void CalculateTouchAreas()
        {
            var safe = Screen.safeArea;
            _steerLeftTouchArea = new Rect(safe.xMin, safe.yMin, safe.width * 0.22f, safe.height * 0.42f);
            _steerRightTouchArea = new Rect(safe.xMin + safe.width * 0.22f, safe.yMin,
                safe.width * 0.22f, safe.height * 0.42f);
            _throttleTouchArea = new Rect(safe.xMin + safe.width * 0.72f, safe.yMin + safe.height * 0.34f,
                safe.width * 0.28f, safe.height * 0.66f);
            _brakeTouchArea = new Rect(safe.xMin + safe.width * 0.55f, safe.yMin,
                safe.width * 0.45f, safe.height * 0.34f);
        }

        private void OnGUI()
        {
            CalculateTouchAreas();
            var safe = Screen.safeArea;
            var scale = Mathf.Max(1f, Screen.height / 720f);
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(20f * scale),
                normal = { textColor = Color.white }
            };

            DrawScreenRect(_steerLeftTouchArea, "← ESQUERDA", style);
            DrawScreenRect(_steerRightTouchArea, "DIREITA →", style);
            DrawScreenRect(_throttleTouchArea, "ACELERAR", style);
            DrawScreenRect(_brakeTouchArea, "FREAR / RÉ", style);

            var hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            var motionLabel = _dynamics.SignedForwardSpeedKph < -0.5f ? "RÉ " : string.Empty;
            GUI.Label(new Rect(safe.x + 20f, Screen.height - safe.yMax + 14f, 420f * scale, 46f * scale),
                $"{motionLabel}{_dynamics.SpeedKph:0} km/h  •  grip {_dynamics.GripRatio:0.00}", hudStyle);
        }

        private static void DrawScreenRect(Rect screenRect, string label, GUIStyle style)
        {
            var guiRect = new Rect(screenRect.x, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.Box(guiRect, label, style);
            GUI.color = previous;
        }
    }
}
