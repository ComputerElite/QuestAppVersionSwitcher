import { createEffect, createResource, createSignal, on } from "solid-js"

import { getAppInfo, getConfig } from "./api/app";
import { HandtrackingTypes, getPatchedModdingStatus, getPatchingOptions } from "./api/patching";
import { getCosmeticsTypes } from "./api/cosmetics";
import { createStore } from "solid-js/store";

export const [initialized, setInitialized] = createSignal<boolean>(false)

// CurrentApplication
export const [currentApplication, setCurrentApplication] = createSignal<string | null>(null)

export const [config, { mutate: mutateSettings, refetch: refetchSettings }] = createResource(getConfig, { storage: createSignal });

export const [appInfo, { mutate: mutateAppInfo, refetch: refetchAppInfo }] = createResource(getAppInfo, { storage: createSignal });

export const [moddingStatus, { mutate: mutateModdingStatus, refetch: refetchModdingStatus }] = createResource(getPatchedModdingStatus, { storage: createSignal });

export const [cosmeticTypes, { mutate: mutateCosmeticTypes, refetch: refetchCosmeticTypes }] = createResource(getCosmeticsTypes, { storage: createSignal });

export const [patchingOptions, { mutate: mutatePatchingOptions, refetch: refetchPatchingOptions }] = createResource<InternalPatchingOptions>(
    // TODO: Check if we can remove the useless flag that enables handtracking
    (async () => {
        let options = await getPatchingOptions();

        // Map api response to internal response
        return {
            handtracking: options.handTracking ? options.handTrackingVersion : HandtrackingTypes.None,
            addExternalStorage: options.externalStorage,
            addDebug: options.debug,
            additionalPermissions: options.otherPermissions,
        }
    }),
    { storage: createSignal });

// Refetch modding status if the config changes
createEffect(on(config, async (config) => {
    setCurrentApplication(config?.currentApp ?? null)
    await refetchModdingStatus();
    await refetchPatchingOptions();
}))


export interface InternalPatchingOptions {
    handtracking: HandtrackingTypes;
    addExternalStorage: boolean;
    addDebug: boolean;
    additionalPermissions: string[];
}
