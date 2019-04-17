using UnityEngine;

namespace SIncLib
{
    public class SIncLibMod : ModMeta
    {
        
        public static bool ModActive { get; set; }
        
        public override void ConstructOptionsScreen(RectTransform parent, bool inGame)
        {
            if (inGame)
            {
                

                //Start by spawning a label
                var label = WindowManager.SpawnLabel();
                label.text = "This Mod was created by Otters Pocket";
                WindowManager.AddElementToElement(label.gameObject, parent.gameObject, new Rect(0, 0, 250, 32),
                    new Rect(0, 0, 0, 0));


            }
        }

        public override string Name
        {
            get { return "SIncLib"; }
        }
    }
}