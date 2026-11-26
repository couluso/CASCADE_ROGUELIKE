// CASCADE — Auteur : Coulu
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cascade
{
    /// <summary>Compatible ancien Input Manager et nouveau Input System.</summary>
    public static class InputShim
    {
#if ENABLE_INPUT_SYSTEM
        static bool K(Key k) => Keyboard.current != null && Keyboard.current[k].wasPressedThisFrame;
        static bool H(Key k) => Keyboard.current != null && Keyboard.current[k].isPressed;
        public static Vector2 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        public static bool LeftClick => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool RightClick => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        public static bool NudgeLeft => K(Key.A) || K(Key.Q) || K(Key.LeftArrow);
        public static bool NudgeRight => K(Key.D) || K(Key.RightArrow);
        public static bool Shake => K(Key.R);
        public static bool Relance => K(Key.E);
        public static bool Brake => K(Key.F);
        public static bool Drop => K(Key.Space) || K(Key.Enter);
        public static bool ToggleVhs => K(Key.V);
        public static bool LeftHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool LeftReleased => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        public static Vector2 MousePosition => Input.mousePosition;
        public static bool LeftClick => Input.GetMouseButtonDown(0);
        public static bool RightClick => Input.GetMouseButtonDown(1);
        public static bool NudgeLeft => Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow);
        public static bool NudgeRight => Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        public static bool Shake => Input.GetKeyDown(KeyCode.R);
        public static bool Relance => Input.GetKeyDown(KeyCode.E);
        public static bool Brake => Input.GetKeyDown(KeyCode.F);
        public static bool Drop => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        public static bool ToggleVhs => Input.GetKeyDown(KeyCode.V);
        public static bool LeftHeld => Input.GetMouseButton(0);
        public static bool LeftReleased => Input.GetMouseButtonUp(0);
#endif

        public static bool PointerOverUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }
    }
}
