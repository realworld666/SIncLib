using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SIncLib
{
    public class SIncLibUI : ModBehaviour
    {
        public static Button btn;

        public bool ModActive { get; set; }

        
        public static void SpawnButton()
        {
            btn = WindowManager.SpawnButton();
            btn.GetComponentInChildren<Text>().text = "SIncLib";
            btn.onClick.AddListener(Show);
            btn.name = "SIncLibButton";

            WindowManager.AddElementToElement(btn.gameObject,
                WindowManager.FindElementPath("MainPanel/Holder/FanPanel").gameObject, new Rect(264, 0, 100, 32),
                new Rect(0, 0, 0, 0));
        }

        public override void OnDeactivate()
        {
            Destroy(btn.gameObject);
        }

        public override void OnActivate()
        {
            if (SceneManager.GetActiveScene().name.Equals("MainScene"))
            {
                SpawnButton();
            }
        }
        
        public static GUIWindow Window;
        private static string title = "SIncLib by Otters Pocket";
        public static bool shown = false;

        public static void Show()
        {
            if (shown)
            {
                Window.Close();
                shown = false;
                return;
            }
            Init();
            shown = true;
        }

        private static void Init()
        {
            Window = WindowManager.SpawnWindow();
            Window.InitialTitle =  Window.TitleText.text = Window.NonLocTitle = title;
            Window.MinSize.x = 670;
            Window.MinSize.y = 580;
            Window.name = "SIncLibOptions";
            Window.MainPanel.name = "SIncLibOptionsPanel";

            if (Window.name == "SIncLibOptions")
            {
                Window.GetComponentsInChildren<Button>()
                  .SingleOrDefault(x => x.name == "CloseButton")
                  .onClick.AddListener(() => shown = false);
            }

            List<GameObject> Buttons = new List<GameObject>();
            List<GameObject> col1 = new List<GameObject>();
            List<GameObject> col2 = new List<GameObject>();
            List<GameObject> col3 = new List<GameObject>();


            Utils.AddButton("DO SOMETHING!", new Rect(1, 0, 150, 32), SIncLibBehaviour.DoSomething);


            Utils.DoLoops(Buttons.ToArray(), col1.ToArray(), col2.ToArray(), col3.ToArray());
        }
    }
}