import { Signal, createResource } from "solid-js";
import { createStore, reconcile, unwrap } from "solid-js/store";
import { IMod, getModsList } from "../api/mods";
import { createDeepSignal } from "../util";
import { proxyFetch } from "../api/app";
import toast from "solid-toast";

// Mods List (all mods and libs)
export const [modsList, { refetch: refetchMods, mutate: mutateMods }] =
  createResource<Array<IMod>>(
    async () => {
      let resp = await getModsList();
      return [...resp.libs, ...resp.mods];
    },
    { storage: createDeepSignal },
  );

/**
 * BeatSaber Specific Mods stuff
 */

/**
 * Core mod info
 */
export interface BSCoreModInfo {
  id: string;
  version: string;
  downloadLink: string;
}

export interface BSCoreModsVersionRaw {
  lastUpdated: string;
  mods: Array<BSCoreModInfo>;
}

export interface BSCoreModsRaw {
  [key: string]: BSCoreModsVersionRaw;
}

/**
 * Core Mods list for Beat Saber
 */
export const [
  beatSaberCores,
  { refetch: refetchBeatSaberCores, mutate: mutateBeatSaberCores },
] = createResource(async () => {
  try {
    let response = await proxyFetch(
      "https://computerelite.github.io/tools/Beat_Saber/coreMods.json",
    );
    if (!response.ok) {
      throw new Error("Failed to get Beat Saber Core Mods list");
    }
    return await response.json();
  } catch (e) {
    toast.error("Failed to get Beat Saber Core Mods list");
    console.error(e);
    return {};
  }
});

let BeatSaberModdableList: Array<string> = [];

/**
 * Get a list of all the moddable versions of Beat Saber
 * @returns
 */
export async function GetBeatsaberModdableVersions() {
  if (BeatSaberModdableList.length > 0) {
    return BeatSaberModdableList;
  }

  let items = beatSaberCores();
  if (!items) {
    try {
      let cores = await refetchBeatSaberCores();
      if (!cores) {
        return [];
      }
      BeatSaberModdableList = Object.keys(cores);
    } catch (e) {
      console.error(e);
    }
  } else {
    BeatSaberModdableList = Object.keys(items);
  }
  return BeatSaberModdableList;
}

export async function getCoreModsList() {
  if (beatSaberCores.length == 0) {
    let mods = await refetchBeatSaberCores();
    return mods;
  } else {
    return beatSaberCores();
  }
}
