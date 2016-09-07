using System;
using System.Collections.Generic;
using UnityEngine;

namespace PWBFuelBalancer
{
  // Utils - Borrowed from KSP Select Root Mod - credit where it is due
  public class Osd
  {
    private class Message
    {
      public string Text;
      public Color Color;
      public float HideAt;
    }

    private List<Message> _msgs = new List<Message>();

    private static GUIStyle CreateStyle(Color color)
    {
      GUIStyle style = new GUIStyle
      {
        stretchWidth = true,
        alignment = TextAnchor.MiddleCenter,
        fontSize = 20,
        fontStyle = FontStyle.Bold,
        normal = { textColor = color }
      };
      return style;
    }

    private Predicate<Message> _pre = delegate (Message m) { return (Time.time >= m.HideAt); };
    private Action<Message> _showMesssage = delegate (Message m) { GUILayout.Label(m.Text, CreateStyle(m.Color)); };

    public void Update()
    {
      if (_msgs.Count == 0) return;
      _msgs.RemoveAll(_pre);
      GUILayout.BeginArea(new Rect(0, Screen.height * 0.1f, Screen.width, Screen.height * 0.8f), CreateStyle(Color.white));
      _msgs.ForEach(_showMesssage);
      GUILayout.EndArea();
    }

    public void Error(string text)
    {
      AddMessage(text, XKCDColors.LightRed);
    }

    public void Success(string text)
    {
      AddMessage(text, XKCDColors.Cerulean);
    }

    public void Info(string text)
    {
      AddMessage(text, XKCDColors.OffWhite);
    }

    public void AddMessage(string text, Color color, float shownFor)
    {
      Message msg = new Message
      {
        Text = text,
        Color = color,
        HideAt = Time.time + shownFor
      };
      _msgs.Add(msg);
    }

    public void AddMessage(string text, Color color)
    {
      AddMessage(text, color, 3);
    }
  }

}
