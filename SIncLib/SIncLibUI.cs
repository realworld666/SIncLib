using System;
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
            btn                                     = WindowManager.SpawnButton();
            btn.GetComponentInChildren<Text>().text = "SIncLib";
            btn.onClick.AddListener(Show);
            btn.name = "SIncLibButton";

            WindowManager.AddElementToElement(btn.gameObject,
                                              WindowManager.FindElementPath("MainPanel/Holder/FanPanel").gameObject,
                                              new Rect(264, 0, 100, 32),
                                              new Rect(0, 0, 0, 0));
        }

        public override void OnDeactivate()
        {
            Destroy(btn.gameObject);
            Destroy(Window.gameObject);
        }

        public override void OnActivate()
        {
            if (SceneManager.GetActiveScene().name.Equals("MainScene"))
            {
                SpawnButton();
            }
        }

        public static  GUIWindow       Window;
        private static string          title    = "SIncLib by Otters Pocket - v"+SIncLibMod.Version;
        public static  bool            shown    = false;
        private static HashSet<string> DevTeams = new HashSet<string>();
        private static Text            TeamText;
        private static Text            SourceText;

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
            DevTeams.Clear();
            DevTeams.AddRange<string>((IEnumerable<string>) GameSettings.Instance.GetDefaultTeams("Design"));

            Window                = WindowManager.SpawnWindow();
            Window.InitialTitle   = Window.TitleText.text = Window.NonLocTitle = title;
            Window.MinSize.x      = 670;
            Window.MinSize.y      = 400;
            Window.name           = "SIncLibOptions";
            Window.MainPanel.name = "SIncLibOptionsPanel";

            if (Window.name == "SIncLibOptions")
            {
                Window.GetComponentsInChildren<Button>()
                      .SingleOrDefault(x => x.name == "CloseButton")
                      .onClick.AddListener(() => shown = false);
            }


            Utils.AddLabel("Team Manager:", new Rect(10, 10, 250, 32));
            Utils.AddLabel("Team:", new Rect(10, 52, 250, 32));

            var teamButton = Utils.AddButton("SELECT TEAM", new Rect(100, 42, 250, 32), ShowTeamWindow);
            TeamText = teamButton.GetComponentInChildren<Text>();

            Utils.AddLabel("Source From:", new Rect(400, 12, 250, 32));

            var sourceButton = Utils.AddButton("SOURCE", new Rect(400, 42, 250, 32), ShowMultiTeamWindow);
            SourceText = sourceButton.GetComponentInChildren<Text>();
            UpdateTeamText();

            Utils.AddLabel("Adjust HR Team Size:", new Rect(10, 80, 250, 32));
            Utils.AddToggle("Code", new Rect(10, 110, 250, 32),
                            (SIncLibBehaviour.Instance.AdjustHR & SIncLibBehaviour.AdjustHRFlags.Code) != 0, (state) =>
                            {
                                if (state)
                                    SIncLibBehaviour.Instance.AdjustHR |= SIncLibBehaviour.AdjustHRFlags.Code;
                                else
                                {
                                    SIncLibBehaviour.Instance.AdjustHR &= ~SIncLibBehaviour.AdjustHRFlags.Code;
                                }
                            });
            Utils.AddToggle("Art", new Rect(100, 110, 250, 32),
                            (SIncLibBehaviour.Instance.AdjustHR & SIncLibBehaviour.AdjustHRFlags.Art) != 0, (state) =>
                            {
                                if (state)
                                    SIncLibBehaviour.Instance.AdjustHR |= SIncLibBehaviour.AdjustHRFlags.Art;
                                else
                                {
                                    SIncLibBehaviour.Instance.AdjustHR &= ~SIncLibBehaviour.AdjustHRFlags.Art;
                                }
                            });
            Utils.AddToggle("Design", new Rect(200, 110, 250, 32),
                            (SIncLibBehaviour.Instance.AdjustHR & SIncLibBehaviour.AdjustHRFlags.Design) != 0,
                            (state) =>
                            {
                                if (state)
                                    SIncLibBehaviour.Instance.AdjustHR |= SIncLibBehaviour.AdjustHRFlags.Design;
                                else
                                {
                                    SIncLibBehaviour.Instance.AdjustHR &= ~SIncLibBehaviour.AdjustHRFlags.Design;
                                }
                            });

            Utils.AddToggle("Transfer Idle Only", new Rect(10, 140, 250, 32),
                            SIncLibBehaviour.Instance.IdleOnly,
                            (state) => { SIncLibBehaviour.Instance.IdleOnly = state; });

            Utils.AddButton("Optimise Team", new Rect(210, 160, 250, 32), () => OptimiseTeam());
        }

        private static void OptimiseTeam()
        {
            if (TeamText.text.Equals("SELECT TEAM"))
            {
                return;
            }

            if (!GameSettings.Instance.sActorManager.Teams.ContainsKey(TeamText.text))
            {
                HUD.Instance.AddPopupMessage("Could not find team with name " + TeamText.text, "Exclamation",
                                             PopupManager.PopUpAction.None, 0U, PopupManager.NotificationSound.Issue,
                                             2f, PopupManager.PopupIDs.None, 1);
                return;
            }

            if (!DevTeams.Any())
            {
                HUD.Instance
                   .AddPopupMessage("You need to select at least one team to pull team members from excluding the target team",
                                    "Exclamation", PopupManager.PopUpAction.None, 0U,
                                    PopupManager.NotificationSound.Issue, 2f, PopupManager.PopupIDs.None, 1);
                return;
            }

            var    team  = GameSettings.Instance.sActorManager.Teams[TeamText.text];
            string error = "";
            if (!SIncLibBehaviour.Instance.TransferBestAvailableStaff(team, DevTeams.Select(t=>GameSettings.Instance.sActorManager.Teams[t]).ToArray(), out error))
            {
                HUD.Instance.AddPopupMessage("Could not optimise team: " + error, "Exclamation",
                                             PopupManager.PopUpAction.None, 0U, PopupManager.NotificationSound.Issue,
                                             2f, PopupManager.PopupIDs.None, 1);
                return;
            }
            else
            {
                HUD.Instance.AddPopupMessage("Team optimised!", "Cogs", PopupManager.PopUpAction.None,
                                             0, PopupManager.NotificationSound.Neutral, Color.black, 0f,
                                             PopupManager.PopupIDs.None);
                return;
            }
        }

        private static void ShowTeamWindow()
        {
            HUD.Instance.TeamSelectWindow.Show(true, TeamText.text, SetTeamName);
        }

        private static void SetTeamName(string[] t)
        {
            TeamText.text = t.First();
        }

        private static void ShowMultiTeamWindow()
        {
            HUD.Instance.TeamSelectWindow.Show(false, DevTeams, (Action<string[]>) (t =>
            {
                DevTeams.Clear();
                DevTeams.AddRange<string>((IList<string>) t);
                UpdateTeamText();
            }), (string) null);
        }

        public static void UpdateTeamText()
        {
            DevTeams = DevTeams
                       .Where<string>((Func<string, bool>) (x =>
                                          GameSettings.Instance.sActorManager.Teams.ContainsKey(x)))
                       .ToHashSet<string>();
            SourceText.text = DevTeams.GetListAbbrev<string>("Team", (Func<string, string>) null);
        }
    }
}