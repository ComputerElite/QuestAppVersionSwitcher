import { createEffect, createResource, createSignal, on } from "solid-js"
import { createStore } from "solid-js/store";
import { IMod, getModsList } from "./api/mods"
import { getPatchingModStatus } from "./api/patching";
import { getConfig } from "./api/app";

export const [initialized, setInitialized] = createSignal<boolean>(false)

// CurrentApplication
export const [currentApplication, setCurrentApplication] = createSignal<string | null>(null)

export const [config, { mutate: mutateSettings, refetch: refetchSettings }] = createResource(getConfig);