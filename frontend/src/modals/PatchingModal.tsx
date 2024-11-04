import { Show, createSignal } from "solid-js";
import { createStore } from "solid-js/store";
import { PermissionStep } from "./InstallSteps/PermissionsStep";
import { PatchingStep } from "./InstallSteps/PatchingStep";
import { InstallStep } from "./InstallSteps/InstallStep";
import { UninstallStep } from "./InstallSteps/UninstallStep";
import { DoneStep } from "./InstallSteps/DoneStep";
import { checkIfGameIsInstalled } from "./InstallSteps/utils";

enum IPatchingStage {
  Patching,
  Uninstalling,
  Installing,
  Permissions,
  GameSpecific,
  Mods,
  Done,
}

export interface GlobalPatchingData {
  backupName?: string;
}

export default function PatchingModal(props: {
  open: boolean;
  onClose?: () => void;
  onPatchFinished?: () => void;
}) {
  const [patchingData, setPatchingData] = createStore<GlobalPatchingData>({});

  const [stage, setStage] = createSignal(IPatchingStage.Patching);

  return (
    <>
      <Show when={props.open}>
        <Show when={stage() === IPatchingStage.Patching}>
          <PatchingStep
            next={async () => {
              const installed = await checkIfGameIsInstalled();
              setStage(
                installed
                  ? IPatchingStage.Uninstalling
                  : IPatchingStage.Installing,
              );
            }}
            onClose={props.onClose}
            setBackupName={(name) => {
              setPatchingData({ backupName: name });
            }}
          />
        </Show>
        <Show when={stage() === IPatchingStage.Uninstalling}>
          <UninstallStep
            next={() => {
              setStage(IPatchingStage.Installing);
            }}
            onClose={props.onClose}
          />
        </Show>
        <Show when={stage() === IPatchingStage.Installing}>
          <InstallStep
            next={() => {
              setStage(IPatchingStage.Permissions);
            }}
            onClose={props.onClose}
            backupName={patchingData.backupName!}
          />
        </Show>
        <Show when={stage() === IPatchingStage.Permissions}>
          <PermissionStep
            next={() => {
              setStage(IPatchingStage.Done);
            }}
          />
        </Show>
        {/* TODO: Add game specific stuff */}
        <Show when={stage() === IPatchingStage.Done}>
          <DoneStep
            next={() => {
              props.onPatchFinished?.();
            }}
            onClose={props.onClose}
            patchingData={patchingData}
          />
        </Show>
      </Show>
    </>
  );
}
