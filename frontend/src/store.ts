import { createEffect, createResource, createSignal, on} from "solid-js"

import { getAppInfo, getConfig } from "./api/app";
import { getPatchedModdingStatus } from "./api/patching";
import { getCosmeticsTypes } from "./api/cosmetics";

export const [initialized, setInitialized] = createSignal<boolean>(false)

// CurrentApplication
export const [currentApplication, setCurrentApplication] = createSignal<string | null>(null)

export const [config, { mutate: mutateSettings, refetch: refetchSettings }] = createResource(getConfig, { storage: createSignal });

export const [appInfo, { mutate: mutateAppInfo, refetch: refetchAppInfo }] = createResource(getAppInfo, { storage: createSignal });

export const [moddingStatus, { mutate: mutateModdingStatus, refetch: refetchModdingStatus }] = createResource(getPatchedModdingStatus, { storage: createSignal });

export const [cosmeticTypes, { mutate: mutateCosmeticTypes, refetch: refetchCosmeticTypes }] = createResource(getCosmeticsTypes, { storage: createSignal });

// Refetch modding status if the config changes
createEffect(on(config, (config) => {
    setCurrentApplication(config?.currentApp ?? null)
    refetchModdingStatus();
}))