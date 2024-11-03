import { Show, createSignal } from "solid-js";
import { config } from "../store";
import toast from "solid-toast";
import RunButton from "../components/Buttons/RunButton";
import { CustomModal } from "./CustomModal";
import { Box, FormControlLabel, Switch } from "@suid/material";
import { SetStoreFunction, createStore } from "solid-js/store";
import { IBackup } from "../api/backups";
import { MediumText, TitleText } from "../styles/TextStyles";
import { PermissionStep } from "./InstallSteps/PermissionsStep";
import { InstallStep } from "./InstallSteps/InstallStep";
import { UninstallStep } from "./InstallSteps/UninstallStep";
import { checkIfGameIsInstalled } from "./InstallSteps/utils";

enum IRestoreStage {
  Form,
  Uninstalling,
  Installing,
  Permissions,
  // TODO:
  GameSpecific,
  Mods,
  Done,
}

export default function RestoreBackupModal(props: {
  open: boolean;
  onClose?: () => void;
  onPatchFinished?: () => void;
  selectedBackup?: IBackup;
}) {
  const [options, setOptions] = createStore<{
    restoreData: boolean;
    backupData?: IBackup;
  }>({
    restoreData: false,
  });
  const [stage, setStage] = createSignal(IRestoreStage.Done);

  /**
   * Used to update the stage and skip stages if needed
   * @param nextStage
   */
  function UpdateStage(nextStage: IRestoreStage) {
    let currentStage = stage;

    setStage(nextStage);
  }

  function internalOnClose() {
    props.onClose?.();

    // Reset the stage
    setStage(IRestoreStage.Form);
  }

  return (
    <CustomModal open={props.open} onClose={internalOnClose} title="Restore">
      <Show when={stage() === IRestoreStage.Form}>
        <RestoreForm
          next={async () => {
            const installed = await checkIfGameIsInstalled();
            UpdateStage(
              installed ? IRestoreStage.Uninstalling : IRestoreStage.Installing,
            );
          }}
          onClose={internalOnClose}
          setOptions={setOptions}
          options={options}
          selectedBackup={props.selectedBackup}
        />
      </Show>
      <Show when={stage() === IRestoreStage.Uninstalling}>
        <UninstallStep
          next={() => {
            UpdateStage(IRestoreStage.Installing);
          }}
          onClose={internalOnClose}
        />
      </Show>
      <Show when={stage() === IRestoreStage.Installing}>
        <InstallStep
          next={() => {
            UpdateStage(IRestoreStage.Permissions);
          }}
          onClose={internalOnClose}
          selectedBackup={props.selectedBackup}
          backupName={props.selectedBackup?.backupName!}
        />
      </Show>
      <Show when={stage() === IRestoreStage.Permissions}>
        <PermissionStep
          next={() => {
            UpdateStage(IRestoreStage.Done);
          }}
          onClose={internalOnClose}
          selectedBackup={props.selectedBackup}
        />
      </Show>
      <Show when={stage() === IRestoreStage.Done}>
        <Box>
          <TitleText>Restore complete</TitleText>
          <RunButton text="Close" onClick={internalOnClose} />
        </Box>
      </Show>
    </CustomModal>
  );
}

function RestoreForm(props: {
  options: {
    restoreData: boolean;
    backupData?: IBackup | undefined;
  };
  onClose?: () => void;
  setOptions: SetStoreFunction<{
    restoreData?: boolean;
    backupData?: IBackup | undefined;
  }>;
  next?: () => void;
  selectedBackup?: IBackup;
}) {
  return (
    <form
      style={{
        "min-width": "300px",
      }}
      onSubmit={async (e) => {
        e.preventDefault();

        let app = config()?.currentApp;
        if (app == null) {
          return toast.error("No app selected");
        }
        props.next?.();
      }}
    >
      <Box class="my-3 bg-slate-950 p-4 rounded-md">
        <TitleText>Selected backup information</TitleText>
        <MediumText>Backup name: {props.selectedBackup?.backupName}</MediumText>
        <MediumText>Size: {props.selectedBackup?.backupSizeString}</MediumText>
        <MediumText>
          Modded:{" "}
          {props.selectedBackup?.isPatchedApk ? (
            <span class="text-green-300">Yes</span>
          ) : (
            <span class="text-red-600">No</span>
          )}
        </MediumText>
        <MediumText>
          Contains apk:{" "}
          {props.selectedBackup?.containsApk ? (
            <span class="text-green-300">Yes</span>
          ) : (
            <span class="text-red-600">No</span>
          )}
        </MediumText>
        <MediumText>
          Contains gamedata:{" "}
          {props.selectedBackup?.containsGamedata ? (
            <span class="text-green-300">Yes</span>
          ) : (
            <span class="text-red-600">No</span>
          )}
        </MediumText>
        <MediumText
          class="max-w-xs wrap"
          sx={{
            wordBreak: "break-all",
          }}
        >
          Location: {props.selectedBackup?.backupLocation}
        </MediumText>
      </Box>

      <FormControlLabel
        sx={{
          pt: 1,
        }}
        control={
          <Switch
            checked={props.options.restoreData}
            onChange={(e, value) => {
              console.log(value);
              props.setOptions("restoreData", value);
            }}
          />
        }
        label="Restore app data"
      />

      <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end", gap: 2 }}>
        <RunButton text="Cancel" onClick={props.onClose} />
        <RunButton text="Start restore" variant="success" type="submit" />
      </Box>
    </form>
  );
}
