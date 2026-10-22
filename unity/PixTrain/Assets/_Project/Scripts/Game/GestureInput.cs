using UnityEngine;
using UnityEngine.InputSystem;

namespace PixTrain.Game
{
    public enum Gesture { None, Left, Right, Down, Up, Pause }

    /// <summary>
    /// Kaydırma hareketleri: sola/sağa kaydırma makası çevirir (parmak kalkmadan her adımda bir kez),
    /// aşağı çekme düdük, yukarı itme turbo. Klavye: ok tuşları, A/D, S/boşluk, W, Esc.
    /// </summary>
    public sealed class GestureInput
    {
        private bool _active;
        private bool _horizontal;
        private bool _vertical;
        private Vector2 _start;
        private Vector2 _last;

        /// <summary>Bir oyun pikselinin ekrandaki boyu; eşikler buna göre ölçeklenir.</summary>
        public float PixelScale = 2f;

        /// <summary>Hareketin başlamaması gereken ekran bölgesi (ör. mola düğmesi).</summary>
        public System.Func<Vector2, bool> IsBlocked;

        public void Reset() => _active = false;

        /// <summary>Bu karede algılanan hareketi döner (en fazla bir tane).</summary>
        public Gesture Poll()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) return Gesture.Left;
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) return Gesture.Right;
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) return Gesture.Down;
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) return Gesture.Up;
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame) return Gesture.Pause;
            }

            var pointer = Pointer.current;
            if (pointer == null) return Gesture.None;
            Vector2 pos = pointer.position.ReadValue();
            if (pointer.press.wasPressedThisFrame)
            {
                _active = IsBlocked == null || !IsBlocked(pos);
                _horizontal = _vertical = false;
                _start = _last = pos;
                return Gesture.None;
            }
            if (!pointer.press.isPressed || !_active)
            {
                _active = false;
                return Gesture.None;
            }

            float step = PixelScale * 12f, verticalStep = PixelScale * 18f;
            float dx = pos.x - _last.x;
            if (!_vertical && Mathf.Abs(dx) >= step && Mathf.Abs(dx) > Mathf.Abs(pos.y - _last.y))
            {
                _horizontal = true;
                _last = pos;
                return dx > 0 ? Gesture.Right : Gesture.Left;
            }
            // Ekran koordinatında y yukarı doğru artar: aşağı çekmek negatif dy.
            float dy = pos.y - _start.y;
            if (!_horizontal && !_vertical && Mathf.Abs(dy) >= verticalStep && Mathf.Abs(dy) > Mathf.Abs(pos.x - _start.x) * 1.4f)
            {
                _vertical = true;
                return dy < 0 ? Gesture.Down : Gesture.Up;
            }
            return Gesture.None;
        }
    }
}
