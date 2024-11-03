import { Signal, createEffect, createResource, on } from "solid-js";
import { createStore, reconcile, unwrap } from "solid-js/store";
import { getBackups, getBackupsResponse } from "../api/backups";
import { config } from "../store";
import { createDeepSignal } from "../util";

// Mods List (all mods and libs)
export const [backupList, { refetch: refetchBackups, mutate: mutateBackups }] =
  createResource<getBackupsResponse>(
    async () => {
      let appId = config()?.currentApp;

      // If no app is selected, return empty
      if (!appId || appId === "") {
        return {
          backups: [],
          backupsSize: 0,
          backupsSizeString: "",
          lastRestored: "",
        };
      }

      let resp = await getBackups(appId);
      return resp;
    },
    { storage: createDeepSignal },
  );

// Refresh the backups list when the app changes
createEffect(
  on(config, () => {
    config()?.currentApp;
    refetchBackups();
  }),
);
