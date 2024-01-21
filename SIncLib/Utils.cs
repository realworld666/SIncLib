using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SIncLib
{
    public class Utils
    {
        public static Button AddButton(string Text, UnityAction Action, ref List<GameObject> Buttons)
        {
            Button x = WindowManager.SpawnButton();
            x.GetComponentInChildren<Text>().text = Text;
            x.onClick.AddListener(Action);
            Buttons.Add(x.gameObject);
            return x;
        }

        public static Button AddButton(string Text, Rect Button, UnityAction Action, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            Button x = WindowManager.SpawnButton();
            x.GetComponentInChildren<UnityEngine.UI.Text>().text = Text;
            x.onClick.AddListener(Action);
            WindowManager.AddElementToWindow(x.gameObject, window, Button, new Rect(0, 0, 0, 0));
            return x;
        }

        public static void AddInputBox(string Text, Rect InputBox, UnityAction<string> Action, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            InputField x = WindowManager.SpawnInputbox();
            x.text = Text;
            x.onValueChanged.AddListener(Action);
            WindowManager.AddElementToWindow(x.gameObject, window, InputBox, new Rect(0, 0, 0, 0));
        }

        public static void AddIntField(int Value, Rect InputBox, UnityAction<int> Action, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            InputField x = WindowManager.SpawnInputbox();
            x.text = Value.ToString();
            x.contentType = InputField.ContentType.IntegerNumber;
            x.onValueChanged.AddListener(value => Action.Invoke(int.Parse(value)));
            WindowManager.AddElementToWindow(x.gameObject, window, InputBox, new Rect(0, 0, 0, 0));
        }

        public static void AddLabel(string Text, Rect Label, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            Text x = WindowManager.SpawnLabel();
            x.text = Text;
            WindowManager.AddElementToWindow(x.gameObject, window, Label, new Rect(0, 0, 0, 0));
        }

        public static void AddToggle(string Text, Rect rect, bool isOn, UnityAction<bool> Action, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            Toggle toggle = WindowManager.SpawnCheckbox();
            toggle.GetComponentInChildren<UnityEngine.UI.Text>().text = Text;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(Action);
            WindowManager.AddElementToWindow(toggle.gameObject, window, rect, new Rect(0, 0, 0, 0));
        }

        public static GUIListView AddListView(Rect rect, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            GUIListView listView = WindowManager.SpawnList();

            WindowManager.AddElementToWindow(listView.gameObject, window, rect, new Rect(0, 0, 1f, 1f));

            return listView;
        }

        public static void DoLoops(GameObject[] Buttons, GameObject[] Col1, GameObject[] Col2, GameObject[] Col3, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            for (int i = 0; i < Buttons.Length; i++)
            {
                GameObject item = Buttons[i];

                WindowManager.AddElementToWindow(item, window, new Rect(1, (i + 7) * 32, 150, 32),
                    new Rect(0, 0, 0, 0));
            }

            for (int i = 0; i < Col1.Length; i++)
            {
                GameObject item = Col1[i];

                WindowManager.AddElementToWindow(item, window, new Rect(161, (i + 7) * 32, 150, 32),
                    new Rect(0, 0, 0, 0));
            }

            for (int i = 0; i < Col2.Length; i++)
            {
                GameObject item = Col2[i];

                WindowManager.AddElementToWindow(item, window, new Rect(322, (i + 7) * 32, 150, 32),
                    new Rect(0, 0, 0, 0));
            }

            for (int i = 0; i < Col3.Length; i++)
            {
                GameObject item = Col3[i];

                WindowManager.AddElementToWindow(item, window, new Rect(483, (i + 7) * 32, 150, 32),
                    new Rect(0, 0, 0, 0));
            }
        }
    }
}