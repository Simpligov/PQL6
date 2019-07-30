﻿using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;
using Pql.Engine.Interfaces;
using StructureMap;

namespace Pql.IntegrationStubs
{
    public class ProgramBase
    {
        protected static ITracer Tracer;
        private static Container s_objectFactory;

        protected static int MainImpl(string[] args, params Assembly[] assemblies)
        {
            InitializeLogger();
            InitializeUnobservedExceptionHandler();
            InitializeContainer(assemblies);

            var arguments = s_objectFactory.GetInstance<IHostedProcessArgs>();
            if (arguments.IsInteractive)
            {
                if (arguments.Help)
                {
                    Tracer.Info("Arguments: anything of installutil.exe and also /serviceDesc, /serviceDisplayName, /serviceName, /serviceStartMode, /serviceAccountType, /serviceAccountPassword, /serviceAccountName");
                    return 0;
                }

                if (arguments.Install)
                {
                    if (arguments.Uninstall)
                    {
                        throw new ArgumentException("Install and uninstall commands cannot be combined");
                    }
                    SelfInstaller.InstallMe();
                }
                else if (arguments.Uninstall)
                {
                    SelfInstaller.UninstallMe();
                }
                else
                {
                    s_objectFactory.GetInstance<WindowsHostService>().RunInteractive(args);
                }
            }
            else
            {
                ServiceBase.Run(s_objectFactory.GetInstance<WindowsHostService>());
            }

            return 0;
        }

        private static void InitializeContainer(params Assembly[] assemblies)
        {
            s_objectFactory = new Container(
                x => x.Scan(
                    scanner =>
                        {
                            //x.PullConfigurationFromAppConfig = true;
                            
                            scanner.With(new HostRegistry.SingletonConvention<IHostedProcess>());

                            scanner.TheCallingAssembly();

                            foreach (var ass in assemblies)
                            {
                                scanner.Assembly(ass);
                            }
                            //scanner.AssembliesFromApplicationBaseDirectory(
                            //    assembly =>
                            //    assembly.FullName.StartsWith("mycompany.", StringComparison.InvariantCultureIgnoreCase));

                            //if (Directory.Exists("Processes"))
                            //{
                            //    scanner.AssembliesFromPath(
                            //        "Processes",
                            //        assembly =>
                            //        assembly.FullName.StartsWith("mycompany.", StringComparison.InvariantCultureIgnoreCase));
                            //}

                            scanner.LookForRegistries();

                            scanner.AddAllTypesOf<IHostedProcessArgs>();
                        }));
        }

        protected static void InitializeLogger()
        {
            //var applicationName = ConfigurationManager.AppSettings["ApplicationName"];

            //if (String.IsNullOrWhiteSpace(applicationName))
            //{
            //    applicationName = "mycompany.WindowsHost";
            //}

            //var loggerConfigPath = ConfigurationManager.AppSettings["LoggerPath"];

            //if (!String.IsNullOrWhiteSpace(loggerConfigPath) && File.Exists(loggerConfigPath))
            //{
            //    LoggerFactory.Initialize(applicationName, loggerConfigPath);
            //}

            //Logger = LoggerFactory.GetLogger(typeof(ProgramBase));
            Tracer = new DummyTracer();
        }

        protected static void InitializeUnobservedExceptionHandler()
        {
            // if there are any unhandled exceptions in tasks - just don't throw them in finalizer
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                if (!e.Observed)
                {
                    e.SetObserved();
                    
                    if (!Environment.HasShutdownStarted)
                    {
                        Tracer.Fatal("Unobserved exception in a task!", e.Exception);
                    }
                }
            };
        }
    }
}
