﻿using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using ToolCore.Utils;
using VRage.Game.Components;

namespace ToolCore.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    internal partial class ToolSession : MySessionComponentBase
    {
        internal static int Tick;
        internal int TickMod10;
        internal int TickMod20;
        internal int TickMod60;
        internal int TickMod120;
        internal bool Tick10;
        internal bool Tick20;
        internal bool Tick60;
        internal bool Tick120;
        internal bool Tick600;

        internal bool IsServer;
        internal bool IsDedicated;
        internal bool IsMultiPlayer;

        private bool FirstRun = true;

        public override void LoadData()
        {
            IsServer = MyAPIGateway.Session.IsServer;
            IsMultiPlayer = MyAPIGateway.Multiplayer.MultiplayerActive;
            IsDedicated = MyAPIGateway.Utilities.IsDedicated;

            MyEntities.OnEntityCreate += OnEntityCreate;

            Logs.InitLogs();

            LoadDefinitions();
            LoadToolCoreDefs();
            PostLoad();

            MyAPIGateway.TerminalControls.CustomActionGetter += Controls.CustomActionGetter;
            MyAPIGateway.TerminalControls.CustomControlGetter += Controls.CustomControlGetter;
        }

        public override void BeforeStart()
        {
            if (IsMultiPlayer)
            {
                if (IsServer)
                    MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Networking.ServerPacketId, Networking.ProcessPacket);
                else
                    MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Networking.ClientPacketId, Networking.ProcessPacket);
            }

            if (IsServer)
            {
                Settings.LoadConfig();
                SettingsLoad(Settings.CoreSettings);
            }
            Controls.CreateTerminalControls<IMyConveyorSorter>();

            MyVisualScriptLogicProvider.PlayerDisconnected += PlayerDisconnected;
            MyVisualScriptLogicProvider.PlayerRespawnRequest += PlayerConnected;
        }

        public override void UpdateBeforeSimulation()
        {
            Tick++;
            TickMod10 = Tick % 10;
            TickMod20 = Tick % 20;
            TickMod60 = Tick % 60;
            TickMod120 = Tick % 120;

            Tick10 = Tick % 10 == 0;
            Tick20 = TickMod20 == 0;
            Tick60 = TickMod60 == 0;
            Tick120 = Tick % 120 == 0;
            Tick600 = Tick % 600 == 0;

            try
            {
                CompLoop();
            }
            catch (Exception ex)
            {
                Logs.LogException(ex);
            }

            if (!_startComps.IsEmpty || !_startGrids.IsEmpty)
                StartComps();

            if (FirstRun)
            {
                if (!IsDedicated)
                    InitPlayers();
                FirstRun = false;
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (IsDedicated) return;
            try
            {
                AvLoop();
            }
            catch (Exception ex)
            {
                Logs.LogException(ex);
            }
        }

        protected override void UnloadData()
        {
            if (IsMultiPlayer)
            {
                if (IsServer)
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Networking.ServerPacketId, Networking.ProcessPacket);
                else
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Networking.ClientPacketId, Networking.ProcessPacket);
            }

            MyEntities.OnEntityCreate -= OnEntityCreate;

            MyVisualScriptLogicProvider.PlayerDisconnected -= PlayerDisconnected;
            MyVisualScriptLogicProvider.PlayerRespawnRequest -= PlayerConnected;

            MyAPIGateway.TerminalControls.CustomActionGetter -= Controls.CustomActionGetter;
            MyAPIGateway.TerminalControls.CustomControlGetter -= Controls.CustomControlGetter;

            Controls.Clean();
            Settings.Clean();

            Logs.Close();
            Clean();

        }

    }
}
