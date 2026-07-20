using System.Collections.Generic;
using UnityEngine;

namespace PixelUI {
    public class CustomCursor : MonoBehaviour {
        [System.Serializable]
        public class CursorMapping {
            public CursorType type;
            public Texture2D texture;
            public Vector2 hotSpot = Vector2.zero;
        }

        public enum CursorType {
            Arrow,
            ArrowAdd,
            ArrowLarge,
            ArrowSubtract,
            Crosshairs,
            Dialogue,
            DialogueEllipsis,
            Eraser,
            Eyedropper,
            EyeHidden,
            EyeVisible,
            FillBucket,
            Grab,
            Hammer,
            HandDown,
            HandLeft,
            HandRight,
            HandUp,
            HourglassBusy,
            HourglassCompleted,
            HourglassEmpty,
            HourglassIncomplete,
            IBeam,
            Lock,
            LockUnlocked,
            MagicPencil,
            Magnify,
            Marker,
            Maximize,
            MoveAll,
            MoveDiagonallyLeft,
            MoveDiagonallyRight,
            MoveHorizontally,
            MoveVertically,
            Pencil,
            Pointer,
            PointerAdd,
            PointerExclamation,
            PointerHourglass,
            PointerQuestion,
            PointerSmall,
            PointerSubtract,
            PointerUnavailable,
            Redo,
            SizeAll,
            SizeHorizontally,
            SizeVertically,
            SplitHorizontally,
            SplitVertically,
            Sword,
            Target,
            Unavailable,
            Undo,
            ZoomIn,
            ZoomOut,
        }

        public CursorType cursor;
        public List<CursorMapping> cursors;
        public CursorMode cursorMode = CursorMode.Auto;
        public int cursorScale = 3;

        void Start() {
            SetImage(cursor);
        }

        public void SetImage(CursorType type) {
            foreach (var cursor in cursors) {
                if (cursor.type == type) {
                    var texture = cursor.texture;
                    var hotspot = cursor.hotSpot;
                    //
                    // #if UNITY_EDITOR
                    // Cursor.SetCursor(texture, hotspot, cursorMode);
                    // #else
                    // #endif
                    Cursor.SetCursor(ResizeTexture(texture, texture.width * cursorScale, texture.height * cursorScale), hotspot * cursorScale, cursorMode);
                }
            }
        }

        Texture2D ResizeTexture(Texture2D original, int width, int height) {
            var rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(original, rt);
            var result = new Texture2D(width, height, original.format, false);
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
    }
}