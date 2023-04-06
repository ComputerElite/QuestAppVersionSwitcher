import { Signal, createEffect, createResource, createSignal, on } from "solid-js"
import { createStore, reconcile, unwrap } from "solid-js/store";
import { IMod, getModsList } from "../api/mods"
import { getPatchingModStatus } from "../api/patching";
import { getConfig } from "../api/app";

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
        debugger;
      }
    ] as Signal<T>;
  }

// Mods List (all mods and libs)
export const [modsList, {refetch: refetchMods, mutate:mutateMods}] = createResource<Array<IMod>>(async () => {
    let resp = await getModsList();
    return [...resp.libs, ...resp.mods];
}, { storage: createDeepSignal});

