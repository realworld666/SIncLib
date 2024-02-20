using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SIncLib
{
    public class Utils
    {
        public static RectTransform AddPanel(Rect rect, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            RectTransform panel = WindowManager.SpawnPanel();
            WindowManager.AddElementToWindow(panel.gameObject, window, rect, new Rect(0, 0, 0, 0));
            return panel;
        }
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

            return AddButton(Text, Button, Action, window.MainPanel);
        }

        public static Button AddButton(string Text, Rect Button, UnityAction Action, GameObject panel)
        {
            Button x = WindowManager.SpawnButton();
            x.GetComponentInChildren<UnityEngine.UI.Text>().text = Text;
            x.onClick.AddListener(Action);
            WindowManager.AddElementToElement(x.gameObject, panel, Button, new Rect(0, 0, 0, 0));
            return x;
        }

        public static InputField AddInputBox(string Text, Rect InputBox, UnityAction<string> Action, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            return AddInputBox(Text, InputBox, Action, window.MainPanel);
        }

        public static InputField AddInputBox(string Text, Rect InputBox, UnityAction<string> Action, GameObject panel)
        {
            InputField x = WindowManager.SpawnInputbox();
            x.text = Text;
            x.onValueChanged.AddListener(Action);
            WindowManager.AddElementToElement(x.gameObject, panel, InputBox, new Rect(0, 0, 0, 0));

            return x;
        }

        public static InputField AddIntField(int Value, Rect InputBox, UnityAction<int> Action, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            return AddIntField(Value, InputBox, Action, window.MainPanel);
        }

        public static InputField AddIntField(int Value, Rect InputBox, UnityAction<int> Action, GameObject panel)
        {
            InputField x = WindowManager.SpawnInputbox();
            x.text = Value.ToString();
            x.contentType = InputField.ContentType.IntegerNumber;
            x.onValueChanged.AddListener(value => Action.Invoke(int.Parse(value)));
            WindowManager.AddElementToElement(x.gameObject, panel, InputBox, new Rect(0, 0, 0, 0));

            return x;
        }

        public static Text AddLabel(string Text, Rect Label, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            return AddLabel(Text, Label, window.MainPanel);
        }

        public static Text AddLabel(string Text, Rect Label, GameObject panel)
        {
            Text x = WindowManager.SpawnLabel();
            x.text = Text;
            WindowManager.AddElementToElement(x.gameObject, panel, Label, new Rect(0, 0, 0, 0));

            return x;
        }

        public static Toggle AddToggle(string Text, Rect rect, bool isOn, UnityAction<bool> Action, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            return AddToggle(Text, rect, isOn, Action, window.MainPanel);
        }

        public static Toggle AddToggle(string Text, Rect rect, bool isOn, UnityAction<bool> Action, GameObject panel)
        {
            Toggle toggle = WindowManager.SpawnCheckbox();
            Text textComponent = toggle.GetComponentInChildren<UnityEngine.UI.Text>();
            textComponent.text = Text;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(Action);
            WindowManager.AddElementToElement(toggle.gameObject, panel, rect, new Rect(0, 0, 0, 0));

            return toggle;
        }

        public static GUIListView AddListView(Rect rect, GUIWindow window = null)
        {
            if (window == null)
                window = SIncLibUI.Window;

            return AddListView(rect, window.MainPanel);
        }

        public static GUIListView AddListView(Rect rect, GameObject panel)
        {
            GUIListView listView = WindowManager.SpawnList();

            WindowManager.AddElementToElement(listView.gameObject, panel, rect, new Rect(0, 0, 1f, 1f));

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