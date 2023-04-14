import { Box, Button, Modal, Typography, List, ListItem } from "@suid/material";
import { For, Signal, createResource, createSignal } from "solid-js"
import { IAppListItem, getAppList } from "../api/app";

import { createStore, reconcile, unwrap } from "solid-js/store";
import { changeManagedApp } from "../api/app";
import { mutateSettings } from "../store";
import { refetchSettings } from "../store";
import toast from "solid-toast";
import { CustomModal } from "./CustomModal";
import RunButton from "../components/Buttons/RunButton";



// Global to show ChangeGameModal
const [isOpen, setIsOpen] = createSignal(false);

export function createDeepSignal<T>(value: T): Signal<T> {
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
const [appList, { refetch: refetchApps, mutate: mutateApps }] = createResource<IAppListItem[]>(async () => {
  let resp = await getAppList();

  // Filter out android apps
  resp = resp.filter((s) => {
    let n = s.PackageName;
    return !(
      n.startsWith("com.android") ||
      n.startsWith("com.google") ||
      // TODO: Remove this for non-dev builds
      // n.startsWith("org.chromium") ||
      n.startsWith("android") ||
      n.startsWith("com.breel")
    );
  });

  return resp;
}, { storage: createDeepSignal });



export async function showChangeGameModal() {
  if (appList && appList.length > 0) {
    refetchApps(appList);
  }

  setIsOpen(true);
}

export function hideChangeGameModal() {
  setIsOpen(false);
}

export default function ChangeGameModal() {

  const closeModal = () => setIsOpen(false);
  const [selected, setSelected] = createSignal<string | null>(null);

  async function onOk() {
    let app = selected();
    if (app == null) {
      toast.error("Please select a game")
      return;
    }

    await changeManagedApp(app);

    await refetchSettings();
    closeModal();
  }

  return (
    <CustomModal
      open={isOpen()}
      onClose={closeModal}
      onBackdropClick={closeModal}
      title={"Change Game"}
      buttons={<>
        <RunButton onClick={closeModal} variant="error" text="Cancel"></RunButton>
        <RunButton onClick={onOk} variant="success" text="Ok" ></RunButton>
      </>}
    >
     
      <Box sx={{ mt: 0, overflowX: "auto" }}>
        <List>
          <For each={appList()}>
            {(app) => (
              <ListItem sx={{
                cursor: "pointer",
                display: "flex",
                flexDirection: "column",
                alignItems: "flex-start",
                backgroundColor: selected() == app.PackageName ? "#2e3847" : "#1F2937",
              }} 
                onClick={() => setSelected(app.PackageName)}
              >
                <Typography component={"div"} sx={{ color: "white" }} >
                  {app.AppName}
                </Typography>
                <Typography component={"div"} fontSize={"0.9em"} sx={{ color: "pink" }} >
                  {app.PackageName}
                </Typography>
              </ListItem>
            )}
          </For>
        </List>
      </Box>
    </CustomModal>
  )
}