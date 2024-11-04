import { createEffect, createResource, createSignal, on } from "solid-js";

import { getAppInfo, getConfig } from "./api/app";
import {
  HandtrackingType,
  IPatchOptions,
  getPatchedModdingStatus,
  getPatchingOptions,
} from "./api/patching";
import { getCosmeticsTypes } from "./api/cosmetics";
import { createStore } from "solid-js/store";
import { getDeviceInfo } from "./api/android";

export const [initialized, setInitialized] = createSignal<boolean>(false);

// CurrentApplication
export const [currentApplication, setCurrentApplication] = createSignal<
  string | null
>(null);

export const [config, { mutate: mutateSettings, refetch: refetchSettings }] =
  createResource(getConfig, { storage: createSignal });

export const [appInfo, { mutate: mutateAppInfo, refetch: refetchAppInfo }] =
  createResource(getAppInfo, { storage: createSignal });

export const [
  moddingStatus,
  { mutate: mutateModdingStatus, refetch: refetchModdingStatus },
] = createResource(getPatchedModdingStatus, { storage: createSignal });

export const [
  cosmeticTypes,
  { mutate: mutateCosmeticTypes, refetch: refetchCosmeticTypes },
] = createResource(getCosmeticsTypes, { storage: createSignal });

export const [
  deviceInfo,
  { mutate: mutateDeviceInfo, refetch: refetchDeviceInfo },
] = createResource(getDeviceInfo, { storage: createSignal });

export const [
  patchingOptions,
  { mutate: mutatePatchingOptions, refetch: refetchPatchingOptions },
] = createResource<IPatchOptions>(
  async () => {
    return await getPatchingOptions();
  },
  { storage: createSignal },
);

// Refetch modding status if the config changes
createEffect(
  on(config, async (config, prevConfig) => {
    setCurrentApplication(config?.currentApp ?? null);
    await refetchModdingStatus();
    await refetchPatchingOptions();
    await refetchDeviceInfo();
  }),
);
