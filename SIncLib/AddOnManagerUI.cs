using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SIncLib
{
    internal static class AddOnManagerUI
    {

        private static Text DesignTeamText;
        private static Text DevTeamText;
        private static GUICombobox SCM;

        private static void UpdateSCMCombo()
        {
            SCM.UpdateContent( (new ServerGroup[ 1 ]).Concat( GameSettings.Instance.GetAllServerGroups() ) );
        }

        private static string GetSCM()
        {
            if (SCM.Selected < 1 || SCM.GetSelected<ServerGroup>() == null)
                return null;
            return SCM.GetSelected<ServerGroup>().Name;
        }

        internal static void Show(ref RectTransform Panel)
        {
            if (Panel != null)
            {
                UnityEngine.Object.Destroy( Panel.gameObject );
            }

            Panel = Utils.AddPanel( new Rect( 270, 10, 250, -1 ) );

            Utils.AddToggle( "Automate Create Addons",
                new Rect( 10, 10, 600, 32 ),
                AddOnBehaviour.Instance.Enabled,
                state =>
                {
                    AddOnBehaviour.Instance.Enabled = state;
                    PlayerPrefs.SetInt( "AddonEnabled", state ? 1 : 0 );
                    PlayerPrefs.Save();
                }, Panel.gameObject );

            Utils.AddLabel( "Design Teams: ", new Rect( 10, 50, 250, 32 ), Panel.gameObject );
            Button teamButton = Utils.AddButton( "SELECT TEAM", new Rect( 160, 40, 250, 32 ), () => ShowTeamWindow( true ), Panel.gameObject );
            DesignTeamText = teamButton.GetComponentInChildren<Text>();
            UpdateTeamText( true );

            Utils.AddLabel( "Dev Teams: ", new Rect( 10, 90, 250, 32 ), Panel.gameObject );
            teamButton = Utils.AddButton( "SELECT TEAM", new Rect( 160, 80, 250, 32 ), () => ShowTeamWindow( false ), Panel.gameObject );
            DevTeamText = teamButton.GetComponentInChildren<Text>();
            UpdateTeamText( false );

            Utils.AddLabel( "Support products for (months): ", new Rect( 10, 130, 250, 32 ), Panel.gameObject );
            Utils.AddIntField( AddOnBehaviour.Instance.SupportForMonths, new Rect( 270, 120, 140, 32 ), i =>
            {
                AddOnBehaviour.Instance.SupportForMonths = i;
                PlayerPrefs.SetInt( "Addon_SupportForMonths", i );
                PlayerPrefs.Save();
            }, Panel.gameObject );

            Utils.AddLabel( "Minimum Active Users: ", new Rect( 10, 170, 250, 32 ), Panel.gameObject );
            Utils.AddIntField( AddOnBehaviour.Instance.MinimumUsers, new Rect( 270, 160, 140, 32 ), i =>
            {
                AddOnBehaviour.Instance.MinimumUsers = i;
                PlayerPrefs.SetInt( "Addon_MinimumUsers", i );
                PlayerPrefs.Save();
            }, Panel.gameObject );

            Utils.AddLabel( "Parallel Jobs: ", new Rect( 10, 210, 250, 32 ), Panel.gameObject );
            Utils.AddIntField( AddOnBehaviour.Instance.ConcurrentJobs, new Rect( 270, 200, 140, 32 ), i =>
            {
                AddOnBehaviour.Instance.ConcurrentJobs = i;
                PlayerPrefs.SetInt( "ConcurrentJobs", i );
                PlayerPrefs.Save();
            }, Panel.gameObject );

            Utils.AddLabel( "SCM: ", new Rect( 10, 250, 250, 32 ), Panel.gameObject );
            SCM = Utils.AddComboBox( "SCM", new Rect( 160, 240, 250, 32 ), () =>
            {
                AddOnBehaviour.Instance.SCM = GetSCM();
            }, Panel.gameObject );
            UpdateSCMCombo();
            ServerGroup server;
            if (!GameSettings.GetPrefServer( "AddonDesign", out server ))
                return;
            SCM.SelectedItem = server;
        }

        private static void ShowTeamWindow(bool isDesign)
        {
            HUD.Instance.TeamSelectWindow.Show( false, isDesign ? AddOnBehaviour.Instance.AddonDesignTeams : AddOnBehaviour.Instance.AddonDevTeams,
                ts =>
                {
                    if (isDesign)
                        AddOnBehaviour.Instance.AddonDesignTeams.Clear();
                    else
                        AddOnBehaviour.Instance.AddonDevTeams.Clear();


                    string teamsString = string.Join( ",", ts );
                    foreach (var team in ts)
                    {
                        if (isDesign)
                            AddOnBehaviour.Instance.AddonDesignTeams.Add( team );
                        else
                            AddOnBehaviour.Instance.AddonDevTeams.Add( team );
                        ;
                    }
                    // convert to string and save to player prefs
                    PlayerPrefs.SetString( isDesign ? "AddonDesignTeams" : "AddonDevTeams", teamsString );
                    PlayerPrefs.Save();

                    UpdateTeamText( isDesign );
                }, null );
        }

        private static void UpdateTeamText(bool isDesign)
        {
            if (isDesign)
            {
                AddOnBehaviour.Instance.AddonDesignTeams = AddOnBehaviour.Instance.AddonDesignTeams
                .Where( x =>
                    GameSettings.Instance.sActorManager.Teams.ContainsKey( x ) )
                .ToHashSet();
                DesignTeamText.text = AddOnBehaviour.Instance.AddonDesignTeams.GetListAbbrev( "Team" );
            }
            else
            {
                AddOnBehaviour.Instance.AddonDevTeams = AddOnBehaviour.Instance.AddonDevTeams
                    .Where( x =>
                                       GameSettings.Instance.sActorManager.Teams.ContainsKey( x ) )
                    .ToHashSet();
                DevTeamText.text = AddOnBehaviour.Instance.AddonDevTeams.GetListAbbrev( "Team" );
            }

        }

    }
}
