﻿using NINA.Plugin.TargetScheduler.Shared.Utility;
using Scheduler.SyncService;

namespace NINA.Plugin.TargetScheduler.SyncService.Sync {

    internal class SyncClientInstance {
        public ClientState ClientState { get; private set; }
        public string Guid { get; private set; }
        public int Pid { get; private set; }
        public string ProfileId { get; private set; }
        public DateTime RegistrationDate { get; private set; }
        public DateTime LastAliveDate { get; private set; }

        public SyncClientInstance(RegistrationRequest request) {
            ClientState = ClientState.Ready;
            Guid = request.Guid;
            Pid = request.Pid;
            ProfileId = request.ProfileId;
            DateTime dateTime = request.Timestamp.ToDateTime().ToLocalTime();
            RegistrationDate = dateTime;
            LastAliveDate = dateTime;
        }

        public void SetState(ClientState state) {
            if (ClientState != state) {
                TSLogger.Info($"SYNC server changing state for client {Guid}: {ClientState} -> {state}");
            }

            ClientState = state;
        }

        public void SetLastAliveDate(ClientIdRequest request) {
            LastAliveDate = DateTime.Now;
        }

        public override string ToString() {
            return $"guid={Guid}, state={ClientState}, reg date={RegistrationDate}, alive date={LastAliveDate}";
        }
    }
}