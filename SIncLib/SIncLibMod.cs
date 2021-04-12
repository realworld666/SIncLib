using UnityEngine;

namespace SIncLib
{
    public class SIncLibMod : ModMeta
    {
        public static string Version = "0.5";
        public static bool ModActive { get; set; }
        
        public override void ConstructOptionsScreen(RectTransform parent, bool inGame)
        {
            if (inGame)
            {
                //Start by spawning a label
                var label = WindowManager.SpawnLabel();
                label.text = "SIncLib v"+Version+" was created by Otters Pocket. GitHub @ https://github.com/realworld666/SIncLib";
                WindowManager.AddElementToElement(label.gameObject, parent.gameObject, new Rect(0, 0, 400, 75),
                    new Rect(0, 0, 0, 0));


            }
        }

        public override string Name
        {
            get { return "SIncLib"; }
        }
    }
}