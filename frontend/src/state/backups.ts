import { Signal, createEffect, createResource, createSignal, on } from "solid-js"
import { createStore, reconcile, unwrap } from "solid-js/store";
import { IMod, getModsList } from "../api/mods"
import { getPatchingModStatus } from "../api/patching";
import { getConfig } from "../api/app";
import { IBackup, getBackups, getBackupsResponse } from "../api/backups";
import { config } from "../store";

// Reconclile
function createDeepSignal<T>(value: T): Signal<T> {
    const [store, setStore] = createStore({
        value
    });
    return [
        () => store.value,
        (v: T) => {
            // unwrap the value to compare it
            const unwrapped = unwrap(store.value);

            // if the value is a function, call it with the unwrapped value
            typeof v === "function" && (v = v(unwrapped));
            setStore("value", reconcile(v));
            return store.value;
        }
    ] as Signal<T>;
}

// Mods List (all mods and libs)
export const [backupList, { refetch: refetchBackups, mutate: mutateBackups }] = createResource<getBackupsResponse>(async () => {
    let appId = config()?.currentApp;

    // If no app is selected, return empty
    if (!appId || appId === "") {
        return {
            backups: [],
            backupsSize: 0,
            backupsSizeString: "",
        };
    }

    let resp = await getBackups(appId);
    return resp;
}, { storage: createDeepSignal });

// Refresh the backups list when the app changes
createEffect(on(config, () => {
    config()?.currentApp
    refetchBackups();
}));
