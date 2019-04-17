using UnityEngine;
using UnityEngine.SceneManagement;

namespace SIncLib
{
    public class SIncLibBehaviour : ModBehaviour
    {
        private void Start()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }
            
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
                {
                    //Other scenes include MainScene and Customization
                    if (scene.name.Equals("MainMenu") && SIncLibUI.btn != null)
                    {
                        Destroy(SIncLibUI.btn.gameObject);
                    }
                    else if (scene.name.Equals("MainScene") && isActiveAndEnabled)
                    {
                        SIncLibUI.SpawnButton();
                    }
                }

        public override void OnDeactivate()
        {
            SIncLibMod.ModActive = false;
            if (!SIncLibMod.ModActive && GameSettings.Instance != null && HUD.Instance != null)
            {
                HUD.Instance.AddPopupMessage("SIncLibUI has been deactivated!", "Cogs", PopupManager.PopUpAction.None,
                    0, PopupManager.NotificationSound.Neutral, Color.black, 0f, PopupManager.PopupIDs.None);
            }
        }

        public override void OnActivate()
        {
            SIncLibMod.ModActive = true;
            if (SIncLibMod.ModActive && GameSettings.Instance != null && HUD.Instance != null)
            {
                HUD.Instance.AddPopupMessage("SIncLibUI has been activated!", "Cogs", PopupManager.PopUpAction.None,
                    0, PopupManager.NotificationSound.Neutral, Color.black, 0f, PopupManager.PopupIDs.None);
            }
        }

        public static void DoSomething()
        {
            HUD.Instance.AddPopupMessage("Done something!", "Cogs", PopupManager.PopUpAction.None,
                0, PopupManager.NotificationSound.Neutral, Color.black, 0f, PopupManager.PopupIDs.None);
        }
    }
}