using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SIncLib
{
    internal class AddOnBehaviour : ModBehaviour
    {
        public static AddOnBehaviour Instance;

        public HashSet<string> AddonDesignTeams = new HashSet<string>();
        public HashSet<string> AddonDevTeams = new HashSet<string>();

        public int SupportForMonths = 12;
        public int MinimumUsers = 10000;
        public int ConcurrentJobs = 1;
        public bool Enabled = false;
        public string SCM = null;

        private List<SoftwareProduct> ValidProducts = new List<SoftwareProduct>();

        private void Awake()
        {
            Instance = this;

            // load from player prefs
            string teamsString = PlayerPrefs.GetString( "AddonDesignTeams", "" );
            if (!string.IsNullOrEmpty( teamsString ))
            {
                AddonDesignTeams = new HashSet<string>( teamsString.Split( ',' ) );
            }
            teamsString = PlayerPrefs.GetString( "AddonDevTeams", "" );
            if (!string.IsNullOrEmpty( teamsString ))
            {
                AddonDevTeams = new HashSet<string>( teamsString.Split( ',' ) );
            }

            SupportForMonths = PlayerPrefs.GetInt( "Addon_SupportForMonths", 12 );
            Enabled = PlayerPrefs.GetInt( "AddonEnabled", 0 ) == 1;
            ConcurrentJobs = PlayerPrefs.GetInt( "ConcurrentJobs", 1 );
        }

        private void Start()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }
            TimeOfDayOnOnDayPassed( null, null );
            TimeOfDay.OnDayPassed += TimeOfDayOnOnDayPassed;

            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        // Recompute the addon job queue when the game is loaded
        private void OnLevelFinishedLoading(Scene arg0, LoadSceneMode arg1)
        {
            TimeOfDayOnOnDayPassed( null, null );
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;

            TimeOfDay.OnDayPassed -= TimeOfDayOnOnDayPassed;
        }

        public override void OnActivate()
        {
        }

        public override void OnDeactivate()
        {
        }

        /**
         * Generate a list of products within the date range that can have addons and order by active users
         */
        public void TimeOfDayOnOnDayPassed(object sender, EventArgs e)
        {

            if (!Enabled || !isActiveAndEnabled || GameSettings.Instance == null || GameSettings.Instance.MyCompany == null)
            {
                return;
            }
            if (AddonDesignTeams == null || AddonDesignTeams.Count == 0 || AddonDevTeams == null || AddonDevTeams.Count == 0)
            {
                DevConsole.Console.Log( "No design or dev teams assigned to add on job" );
                return;
            }

            ValidProducts.Clear();

            // get all software in date range
            List<SoftwareProduct> products = GameSettings.Instance.MyCompany.Products;
            // get current date
            var currentDate = TimeOfDay.Instance.GetDate();
            // subtract the number of months from the current date
            var cutoffDate = currentDate - SupportForMonths;
            // filter list and order by active users
            products = products.Where( x => x.GetReleaseDate() > cutoffDate && x.Userbase >= MinimumUsers ).OrderByDescending( x => x.Userbase ).ToList();

            // filter out any product that can't have addons
            foreach (var product in products)
            {
                // if any AddonDesignTeam or AddonDevTeam is already working on an addon for this product, skip it
                if (AddonDesignTeams.Any( x => GameSettings.GetTeam( x ).WorkItems.OfType<SoftwareAddOn>().Any( y =>
                {
                    DevConsole.Console.Log( "y = " + y.Name + " y.Parent = " + y.Parent.Name );
                    return y.Parent == product.Type;
                } ) ) ||
                    AddonDevTeams.Any( x => GameSettings.GetTeam( x ).WorkItems.OfType<SoftwareAddOn>().Any( y => y.Parent == product.Type ) ))
                {
                    continue;
                }

                List<SoftwareAddOn> list = product.Type.GetValidAddons( product.Category, product.TechLevels, product.Features, SDateTime.Now() ).ToList();
                if (list.Count > 0)
                {
                    ValidProducts.Add( product );
                }
            }
        }

        public void Update()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }

            AssignNewJobs();

            // Check if any of the tasks assigned to the design team are complete and need to progress to the dev team
            foreach (var team in AddonDesignTeams.SelectNotNull( new Func<string, Team>( GameSettings.GetTeam ) ))
            {
                foreach (var job in team.WorkItems.OfType<DesignDocument>())
                {
                    if (job.GetActualProgress() >= 1f)
                    {
                        // Check if the design team is competent
                        var devTeam = GetFirstAvailableDevTeam();
                        job.AddDevTeam( devTeam );
                        job.CheckCompetency();
                        {
                            // Assign the job to the dev team

                            GameSettings.Instance.MyCompany.AddWorkItem( job );
                            team.WorkItems.Remove( job );
                        }
                    }
                }
            }
            // NOTE TO FUTURE SELF: Code to generate design document is done but untested. Design team is assigned
            // Need to monitor design completing and promote to dev team and check competency. Need to assign marketing team to do release date 
            // and press builds. Need to make sure enough copies of product are ordered before release

        }

        private void AssignNewJobs()
        {
            // If there is an available team
            var team = GetFirstAvailableDesignTeam();
            if (team != null)
            {
                // Get first product in the list and remove it
                if (ValidProducts.Count > 0)
                {
                    SoftwareProduct product = ValidProducts[ 0 ];
                    ValidProducts.RemoveAt( 0 );

                    // Get a list of valid addons for the product
                    List<SoftwareAddOn> list = product.Type.GetValidAddons( product.Category, product.TechLevels, product.Features, SDateTime.Now() ).ToList();
                    if (list.Count > 0)
                    {
                        // Get a random addon from the list
                        var addonType = list[ MarketSimulation.Active.Random.Next( list.Count ) ];
                        // Create a new addon prototype
                        DesignDocument addonPrototype = CreateAddonPrototype( addonType, product, SDateTime.Now() );
                        if (addonPrototype != null)
                        {
                            addonPrototype.AddDevTeams( new List<string>() { team.Name } );
                            addonPrototype.CheckCompetency();
                            GameSettings.Instance.MyCompany.AddWorkItem( addonPrototype );

                            // Add the addon prototype to the design team
                            team.WorkItems.Add( addonPrototype );
                        }
                    }
                }
            }
        }

        private Team GetFirstAvailableDesignTeam()
        {
            // check if any of the design teams have less than ConcurrentJobs porting jobs running
            foreach (var team in AddonDesignTeams.SelectNotNull( new Func<string, Team>( GameSettings.GetTeam ) ))
            {
                if (team.WorkItems.OfType<DesignDocument>().Count() < ConcurrentJobs)
                {
                    return team;
                }
            }
            return null;
        }

        private Team GetFirstAvailableDevTeam()
        {
            // check if any of the design teams have less than ConcurrentJobs porting jobs running
            foreach (var team in AddonDevTeams.SelectNotNull( new Func<string, Team>( GameSettings.GetTeam ) ))
            {
                if (team.WorkItems.OfType<SoftwareAlpha>().Count() < ConcurrentJobs)
                {
                    return team;
                }
            }
            return null;
        }

        public Dictionary<string, SoftwareProduct> ChooseNeeds(
            SoftwareType type,
            string category,
            SDateTime time)
        {
            Dictionary<string, SoftwareProduct> dictionary = new Dictionary<string, SoftwareProduct>();
            Dictionary<string, List<string>> needsWithSpecs = type.GetNeedsWithSpecs( category );
            if (needsWithSpecs.Count > 0)
            {
                List<SoftwareProduct> list = GameSettings.Instance.simulation.GetAllProducts( false ).ToList();
                foreach (KeyValuePair<string, List<string>> keyValuePair in needsWithSpecs)
                {
                    float num1 = float.MinValue;
                    SoftwareProduct softwareProduct1 = null;
                    for (int index1 = 0; index1 < list.Count; ++index1)
                    {
                        SoftwareProduct softwareProduct2 = list[ index1 ];
                        if ((!softwareProduct2.InHouse || softwareProduct2.DevCompany == GameSettings.Instance.MyCompany) && softwareProduct2.Type.Name.Equals( keyValuePair.Key ) && (time - softwareProduct2.Release).Year < 5 && softwareProduct2.FairPrice( time ))
                        {
                            float num2 = (float) softwareProduct2.RelativeFeatureScore( MarketSimulation.Active, time ) + MarketSimulation.Active.Random.NextFloat() * 0.05f;
                            for (int index2 = 0; index2 < keyValuePair.Value.Count; ++index2)
                                num2 += softwareProduct2.TechLevels.GetOrDefault( keyValuePair.Value[ index2 ], z => z.ActualYear );
                            if ((double) num2 > (double) num1 && (softwareProduct1 == null || !softwareProduct2.DevCompany.Player || (double) MarketSimulation.Active.Random.NextFloat() < (double) softwareProduct2.DevCompany.BusinessReputation))
                            {
                                num1 = num2;
                                softwareProduct1 = softwareProduct2;
                            }
                        }
                    }
                    if (softwareProduct1 != null)
                        dictionary.Add( keyValuePair.Key, softwareProduct1 );
                }
            }
            return dictionary;
        }

        private DesignDocument CreateAddonPrototype(
            SoftwareAddOn type,
            SoftwareProduct parent,
            SDateTime time)
        {
            System.Random random = MarketSimulation.Active.Random;
            List<AddOnFeature> addOnFeatureList = new List<AddOnFeature>();
            List<uint> factors = new List<uint>();
            Dictionary<string, SoftwareProduct> tools = this.ChooseNeeds( parent.Type, parent.Category.Name, time );
            type.GenerateFeatures( parent.Features, parent.TechLevels, parent.Submarkets, parent.Category, random, addOnFeatureList, factors );
            HashSet<string> specs = addOnFeatureList.Select( x => x.Spec ).Distinct().ToHashSet();
            tools = Enumerable.OfType<SpecFeature>( parent.Features ).Where( x => specs.Contains( x.Spec ) ).SelectMany( x => x.Dependencies ).Distinct().ToDictionary( x => x, x => tools.GetOrDefault( x ) );
            if (!tools.Values.None( x => x == null ))
                return null;
            float devTime = type.DevTime( addOnFeatureList, factors, parent.Category, GameSettings.Instance.MyCompany, parent.TechLevels );
            float artRatio = SoftwareType.CodeArtRatio( addOnFeatureList.OfType<FeatureBase>().ToList() );
            int[] optimalEmployeeCount = SoftwareType.GetOptimalEmployeeCount( devTime );
            int num = Mathf.RoundToInt( (float) ((double) GameData.ProjectDevTime( optimalEmployeeCount[ 0 ], optimalEmployeeCount[ 1 ], devTime, artRatio ) * (double) MarketSimulation.Active.Random.Range( 0.9f, 1.1f ) * 1.5) );
            SDateTime releaseDate = time + num;
            float price = GameSettings.Instance.simulation.GetIdealMarketPrice( type ) * (float) type.PerceivedValue( addOnFeatureList, factors, parent.Category, parent.TechLevels );
            //AddonDesignWindow.AddonDesign addonPrototype = new AddonDesignWindow.AddonDesign( MarketSimulation.Active.GenerateAddonName( parent, null, type, false, random ),
            //    type, price, addOnFeatureList.ToArray(), factors.ToArray(), tools, new HashSet<string>( new[] { GetFirstAvailableDesignTeam().Name } ), null, null, SCM );

            DesignDocument design = new DesignDocument( MarketSimulation.Active.GenerateAddonName( parent, null, type, false, random ),
                type, parent.Category, tools, price, SDateTime.Now(), GameSettings.Instance.MyCompany, parent, null, 0.0f, addOnFeatureList.ToArray(), factors.ToArray(), SCM, tools.Values.ToList() );

            return design;
        }
    }
}
