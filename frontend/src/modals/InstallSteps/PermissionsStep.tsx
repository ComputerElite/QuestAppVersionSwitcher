import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { Box } from "@suid/material";
import { config, moddingStatus } from "../../store";
import { createSignal, createMemo, onCleanup, Show } from "solid-js";
import toast from "solid-toast";
import {
  grantAccessToAppAndroidFolders,
  grantManageStorageAccess,
  gotAccessToAppAndroidFolders,
  hasManageStorageAccess,
} from "../../api/android";
import { IBackup } from "../../api/backups";
import RunButton from "../../components/Buttons/RunButton";
import { deviceInfo } from "../../store";
import { MediumText } from "../../styles/TextStyles";
import { GetAndroidVersionName } from "../../util";

enum AndroidPermission {
  AppDirAccess,
  ManageStorageAccess,
}

/**
 * Here we ask to grant permissions to the app and check if they are granted
 */
export function PermissionStep(props: {
  onClose?: () => void;
  next?: () => void;
  prev?: () => void;
  selectedBackup?: IBackup;
}) {
  /**
   * The current device sdk version
   */
  const [sdkVersion, setsdkVersion] = createSignal<number>(
    deviceInfo()?.sdkVersion ?? 0,
  );

  // Access to app dirs is granted
  const [appDirGranted, setAppDirGranted] = createSignal(false);
  const [appManageAccessGranted, setAppManageAccessGranted] =
    createSignal(false);

  const [done, setDone] = createSignal(false);

  async function grantAppDirAccess() {
    let currentApp = config()?.currentApp;
    if (!currentApp) return toast.error("No game selected");

    await grantAccessToAppAndroidFolders(currentApp);
  }

  async function grantManageStorageToApp() {
    let currentApp = config()?.currentApp;
    if (!currentApp) return toast.error("No game selected");
    await grantManageStorageAccess(currentApp);
  }

  const permissionsToGrant = createMemo(() => {
    let permissions: AndroidPermission[] = [];

    if (sdkVersion() > 29 && sdkVersion() < 33) {
      permissions.push(AndroidPermission.AppDirAccess);
    }

    if (sdkVersion() > 29 && moddingStatus()?.isPatched) {
      permissions.push(AndroidPermission.ManageStorageAccess);
    }
    return permissions;
  });

  let allowToProceed = createMemo(() => {
    if (permissionsToGrant().length == 0) return true;

    if (permissionsToGrant().includes(AndroidPermission.AppDirAccess)) {
      if (!appDirGranted()) return false;
    }

    if (permissionsToGrant().includes(AndroidPermission.ManageStorageAccess)) {
      if (!appManageAccessGranted()) return false;
    }
    return true;
  });

  /**
   * Continuously check if the permissions are granted
   */
  const timer: NodeJS.Timeout = setInterval(async () => {
    let currentApp = config()?.currentApp;
    if (!currentApp) return toast.error("No game selected");

    // If we are on Android 11 or and below 13, we need to grant access to the app dir
    if (permissionsToGrant().includes(AndroidPermission.AppDirAccess)) {
      let gotAccess = await gotAccessToAppAndroidFolders(currentApp);
      if (appDirGranted() != gotAccess) {
        setAppDirGranted(gotAccess);
      }
    }

    if (permissionsToGrant().includes(AndroidPermission.ManageStorageAccess)) {
      let gotAccess = await hasManageStorageAccess(currentApp);

      if (appManageAccessGranted() != gotAccess) {
        setAppManageAccessGranted(gotAccess);
      }
    }
  }, 400);

  onCleanup(() => {
    if (timer) clearInterval(timer);
  });

  return (
    <div>
      <Show when={sdkVersion() >= 35}>
        <MediumText>
          Your android version is not supported yet, proceed at your own risk{" "}
          <span>{GetAndroidVersionName(sdkVersion())}</span>
        </MediumText>
      </Show>

      <Show when={allowToProceed()}>
        <MediumText class="text-accent">
          Every permission is granted, you can proceed to the next step
        </MediumText>
      </Show>
      <Show when={!allowToProceed()}>
        <MediumText>
          To allow the game to run properly, we need to grant some permissions
          to the game.
        </MediumText>
        <MediumText>
          Please allow the permissions on your Quest after pressing the buttons
          below. All the buttons need to fade out to continue.
        </MediumText>
      </Show>

      {/* Permission buttons */}
      <Box
        sx={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          mt: 2,
          gap: "5px",
        }}
      >
        <Show
          when={permissionsToGrant().includes(
            AndroidPermission.ManageStorageAccess,
          )}
        >
          {/* Allows the game to access mod data (not in the modloader rn, just to be safe we will add it here) */}
          <RunButton
            disabled={appManageAccessGranted()}
            text="Allow manage storage permission for the app"
            onClick={grantManageStorageToApp}
          />
        </Show>
        <Show
          when={permissionsToGrant().includes(AndroidPermission.AppDirAccess)}
        >
          <RunButton
            disabled={appDirGranted()}
            text="Allows QAVS access to game dirs"
            onClick={grantAppDirAccess}
          />
        </Show>
      </Box>
      <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end", gap: 2 }}>
        {/* If success, allow to go to next step (Done) */}
        <RunButton
          disabled={!allowToProceed()}
          text="Next step"
          icon={<PlayArrowRounded />}
          variant="success"
          onClick={() => {
            props.next?.();
          }}
        />
      </Box>
    </div>
  );
}
