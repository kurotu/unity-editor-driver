using System;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Creates and dispatches synthetic input events to EditorWindows.
    /// </summary>
    public static class InputSimulator
    {
        public static void MouseDown(EditorWindow window, Vector2 position, int button = 0)
        {
            var evt = CreateMouseEvent(EventType.MouseDown, position, button);
            window.SendEvent(evt);
        }

        public static void MouseUp(EditorWindow window, Vector2 position, int button = 0)
        {
            var evt = CreateMouseEvent(EventType.MouseUp, position, button);
            window.SendEvent(evt);
        }

        public static void MouseClick(EditorWindow window, Vector2 position, int button = 0)
        {
            MouseDown(window, position, button);
            MouseUp(window, position, button);
        }

        public static void KeyDown(EditorWindow window, KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
        {
            var evt = CreateKeyEvent(EventType.KeyDown, keyCode, modifiers);
            window.SendEvent(evt);
        }

        public static void KeyUp(EditorWindow window, KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
        {
            var evt = CreateKeyEvent(EventType.KeyUp, keyCode, modifiers);
            window.SendEvent(evt);
        }

        public static void TypeText(EditorWindow window, string text)
        {
            foreach (var c in text)
            {
                var keyCode = CharToKeyCode(c);
                var evt = new Event();
                evt.keyCode = keyCode;
                evt.character = c;
                evt.modifiers = EventModifiers.None;
                evt.type = EventType.KeyDown;
                window.SendEvent(evt);
            }
        }

        public static Event CreateMouseEvent(EventType type, Vector2 position, int button = 0)
        {
            var evt = new Event();
            evt.mousePosition = position;
            evt.button = button;
            // Set type last — Unity's native Event may reset type when other properties change
            evt.type = type;
            return evt;
        }

        public static Event CreateKeyEvent(EventType type, KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
        {
            var evt = new Event();
            evt.keyCode = keyCode;
            evt.modifiers = modifiers;

            // Set character for printable keys on KeyDown
            if (type == EventType.KeyDown)
            {
                var c = KeyCodeToChar(keyCode, modifiers);
                if (c != '\0')
                {
                    evt.character = c;
                }
            }

            // Set type last
            evt.type = type;
            return evt;
        }

        private static KeyCode CharToKeyCode(char c)
        {
            if (c >= 'a' && c <= 'z')
                return KeyCode.A + (c - 'a');
            if (c >= 'A' && c <= 'Z')
                return KeyCode.A + (c - 'A');
            if (c >= '0' && c <= '9')
                return KeyCode.Alpha0 + (c - '0');

            return c switch
            {
                ' ' => KeyCode.Space,
                '\n' or '\r' => KeyCode.Return,
                '\t' => KeyCode.Tab,
                '.' => KeyCode.Period,
                ',' => KeyCode.Comma,
                '/' => KeyCode.Slash,
                '\\' => KeyCode.Backslash,
                '-' => KeyCode.Minus,
                '=' => KeyCode.Equals,
                ';' => KeyCode.Semicolon,
                '\'' => KeyCode.Quote,
                '[' => KeyCode.LeftBracket,
                ']' => KeyCode.RightBracket,
                '`' => KeyCode.BackQuote,
                _ => KeyCode.None,
            };
        }

        private static char KeyCodeToChar(KeyCode keyCode, EventModifiers modifiers)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                var offset = keyCode - KeyCode.A;
                return (modifiers & EventModifiers.Shift) != 0
                    ? (char)('A' + offset)
                    : (char)('a' + offset);
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                var offset = keyCode - KeyCode.Alpha0;
                return (char)('0' + offset);
            }

            return '\0';
        }
    }
}
