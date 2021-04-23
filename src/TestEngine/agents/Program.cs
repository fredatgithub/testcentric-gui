// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using TestCentric.Common;
using TestCentric.Engine.Internal;
using TestCentric.Engine.Services;
using TestCentric.Engine.Helpers;
using NUnit.Engine;

namespace TestCentric.Engine.Agents
{
    public class TestCentricAgent
    {
        static Guid AgentId;
        static string AgencyUrl;
        static Process AgencyProcess;
        static RemoteTestAgent Agent;
        private static Logger log;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            AgentId = new Guid(args[0]);
            AgencyUrl = args[1];

            var traceLevel = InternalTraceLevel.Off;
            var pid = Process.GetCurrentProcess().Id;
            var debugArgPassed = false;
            var workDirectory = string.Empty;
            var agencyPid = string.Empty;

            for (int i = 2; i < args.Length; i++)
            {
                string arg = args[i];

                // NOTE: we can test these strings exactly since
                // they originate from the engine itself.
                if (arg == "--debug-agent")
                {
                    debugArgPassed = true;
                }
                else if (arg.StartsWith("--trace:"))
                {
                    traceLevel = (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), arg.Substring(8));
                }
                else if (arg.StartsWith("--pid="))
                {
                    agencyPid = arg.Substring(6);
                }
                else if (arg.StartsWith("--work="))
                {
                    workDirectory = arg.Substring(7);
                }
            }

            var logName = $"testcentric-agent_{pid}.log";
            InternalTrace.Initialize(Path.Combine(workDirectory, logName), traceLevel);
            log = InternalTrace.GetLogger(typeof(TestCentricAgent));

            if (debugArgPassed)
                TryLaunchDebugger();

            LocateAgencyProcess(agencyPid);

            log.Info("Agent process {0} starting", pid);

            // TODO: CurrentFramework throws under .NET 5.0
#if NET5_0
            log.Info("Running under .NET 5.0");
#else
            log.Info("Running under version {0}, {1}",
                Environment.Version,
                RuntimeFramework.CurrentFramework.DisplayName);
#endif

            // Create CoreEngine
            var engine = new CoreEngine
            {
                WorkDirectory = workDirectory,
                InternalTraceLevel = traceLevel
            };

            // Custom Service Initialization
            engine.Services.Add(new ExtensionService());

            // Initialize Services
            log.Info("Initializing Services");
            engine.InitializeServices();

            log.Info("Starting RemoteTestAgent");
            Agent = new RemoteTestAgent(AgentId);
            Agent.Transport =
#if NETFRAMEWORK
                new TestCentric.Engine.Communication.Transports.Remoting.TestAgentRemotingTransport(Agent, AgencyUrl);
#else
                new TestCentric.Engine.Communication.Transports.Tcp.TestAgentTcpTransport(Agent, AgencyUrl);
#endif
            try
            {
                if (Agent.Start())
                    WaitForStop();
                else
                {
                    log.Error("Failed to start RemoteTestAgent");
                    Environment.Exit(AgentExitCodes.FAILED_TO_START_REMOTE_AGENT);
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in RemoteTestAgent. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                Environment.Exit(AgentExitCodes.UNEXPECTED_EXCEPTION);
            }
            log.Info("Agent process {0} exiting cleanly", pid);

            Environment.Exit(AgentExitCodes.OK);
        }

        private static void LocateAgencyProcess(string agencyPid)
        {
            var agencyProcessId = int.Parse(agencyPid);
            try
            {
                AgencyProcess = Process.GetProcessById(agencyProcessId);
            }
            catch (Exception e)
            {
                log.Error($"Unable to connect to agency process with PID: {agencyProcessId}");
                log.Error($"Failed with exception: {e.Message} {e.StackTrace}");
                Environment.Exit(AgentExitCodes.UNABLE_TO_LOCATE_AGENCY);
            }
        }

        private static void WaitForStop()
        {
            log.Debug("Waiting for stopSignal");

            while (!Agent.WaitForStop(500))
            {
                if (AgencyProcess.HasExited)
                {
                    log.Error("Parent process has been terminated.");
                    Environment.Exit(AgentExitCodes.PARENT_PROCESS_TERMINATED);
                }
            }

            log.Debug("Stop signal received");
        }

        private static void TryLaunchDebugger()
        {
            if (Debugger.IsAttached)
                return;

            try
            {
                Debugger.Launch();
            }
            catch (SecurityException se)
            {
                if (InternalTrace.Initialized)
                {
                    log.Error($"System.Security.Permissions.UIPermission is not set to start the debugger. {se} {se.StackTrace}");
                }
                Environment.Exit(AgentExitCodes.DEBUGGER_SECURITY_VIOLATION);
            }
            catch (NotImplementedException nie) //Debugger is not implemented on mono
            {
                if (InternalTrace.Initialized)
                {
                    log.Error($"Debugger is not available on all platforms. {nie} {nie.StackTrace}");
                }
                Environment.Exit(AgentExitCodes.DEBUGGER_NOT_IMPLEMENTED);
            }
        }
    }
}
