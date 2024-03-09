using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Console = DevConsole.Console;

namespace SIncLib
{
    internal class PortingBehaviour : ModBehaviour
    {
        public static PortingBehaviour Instance;

        public List<PortingJob> PortingJobQueue = new List<PortingJob>();


        public HashSet<string> PortingTeams = new HashSet<string>();
        [System.NonSerialized]
        public SimulatedCompany OutsourcedPorting = null;
        public int SupportForMonths = 12;
        public int MinimumUserbase = 1000;
        public int ConcurrentJobs = 1;
        public bool Enabled = false;
        public bool AlwaysPortToInhouseOS;

        private void Awake()
        {
            Instance = this;

            // load from player prefs
            string teamsString = PlayerPrefs.GetString("PortingTeams", "");
            if (!string.IsNullOrEmpty(teamsString))
            {
                PortingTeams = new HashSet<string>(teamsString.Split(','));
            }
            var outsourcedPortingId = PlayerPrefs.GetInt("OutsourcedPorting", -1);
            if (outsourcedPortingId >= 0)
            {
                try
                {
                    var company = GameSettings.Instance.simulation.Companies.First(x => x.Key == outsourcedPortingId);
                    OutsourcedPorting = company.Value;
                }
                catch (Exception e)
                {
                    Console.LogError("Failed to load outsourced porting company " + outsourcedPortingId + " " + e.Message);
                }

            }
            SupportForMonths = PlayerPrefs.GetInt("SupportForMonths", 12);
            MinimumUserbase = PlayerPrefs.GetInt("MinimumUserbase", 1000);
            ConcurrentJobs = PlayerPrefs.GetInt("ConcurrentJobs", 1);
            Enabled = PlayerPrefs.GetInt("PortingEnabled", 0) == 1;
            AlwaysPortToInhouseOS = PlayerPrefs.GetInt("AlwaysPortToInhouseOS", 0) == 1;
        }

        private void Start()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }
            TimeOfDayOnOnDayPassed(null, null);
            TimeOfDay.OnDayPassed += TimeOfDayOnOnDayPassed;

            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        // Recompute the porting job queue when the game is loaded
        private void OnLevelFinishedLoading(Scene arg0, LoadSceneMode arg1)
        {
            TimeOfDayOnOnDayPassed(null, null);
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



        public void TimeOfDayOnOnDayPassed(object sender, EventArgs e)
        {
            if (!Enabled || !isActiveAndEnabled || GameSettings.Instance == null || GameSettings.Instance.MyCompany == null)
            {
                return;
            }

            PortingJobQueue.Clear();

            // get all software in date range
            List<SoftwareProduct> products = GameSettings.Instance.MyCompany.Products;
            // get current date
            var currentDate = TimeOfDay.Instance.GetDate();
            // subtract the number of months from the current date
            var cutoffDate = currentDate - SupportForMonths;
            // filter list and remove anything that can't be ported
            products = products.Where(x => x.GetReleaseDate() > cutoffDate && x.Type.OSSpecific && !PublisherDeal.HasDeal(x, "OSExclusivity")).OrderByDescending(x => x.Userbase).ToList();

            // add existing porting jobs to the job queue
            foreach (Team team in PortingTeams.SelectNotNull(new Func<string, Team>(GameSettings.GetTeam)))
            {
                foreach (var workItem in team.WorkItems.OfType<SoftwarePort>())
                {
                    PortingJobQueue.Add(new PortingJob()
                    {
                        Product = workItem.Product,
                        TargetProduct = workItem.OSs.FirstOrDefault().Product,
                        Team = team,
                        WorkItem = workItem,
                        IsPaused = false
                    });
                }
            }

            foreach (var product in products)
            {
                var supportedOSs = FilterAvailableTargets(product).OrderByDescending(p => p.Userbase);

                foreach (var os in supportedOSs)
                {
                    // console.log all of the OSs that are available to port to
                    //Console.Log("Adding " + product.Name + " to " + os.Name);

                    // if not already in the queue with same product and same target
                    if (PortingJobQueue.Any(x => x.Product == product && x.TargetProduct == os))
                    {
                        continue;
                    }

                    // If we already have a porting job in progress then link it
                    var portingJob = GameSettings.Instance.MyCompany
                            .WorkItems.OfType<SoftwarePort>().FirstOrDefault(workItem =>
                            {
                                var portItem = workItem as SoftwarePort;
                                if (portItem == null || portItem.OSs == null || portItem.OSs.Count == 0)
                                {
                                    return false;
                                }
                                //Console.Log("Checking " + portItem.OSs.First().Product.Name + " is " + os.Name);
                                return portItem.OSs.First().Product == os && product == portItem.Product;
                            });
                    if (portingJob != null)
                    {
                        portingJob.FixReferences();
                    }
                    //Console.Log("Porting job is " + (portingJob != null ? "not null" : "null"));
                    PortingJobQueue.Add(new PortingJob()
                    {
                        Product = product,
                        TargetProduct = os,
                        Team = (portingJob != null && portingJob.GetDevTeams() != null && portingJob.GetDevTeams().Count > 0) ? portingJob.GetDevTeams().FirstOrDefault() : null,
                        WorkItem = portingJob,
                        IsPaused = true
                    });
                    //Console.Log("Added " + product.Name + " to " + os.Name);
                }
            }


        }

        private List<SoftwareProduct> FilterAvailableTargets(SoftwareProduct p)
        {
            List<SoftwareProduct> allProducts = GameSettings.Instance.simulation.GetProductsWithMock(true).ToList<SoftwareProduct>();

            var companyFilter = p.Publishing != null ? p.Publishing.Publisher.ID : 0U;
            var catFilter = p.Type.HasOSLimits() ? p.Type.GetOSLimits().ToHashSet() : null;
            var ignoreCat = catFilter == null;
            //Console.Log("Filtering for " + p.Name + " company " + companyFilter + ((!ignoreCat) ? " categories " + catFilter.GetListAbbrev("Category") : ""));
            return allProducts.Where(targetProd =>
            {
                if (!targetProd.Type.Name.Equals("Operating System"))
                {
                    return false;
                }

                if (companyFilter > 0U && (int)targetProd.DevCompany.ID != (int)companyFilter)
                {
                    //Console.Log("Filtering out " + targetProd.Name + " company " + targetProd.DevCompany.Name);
                    return false;
                }

                if (!ignoreCat && !catFilter.Contains(targetProd.Category.Name))
                {
                    // Console.Log("Filtering out " + targetProd.Name + " category " + targetProd.Category.Name);
                    return false;
                }
                if (p.Features != null && !SoftwareType.OSDependenciesMet(targetProd, p.Features))
                {
                    //Console.Log(p.Name + " requires " + p.Features.GetListAbbrev("Feature") + " but " + targetProd.Name + " does not have them");
                    return false;
                }

                if (p.OSs.Contains(targetProd) || !SoftwareType.OSDependenciesMet(targetProd, p.Features))
                {
                    //Console.Log("2 Filtering out " + targetProd.Name);
                    return false;
                }
                /*SoftwarePort softwarePort = GameSettings.Instance.MyCompany.WorkItems.OfType<SoftwarePort>().FirstOrDefault((z => z.Product == p));

                if (softwarePort != null && softwarePort.OSs.Any((z => z.Product == targetProd || z.Product.MockSucceeded == targetProd)))
                {
                    //Console.Log("3 Filtering out " + targetProd.Name);
                    return false;
                }*/

                // has this os got enough active users?
                if (targetProd.Userbase < MinimumUserbase)
                {
                    // If we are always porting to inhouse OS, then we don't care about userbase
                    if (!Instance.AlwaysPortToInhouseOS)
                    {
                        //Console.Log("Filtering out " + targetProd.Name + " user base " + targetProd.Userbase);
                        return false;
                    }
                }

                // if we are porting to one of our OSs then we only want OSs that are from the last 3 years
                // get current date
                var currentDate = TimeOfDay.Instance.GetDate();
                // subtract the 36 months from the current date
                var cutoffDate = currentDate - 36;
                if (Instance.AlwaysPortToInhouseOS && !targetProd.IsMock && targetProd.GetReleaseDate() < cutoffDate)
                {
                    return false;
                }

                //Console.Log("Keeping " + targetProd.Name + " is mock? " + targetProd.IsMock);
                return true;
            }).ToList();
        }

        public void Update()
        {
            if (!SIncLibMod.ModActive || !isActiveAndEnabled)
            {
                return;
            }

            // check if any jobs are complete
            foreach (var job in PortingJobQueue.Where(x => x.WorkItem != null &&
                (x.WorkItem.GetActualProgress() >= 1 || x.WorkItem.GetCurrentStage().Equals("MockOSPortWait".Loc()))))
            {
                job.WorkItem.Hidden = true;
                if (job.WorkItem.GetDevTeams().Count > 0 && job.Team != null)
                {
                    job.WorkItem.RemoveDevTeam(job.Team);
                }
                // remove the job from the queue unless its waiting for the OS to be released
                if (!job.WorkItem.GetCurrentStage().Equals("MockOSPortWait".Loc()))
                {
                    PortingJobQueue.Remove(job);
                }

            }

            // check if any of the porting teams have less than ConcurrentJobs porting jobs running
            foreach (var team in PortingTeams.SelectNotNull(new Func<string, Team>(GameSettings.GetTeam)))
            {
                if (team.WorkItems.OfType<SoftwarePort>().Count() < ConcurrentJobs)
                {
                    // get the first job in the queue and start a porting job
                    var nextJob = PortingJobQueue.FirstOrDefault(job => job.WorkItem == null);
                    if (nextJob != null)
                    {
                        StartPortingJob(team, nextJob);
                    }
                }
            }
        }

        private void StartPortingJob(Team team, PortingJob nextJob)
        {
            // create a new porting job
            var portingJob = new SoftwarePort(nextJob.Product, new SoftwareProduct[] { nextJob.TargetProduct });
            if (OutsourcedPorting != null)
            {
                portingJob.CompanyWorker = OutsourcedPorting;
            }
            else
            {
                // add the job to the team
                portingJob.AddDevTeam(team);
            }
            nextJob.IsPaused = false;
            nextJob.WorkItem = portingJob;
            nextJob.Team = team;
            portingJob.FixReferences();
            GameSettings.Instance.MyCompany.WorkItems.Add(portingJob);

            return;

        }

    }
}
