﻿using System.Collections.Generic;
using System.Numerics;
using Engine.Display;
using Veldrid;
using Point = Engine.Geom.Point;

namespace Engine
{
    public static class Input
    {
        private static readonly HashSet<Key> _currentlyPressedKeys = new HashSet<Key>();
        private static readonly HashSet<Key> _newKeysThisFrame = new HashSet<Key>();

        private static readonly HashSet<MouseButton> _currentlyPressedMouseButtons = new HashSet<MouseButton>();
        private static readonly HashSet<MouseButton> _newMouseButtonsThisFrame = new HashSet<MouseButton>();
        private static (double timeSinceStart, Vector2 mouseCoords) _mouseDownData;

        /// <summary>
        /// Returns if the given key is pressed down currently.
        /// </summary>
        public static bool IsKeyDown(Key key)
        {
            return _currentlyPressedKeys.Contains(key);
        }

        /// <summary>
        /// Returns if the given keyboard key was pressed this frame.
        /// </summary>
        public static bool IsKeyPressedThisFrame(Key key)
        {
            return _newKeysThisFrame.Contains(key);
        }

        private static DisplayObject _lastMouseDownObject;
        public static void UpdateFrameInput(InputSnapshot snapshot, double elapsedTimeSinceStart)
        {
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();
            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                KeyEvent ke = snapshot.KeyEvents[i];
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            var mousePosition = snapshot.MousePosition;
            for (int i = 0; i < snapshot.MouseEvents.Count; i++)
            {
                MouseEvent me = snapshot.MouseEvents[i];
                if (me.Down)
                {
                    if (_currentlyPressedMouseButtons.Add(me.MouseButton))
                    {
                        _newMouseButtonsThisFrame.Add(me.MouseButton);
                        _lastMouseDownObject = KestrelApp.Stage.DispatchMouseDownInternal(me.MouseButton, mousePosition);
                        if (me.MouseButton == MouseButton.Left)
                        {
                            _mouseDownData = (elapsedTimeSinceStart, mousePosition);   
                        }
                    }
                }
                else
                {
                    _currentlyPressedMouseButtons.Remove(me.MouseButton);
                    _newMouseButtonsThisFrame.Remove(me.MouseButton);
                    var lastMouseUpObject = KestrelApp.Stage.DispatchMouseUpInternal(me.MouseButton, mousePosition);
                    if (me.MouseButton == MouseButton.Left && 
                        elapsedTimeSinceStart -_mouseDownData.timeSinceStart < 0.3 &&
                        _lastMouseDownObject == lastMouseUpObject)
                    {
                        //Console.WriteLine("CLICK " + mousePosition + " " + lastMouseUpObject);
                        lastMouseUpObject.DispatchMouseClick(new Point(mousePosition.X, mousePosition.Y));
                    }
                    _lastMouseDownObject = null;
                }
            }
            KestrelApp.Stage.OnMouseMoveInternal(mousePosition.X, mousePosition.Y);
        }

        private static void KeyUp(Key key)
        {
            _currentlyPressedKeys.Remove(key);
            _newKeysThisFrame.Remove(key);
        }

        private static void KeyDown(Key key)
        {
            if (_currentlyPressedKeys.Add(key))
            {
                _newKeysThisFrame.Add(key);
            }
        }
    }
}
