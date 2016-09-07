using System;
using UnityEngine;

namespace PWBFuelBalancer
{
  public static class GuiUtils
  {
    private static GUIStyle _yellowOnHover;
    public static GUIStyle YellowOnHover
    {
      get
      {
        if (_yellowOnHover != null) return _yellowOnHover;
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, new Color(0, 0, 0, 0));
        t.Apply();
        _yellowOnHover = new GUIStyle(GUI.skin.label)
        {
          hover =
        {
          textColor = Color.yellow,
          background = t
        }
        };
        return _yellowOnHover;
      }
    }


    // Code blagged directly out of MechJeb - Credit where it is due!
    public class ComboBox
    {
      // Easy to use combobox class
      // ***** For users *****
      // Call the Box method with the latest selected item, list of text entries
      // and an object identifying who is making the request.
      // The result is the newly selected item.
      // There is currently no way of knowing when a choice has been made

      // Position of the popup
      private static Rect _rect;
      // Identifier of the caller of the popup, null if nobody is waiting for a value
      private static object _popupOwner;
      private static string[] _entries;
      private static bool _popupActive;
      // Result to be returned to the owner
      private static int _selectedItem;
      // Unity identifier of the window, just needs to be unique
      private static int _id = GUIUtility.GetControlID(FocusType.Passive);
      // ComboBox GUI Style
      private static GUIStyle _style;

      static ComboBox()
      {
        Texture2D background = new Texture2D(16, 16, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

        for (int x = 0; x < background.width; x++)
          for (int y = 0; y < background.height; y++)
          {
            if (x == 0 || x == background.width - 1 || y == 0 || y == background.height - 1)
              background.SetPixel(x, y, new Color(0, 0, 0, 1));
            else
              background.SetPixel(x, y, new Color(0.05f, 0.05f, 0.05f, 0.95f));
          }

        background.Apply();

        _style = new GUIStyle(GUI.skin.window)
        {
          normal = { background = background },
          onNormal = { background = background }
        };
        _style.border.top = _style.border.bottom;
        _style.padding.top = _style.padding.bottom;
      }

      public static void DrawGui()
      {
        //Debug.Log("popupActive: " + popupActive);

        if (_popupOwner == null || _rect.height == 0 || !_popupActive)
          return;

        // Make sure the rectangle is fully on screen
        _rect.x = Math.Max(0, Math.Min(_rect.x, Screen.width - _rect.width));
        _rect.y = Math.Max(0, Math.Min(_rect.y, Screen.height - _rect.height));

        _rect = GUILayout.Window(_id, _rect, identifier =>
        {
          _selectedItem = GUILayout.SelectionGrid(-1, _entries, 1, YellowOnHover);
          if (GUI.changed)
            _popupActive = false;
        }, "", _style);

        //Cancel the popup if we click outside
        if (Event.current.type == EventType.MouseDown && !_rect.Contains(Event.current.mousePosition))
          _popupOwner = null;
      }

      public static int Box(int selectedItem, string[] entries, object caller)
      {
        // Trivial cases (0-1 items)
        if (entries.Length == 0) return 0;
        if (entries.Length == 1)
        {
          GUILayout.Label(entries[0]);
          return 0;
        }

        // A choice has been made, update the return value
        if (_popupOwner == caller && !_popupActive)
        {
          _popupOwner = null;
          selectedItem = _selectedItem;
          GUI.changed = true;
        }

        bool guiChanged = GUI.changed;
        if (GUILayout.Button("↓ " + entries[selectedItem] + " ↓"))
        {
          // We will set the changed status when we return from the menu instead
          GUI.changed = guiChanged;
          // Update the global state with the new items
          _popupOwner = caller;
          _popupActive = true;
          _entries = entries;
          // Magic value to force position update during repaint event
          _rect = new Rect(0, 0, 0, 0);
        }
        // The GetLastRect method only works during repaint event, but the Button will return false during repaint
        if (Event.current.type != EventType.Repaint || _popupOwner != caller || _rect.height != 0) return selectedItem;
        _rect = GUILayoutUtility.GetLastRect();
        // But even worse, I can't find a clean way to convert from relative to absolute coordinates
        Vector2 mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y;
        Vector2 clippedMousePos = Event.current.mousePosition;
        _rect.x = (_rect.x + mousePos.x) - clippedMousePos.x;
        _rect.y = (_rect.y + mousePos.y) - clippedMousePos.y;

        return selectedItem;
      }
    }
  }
}
